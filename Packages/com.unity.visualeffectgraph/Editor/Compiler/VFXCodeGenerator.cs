using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

using Object = UnityEngine.Object;
using System.Text.RegularExpressions;

namespace UnityEditor.VFX
{
    static class VFXCodeGenerator
    {
        private static string GetIndent(string src, int index)
        {
            var indent = "";
            index--;
            while (index > 0 && (src[index] == ' ' || src[index] == '\t'))
            {
                indent = src[index] + indent;
                index--;
            }
            return indent;
        }

        //This function insure to keep padding while replacing a specific string
        private static void ReplaceMultiline(StringBuilder target, string targetQuery, StringBuilder value)
        {
            string[] delim = { System.Environment.NewLine, "\n" };
            var valueLines = value.ToString().Split(delim, System.StringSplitOptions.None);
            if (valueLines.Length <= 1)
            {
                target.Replace(targetQuery, value.ToString());
            }
            else
            {
                while (true)
                {
                    var targetCopy = target.ToString();
                    var index = targetCopy.IndexOf(targetQuery);
                    if (index == -1)
                    {
                        break;
                    }

                    var indent = GetIndent(targetCopy, index);
                    var currentValue = new StringBuilder();
                    foreach (var line in valueLines)
                    {
                        currentValue.Append(indent + line + '\n');
                    }
                    target.Replace(indent + targetQuery, currentValue.ToString());
                }
            }
        }

        static private VFXShaderWriter GenerateLoadAttribute(string matching, VFXContext context)
        {
            var r = new VFXShaderWriter();

            var regex = new Regex(matching);
            var attributesFromContext = context.GetData().GetAttributes().Where(o => regex.IsMatch(o.attrib.name)).ToArray();
            var attributesSource = attributesFromContext.Where(o => (o.mode & VFXAttributeMode.ReadSource) != 0).ToArray();
            var attributesCurrent = attributesFromContext.Where(o => o.mode != VFXAttributeMode.ReadSource).Where(a => context.GetData().IsAttributeUsed(a.attrib, context) || (context.contextType == VFXContextType.kInit && context.GetData().IsAttributeStored(a.attrib))).ToArray();

            //< Current Attribute
            foreach (var attribute in attributesCurrent.Select(o => o.attrib))
            {
                var name = attribute.name;
                if (name != VFXAttribute.EventCount.name)
                {
                    if (context.contextType != VFXContextType.kInit && context.GetData().IsAttributeStored(attribute))
                    {
                        r.WriteVariable(attribute.type, name, context.GetData().GetLoadAttributeCode(attribute, VFXAttributeLocation.Current));
                    }
                    else
                    {
                        r.WriteVariable(attribute.type, name, attribute.value.GetCodeString(null));
                    }
                }
                else
                {
                    var linkedOutCount = context.allLinkedOutputSlot.Count();
                    for (uint i = 0; i < linkedOutCount; ++i)
                    {
                        r.WriteLineFormat("uint {0}_{1} = 0u;", name, VFXCodeGeneratorHelper.GeneratePrefix(i));
                    }
                    r.WriteVariable(attribute.type, name, attribute.value.GetCodeString(null));
                }
                r.WriteLine();
            }

            //< Source Attribute (default temporary behavior, source is always the initial current value except for init context)
            foreach (var attribute in attributesSource.Select(o => o.attrib))
            {
                var name = string.Format("{0}_source", attribute.name);
                if (context.contextType == VFXContextType.kInit)
                {
                    r.WriteVariable(attribute.type, name, context.GetData().GetLoadAttributeCode(attribute, VFXAttributeLocation.Source));
                }
                else
                {
                    if (attributesCurrent.Any(o => o.attrib.name == attribute.name))
                    {
                        var reference = new VFXAttributeExpression(new VFXAttribute(attribute.name, attribute.value), VFXAttributeLocation.Current);
                        r.WriteVariable(reference.valueType, name, reference.GetCodeString(null));
                    }
                    else
                    {
                        r.WriteVariable(attribute.type, name, attribute.value.GetCodeString(null));
                    }
                }
                r.WriteLine();
            }
            return r;
        }

        private const string eventListOutName = "eventListOut";

        static private VFXShaderWriter GenerateStoreAttribute(string matching, VFXContext context, uint linkedOutCount)
        {
            var r = new VFXShaderWriter();
            var regex = new Regex(matching);

            var attributesFromContext = context.GetData().GetAttributes().Where(o => regex.IsMatch(o.attrib.name) &&
                context.GetData().IsAttributeStored(o.attrib) &&
                (context.contextType == VFXContextType.kInit || context.GetData().IsCurrentAttributeWritten(o.attrib, context))).ToArray();

            foreach (var attribute in attributesFromContext.Select(o => o.attrib))
            {
                r.Write(context.GetData().GetStoreAttributeCode(attribute, new VFXAttributeExpression(attribute).GetCodeString(null)));
                r.WriteLine(';');
            }

            var eventCountName = VFXAttribute.EventCount.name;
            if (regex.IsMatch(eventCountName))
            {
                for (uint i = 0; i < linkedOutCount; ++i)
                {
                    var prefix = VFXCodeGeneratorHelper.GeneratePrefix(i);
                    r.WriteLineFormat("for (uint i = 0; i < {1}_{0}; ++i) {2}_{0}.Append(index);", prefix, eventCountName, eventListOutName);
                }
            }
            return r;
        }

        static private VFXShaderWriter GenerateLoadParameter(string matching, VFXNamedExpression[] namedExpressions, Dictionary<VFXExpression, string> expressionToName)
        {
            var r = new VFXShaderWriter();
            var regex = new Regex(matching);

            var filteredNamedExpressions = namedExpressions.Where(o => regex.IsMatch(o.name) &&
                !(expressionToName.ContainsKey(o.exp) && expressionToName[o.exp] == o.name));     // if parameter already in the global scope, there's nothing to do

            bool needScope = false;
            foreach (var namedExpression in filteredNamedExpressions)
            {
                r.WriteVariable(namedExpression.exp.valueType, namedExpression.name, "0");
                r.WriteLine();
                needScope = true;
            }

            if (needScope)
            {
                var expressionToNameLocal = new Dictionary<VFXExpression, string>(expressionToName);
                r.EnterScope();
                foreach (var namedExpression in filteredNamedExpressions)
                {
                    if (!expressionToNameLocal.ContainsKey(namedExpression.exp))
                    {
                        r.WriteVariable(namedExpression.exp, expressionToNameLocal);
                        r.WriteLine();
                    }
                    r.WriteAssignement(namedExpression.exp.valueType, namedExpression.name, expressionToNameLocal[namedExpression.exp]);
                    r.WriteLine();
                }
                r.ExitScope();
            }

            return r;
        }

        static public StringBuilder Build(VFXContext context, VFXCompilationMode compilationMode, VFXContextCompiledData contextData)
        {
            var templatePath = string.Format("{0}.template", context.codeGeneratorTemplate);
            return Build(context, templatePath, compilationMode, contextData);
        }

        static private void GetFunctionName(VFXBlock block, out string functionName, out string comment)
        {
            var settings = block.GetSettings(true).ToArray();
            if (settings.Length > 0)
            {
                comment = "";
                int hash = 0;
                foreach (var setting in settings)
                {
                    var value = setting.GetValue(block);
                    hash = (hash * 397) ^ value.GetHashCode();
                    comment += string.Format("{0}:{1} ", setting.Name, value.ToString());
                }
                functionName = string.Format("{0}_{1}", block.GetType().Name, hash.ToString("X"));
            }
            else
            {
                comment = null;
                functionName = block.GetType().Name;
            }
        }

        static private string FormatPath(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                #if !UNITY_STANDALONE_LINUX
                .ToLowerInvariant()
                #endif
                ;
        }

        static IEnumerable<Match> GetUniqueMatches(string regexStr, string src)
        {
            var regex = new Regex(regexStr);
            var matches = regex.Matches(src);
            return matches.Cast<Match>().GroupBy(m => m.Groups[0].Value).Select(g => g.First());
        }

        static private VFXShaderWriter GenerateComputeSourceIndex(VFXContext context)
        {
            var r = new VFXShaderWriter();
            var spawnCountAttribute = new VFXAttribute("spawnCount", VFXValueType.Float);
            if (!context.GetData().dependenciesIn.Any())
            {
                var spawnLinkCount = context.GetData().sourceCount;
                r.WriteLine("int sourceIndex = 0;");

                if (spawnLinkCount <= 1)
                    r.WriteLine("/*//Loop with 1 iteration generate a wrong IL Assembly (and actually, useless code)");

                r.WriteLine("uint currentSumSpawnCount = 0u;");
                r.WriteLineFormat("for (sourceIndex=0; sourceIndex<{0}; sourceIndex++)", spawnLinkCount);
                r.EnterScope();
                r.WriteLineFormat("currentSumSpawnCount += uint({0});", context.GetData().GetLoadAttributeCode(spawnCountAttribute, VFXAttributeLocation.Source));
                r.WriteLine("if (id < currentSumSpawnCount)");
                r.EnterScope();
                r.WriteLine("break;");
                r.ExitScope();
                r.ExitScope();

                if (spawnLinkCount <= 1)
                    r.WriteLine("*/");
            }
            else
            {
                /* context invalid or GPU event */
            }
            return r;
        }

        static private StringBuilder GetFlattenedTemplateContent(string path, List<string> includes, IEnumerable<string> defines)
        {
            var formattedPath = FormatPath(path);

            if (includes.Contains(formattedPath))
            {
                var includeHierarchy = new StringBuilder(string.Format("Cyclic VFXInclude dependency detected: {0}\n", formattedPath));
                foreach (var str in Enumerable.Reverse<string>(includes))
                    includeHierarchy.Append(str + '\n');
                throw new InvalidOperationException(includeHierarchy.ToString());
            }

            includes.Add(formattedPath);
            var templateContent = new StringBuilder(System.IO.File.ReadAllText(formattedPath));

            foreach (var match in GetUniqueMatches("\\${VFXInclude\\(\\\"(.*?)\\\"\\)(,.*)?}", templateContent.ToString()))
            {
                var groups = match.Groups;
                var includePath = groups[1].Value;

                if (groups.Count > 2 && !String.IsNullOrEmpty(groups[2].Value))
                {
                    var allDefines = groups[2].Value.Split(new char[] {',', ' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                    var neededDefines = allDefines.Where(d => d[0] != '!');
                    var forbiddenDefines = allDefines.Except(neededDefines).Select(d => d.Substring(1));
                    if (!neededDefines.All(d => defines.Contains(d)) || forbiddenDefines.Any(d => defines.Contains(d)))
                    {
                        ReplaceMultiline(templateContent, groups[0].Value, new StringBuilder());
                        continue;
                    }
                }

                var includeBuilder = GetFlattenedTemplateContent(VisualEffectGraphPackageInfo.assetPackagePath + "/" + includePath, includes, defines);
                ReplaceMultiline(templateContent, groups[0].Value, includeBuilder);
            }

            includes.Remove(formattedPath);
            return templateContent;
        }

        static private void SubstituteMacros(StringBuilder builder)
        {
            var definesToCode = new Dictionary<string, string>();
            var source = builder.ToString();
            Regex beginRegex = new Regex("\\${VFXBegin:(.*)}");

            int currentPos = -1;
            int builderOffset = 0;
            while ((currentPos = source.IndexOf("${")) != -1)
            {
                int endPos = source.IndexOf('}', currentPos);
                if (endPos == -1)
                    throw new FormatException("Ill-formed VFX tag (Missing closing brace");

                var tag = source.Substring(currentPos, endPos - currentPos + 1);
                // Replace any tag found
                if (definesToCode.ContainsKey(tag))
                {
                    var macro = definesToCode[tag];
                    builder.Remove(currentPos + builderOffset, tag.Length);
                    var indentedMacro = macro.Replace("\n", "\n" + GetIndent(source, currentPos));
                    builder.Insert(currentPos + builderOffset, indentedMacro);
                }
                else
                {
                    const string endStr = "${VFXEnd}";
                    var match = beginRegex.Match(source, currentPos, tag.Length);
                    if (match.Success)
                    {
                        var macroStartPos = match.Index + match.Length;
                        var macroEndCodePos = source.IndexOf(endStr, macroStartPos);
                        if (macroEndCodePos == -1)
                            throw new FormatException("${VFXBegin} found without ${VFXEnd}");

                        var defineStr = "${" + match.Groups[1].Value + "}";
                        definesToCode[defineStr] = source.Substring(macroStartPos, macroEndCodePos - macroStartPos);

                        // Remove the define in builder
                        builder.Remove(match.Index + builderOffset, macroEndCodePos - match.Index + endStr.Length);
                    }
                    else if (tag == endStr)
                        throw new FormatException("${VFXEnd} found without ${VFXBegin}");
                    else // Remove undefined tag
                        builder.Remove(currentPos + builderOffset, tag.Length);
                }

                builderOffset += currentPos;
                source = builder.ToString(builderOffset, builder.Length - builderOffset);
            }
        }

        static private StringBuilder Build(VFXContext context, string templatePath, VFXCompilationMode compilationMode, VFXContextCompiledData contextData)
        {
            var dependencies = new HashSet<ScriptableObject>();
            context.CollectDependencies(dependencies);

            var stringBuilder = GetFlattenedTemplateContent(templatePath, new List<string>(), context.additionalDefines);

            var globalDeclaration = new VFXShaderWriter();
            globalDeclaration.WriteCBuffer(contextData.uniformMapper, "parameters");
            globalDeclaration.WriteTexture(contextData.uniformMapper);

            var linkedEventOut = context.allLinkedOutputSlot.Where(s => ((VFXModel)s.owner).GetFirstOfType<VFXContext>().CanBeCompiled()).ToList();
            globalDeclaration.WriteEventBuffer(eventListOutName, linkedEventOut.Count);

            //< Block processor
            var blockFunction = new VFXShaderWriter();
            var blockCallFunction = new VFXShaderWriter();
            var blockDeclared = new HashSet<string>();
            var expressionToName = context.GetData().GetAttributes().ToDictionary(o => new VFXAttributeExpression(o.attrib) as VFXExpression, o => (new VFXAttributeExpression(o.attrib)).GetCodeString(null));
            expressionToName = expressionToName.Union(contextData.uniformMapper.expressionToCode).ToDictionary(s => s.Key, s => s.Value);

            foreach (var current in context.activeChildrenWithImplicit.Select((v, i) => new { block = v, blockIndex = i }))
            {
                var block = current.block;
                var blockIndex = current.blockIndex;

                var parameters = block.mergedAttributes.Select(o =>
                {
                    return new VFXShaderWriter.FunctionParameter
                    {
                        name = o.attrib.name,
                        expression = new VFXAttributeExpression(o.attrib) as VFXExpression,
                        mode = o.mode
                    };
                }).ToList();

                foreach (var parameter in block.parameters)
                {
                    var expReduced = contextData.gpuMapper.FromNameAndId(parameter.name, blockIndex);
                    if (VFXExpression.IsTypeValidOnGPU(expReduced.valueType))
                    {
                        parameters.Add(new VFXShaderWriter.FunctionParameter
                        {
                            name = parameter.name,
                            expression = expReduced,
                            mode = VFXAttributeMode.None
                        });
                    }
                }

                string methodName, commentMethod;
                GetFunctionName(block, out methodName, out commentMethod);
                if (!blockDeclared.Contains(methodName))
                {
                    blockDeclared.Add(methodName);
                    blockFunction.WriteBlockFunction(contextData.gpuMapper,
                        methodName,
                        block.source,
                        parameters,
                        commentMethod);
                }

                //< Parameters (computed and/or extracted from uniform)
                var expressionToNameLocal = expressionToName;
                bool needScope = parameters.Any(o => !expressionToNameLocal.ContainsKey(o.expression));
                if (needScope)
                {
                    expressionToNameLocal = new Dictionary<VFXExpression, string>(expressionToNameLocal);
                    blockCallFunction.EnterScope();
                    foreach (var exp in parameters.Select(o => o.expression))
                    {
                        if (expressionToNameLocal.ContainsKey(exp))
                        {
                            continue;
                        }
                        blockCallFunction.WriteVariable(exp, expressionToNameLocal);
                    }
                }

                var indexEventCount = parameters.FindIndex(o => o.name == VFXAttribute.EventCount.name);
                if (indexEventCount != -1)
                {
                    if ((parameters[indexEventCount].mode & VFXAttributeMode.Read) != 0)
                        throw new InvalidOperationException(string.Format("{0} isn't expected as read (special case)", VFXAttribute.EventCount.name));
                    blockCallFunction.WriteLine(string.Format("{0} = 0u;", VFXAttribute.EventCount.name));
                }

                blockCallFunction.WriteCallFunction(methodName,
                    parameters,
                    contextData.gpuMapper,
                    expressionToNameLocal);

                if (indexEventCount != -1)
                {
                    foreach (var outputSlot in block.outputSlots.SelectMany(o => o.LinkedSlots))
                    {
                        var eventIndex = linkedEventOut.IndexOf(outputSlot);
                        if (eventIndex != -1)
                            blockCallFunction.WriteLineFormat("{0}_{1} += {0};", VFXAttribute.EventCount.name, VFXCodeGeneratorHelper.GeneratePrefix((uint)eventIndex));
                    }
                }
                if (needScope)
                    blockCallFunction.ExitScope();
            }

            //< Final composition
            var renderPipePath = UnityEngine.Experimental.VFX.VFXManager.renderPipeSettingsPath;
            var renderPipeShaderIncludePath = renderPipePath;
            string renderPipeCommon = "Packages/com.unity.visualeffectgraph/Shaders/Common/VFXCommonCompute.cginc";
            string renderPipePasses = null;
            string renderPipeDefines = null;

            if (!context.codeGeneratorCompute && !string.IsNullOrEmpty(renderPipePath))
            {
                renderPipeCommon = renderPipeShaderIncludePath + "/VFXCommon.cginc";
                renderPipePasses = renderPipePath + "/VFXPasses.template";
                renderPipeDefines = renderPipeShaderIncludePath + "/VFXGlobalDefines.cginc";
            }

            var globalIncludeContent = new VFXShaderWriter();
            globalIncludeContent.WriteLine("#include \"HLSLSupport.cginc\"");
            globalIncludeContent.WriteLine("#define NB_THREADS_PER_GROUP 64");
            foreach (var attribute in context.GetData().GetAttributes().Where(a => (context.contextType == VFXContextType.kInit && context.GetData().IsAttributeStored(a.attrib)) || (context.GetData().IsAttributeUsed(a.attrib, context))))
                globalIncludeContent.WriteLineFormat("#define VFX_USE_{0}_{1} 1", attribute.attrib.name.ToUpper(), "CURRENT");
            foreach (var attribute in context.GetData().GetAttributes().Where(a => context.GetData().IsSourceAttributeUsed(a.attrib, context)))
                globalIncludeContent.WriteLineFormat("#define VFX_USE_{0}_{1} 1", attribute.attrib.name.ToUpper(), "SOURCE");

            foreach (var additionnalDefine in context.additionalDefines)
                globalIncludeContent.WriteLineFormat("#define {0} 1", additionnalDefine);

            if (renderPipePasses != null)
                globalIncludeContent.Write(GetFlattenedTemplateContent(renderPipePasses, new List<string>(), context.additionalDefines));

            if (context.GetData() is ISpaceable)
            {
                var spaceable = context.GetData() as ISpaceable;
                globalIncludeContent.WriteLineFormat("#define {0} 1", spaceable.space == VFXCoordinateSpace.World ? "VFX_WORLD_SPACE" : "VFX_LOCAL_SPACE");
            }

            var globalDefinesContent = new VFXShaderWriter();
            globalDefinesContent.WriteLine("#include \"" + renderPipeDefines + "\"");

            var perPassIncludeContent = new VFXShaderWriter();
            perPassIncludeContent.WriteLine("#include \"" + renderPipeCommon + "\"");
            perPassIncludeContent.WriteLine("#include \"Packages/com.unity.visualeffectgraph/Shaders/VFXCommon.cginc\"");

            // Per-block includes
            var includes = Enumerable.Empty<string>();
            foreach (var block in context.activeChildrenWithImplicit)
                includes = includes.Concat(block.includes);
            var uniqueIncludes = new HashSet<string>(includes);
            foreach (var includePath in uniqueIncludes)
                perPassIncludeContent.WriteLine(string.Format("#include \"{0}\"", includePath));

            ReplaceMultiline(stringBuilder, "${VFXGlobalInclude}", globalIncludeContent.builder);
            ReplaceMultiline(stringBuilder, "${VFXGlobalDeclaration}", globalDeclaration.builder);
            ReplaceMultiline(stringBuilder, "${VFXGlobalDefines}", globalDefinesContent.builder);
            ReplaceMultiline(stringBuilder, "${VFXPerPassInclude}", perPassIncludeContent.builder);
            ReplaceMultiline(stringBuilder, "${VFXGeneratedBlockFunction}", blockFunction.builder);
            ReplaceMultiline(stringBuilder, "${VFXProcessBlocks}", blockCallFunction.builder);

            var mainParameters = contextData.gpuMapper.CollectExpression(-1).ToArray();
            foreach (var match in GetUniqueMatches("\\${VFXLoadParameter:{(.*?)}}", stringBuilder.ToString()))
            {
                var str = match.Groups[0].Value;
                var pattern = match.Groups[1].Value;
                var loadParameters = GenerateLoadParameter(pattern, mainParameters, expressionToName);
                ReplaceMultiline(stringBuilder, str, loadParameters.builder);
            }

            //< Compute sourceIndex
            if (stringBuilder.ToString().Contains("${VFXComputeSourceIndex}"))
            {
                var r = GenerateComputeSourceIndex(context);
                ReplaceMultiline(stringBuilder, "${VFXComputeSourceIndex}", r.builder);
            }

            //< Load Attribute
            if (stringBuilder.ToString().Contains("${VFXLoadAttributes}"))
            {
                var loadAttributes = GenerateLoadAttribute(".*", context);
                ReplaceMultiline(stringBuilder, "${VFXLoadAttributes}", loadAttributes.builder);
            }

            foreach (var match in GetUniqueMatches("\\${VFXLoadAttributes:{(.*?)}}", stringBuilder.ToString()))
            {
                var str = match.Groups[0].Value;
                var pattern = match.Groups[1].Value;
                var loadAttributes = GenerateLoadAttribute(pattern, context);
                ReplaceMultiline(stringBuilder, str, loadAttributes.builder);
            }

            //< Store Attribute
            if (stringBuilder.ToString().Contains("${VFXStoreAttributes}"))
            {
                var storeAttribute = GenerateStoreAttribute(".*", context, (uint)linkedEventOut.Count);
                ReplaceMultiline(stringBuilder, "${VFXStoreAttributes}", storeAttribute.builder);
            }

            foreach (var match in GetUniqueMatches("\\${VFXStoreAttributes:{(.*?)}}", stringBuilder.ToString()))
            {
                var str = match.Groups[0].Value;
                var pattern = match.Groups[1].Value;
                var storeAttributes = GenerateStoreAttribute(pattern, context, (uint)linkedEventOut.Count);
                ReplaceMultiline(stringBuilder, str, storeAttributes.builder);
            }

            foreach (var addionalReplacement in context.additionalReplacements)
            {
                ReplaceMultiline(stringBuilder, addionalReplacement.Key, addionalReplacement.Value.builder);
            }

            // Replace defines
            SubstituteMacros(stringBuilder);

            if (VFXViewPreference.advancedLogs)
                Debug.LogFormat("GENERATED_OUTPUT_FILE_FOR : {0}\n{1}", context.ToString(), stringBuilder.ToString());

            return stringBuilder;
        }
    }
}
