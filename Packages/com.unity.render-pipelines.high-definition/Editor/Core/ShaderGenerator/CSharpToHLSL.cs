using System;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    public class CSharpToHLSL
    {
        public static bool GenerateHLSL(System.Type type, GenerateHLSL attribute, out string shaderSource)
        {
            List<string> errors;
            return GenerateHLSL(type, attribute, out shaderSource, out errors);
        }

        public static bool GenerateHLSL(System.Type type, GenerateHLSL attribute, out string shaderSource, out List<string> errors)
        {
            ShaderTypeGenerator gen = new ShaderTypeGenerator(type, attribute);
            bool success = gen.Generate();

            if (success)
            {
                shaderSource = gen.Emit();
            }
            else
            {
                shaderSource = null;
            }

            errors = gen.errors;
            return success;
        }

        public static void GenerateAll()
        {
            s_TypeName = new Dictionary<string, ShaderTypeGenerator>();

            // Iterate over assemblyList, discover all applicable types with fully qualified names
            var assemblyList = AppDomain.CurrentDomain.GetAssemblies()
                // We need to exclude dynamic assemblies (their type can't be queried, throwing an exception below)
                .Where(ass => !(ass.ManifestModule is System.Reflection.Emit.ModuleBuilder))
                .ToList();

            foreach (var assembly in assemblyList)
            {
                var types = assembly.GetExportedTypes();

                foreach (var type in types)
                {
                    var attributes = type.GetCustomAttributes(true);

                    foreach (var attr in attributes)
                    {
                        if (attr is GenerateHLSL)
                        {
                            ShaderTypeGenerator gen;
                            if (s_TypeName.TryGetValue(type.FullName, out gen))
                            {
                                Debug.LogError("Duplicate typename with the GenerateHLSL attribute detected: " + type.FullName +
                                    " declared in both " + gen.type.Assembly.FullName + " and " + type.Assembly.FullName + ".  Skipping the second instance.");
                            }
                            s_TypeName[type.FullName] = new ShaderTypeGenerator(type, attr as GenerateHLSL);
                        }
                    }
                }
            }


            // Now that we have extracted all the typenames that we care about, parse all .cs files in all asset
            // paths and figure out in which files those types are actually declared.
            s_SourceGenerators = new Dictionary<string, List<ShaderTypeGenerator>>();

            var assetPaths = AssetDatabase.GetAllAssetPaths().Where(s => s.EndsWith(".cs")).ToList();
            foreach (var assetPath in assetPaths)
            {
                LoadTypes(assetPath);
            }

            // Finally, write out the generated code
            foreach (var it in s_SourceGenerators)
            {
                string fileName = it.Key + ".hlsl";
                bool skipFile = false;
                foreach (var gen in it.Value)
                {
                    if (!gen.Generate())
                    {
                        // Error reporting will be done by the generator.  Skip this file.
                        gen.PrintErrors();
                        skipFile = true;
                        break;
                    }
                }

                if (skipFile)
                    continue;

                using (var writer = File.CreateText(fileName))
                {
                    var guard = Path.GetFileName(fileName).Replace(".", "_").ToUpper();
                    if (!char.IsLetter(guard[0]))
                        guard = "_" + guard;

                    writer.Write("//\n");
                    writer.Write("// This file was automatically generated. Please don't edit by hand.\n");
                    writer.Write("//\n\n");
                    writer.Write("#ifndef " + guard + "\n");
                    writer.Write("#define " + guard + "\n");

                    foreach (var gen in it.Value)
                    {
                        if (gen.hasStatics)
                        {
                            writer.Write(gen.EmitDefines() + "\n");
                        }
                    }

                    foreach (var gen in it.Value)
                    {
                        if (gen.hasFields)
                        {
                            writer.Write(gen.EmitTypeDecl() + "\n");
                        }
                    }

                    foreach (var gen in it.Value)
                    {
                        if (gen.hasFields && gen.needAccessors)
                        {
                            writer.Write(gen.EmitAccessors() + "\n");
                        }
                    }

                    foreach (var gen in it.Value)
                    {
                        if (gen.hasStatics && gen.hasFields && gen.needParamDebug)
                        {
                            writer.Write(gen.EmitFunctions() + "\n");
                        }
                    }

                    writer.Write("\n#endif\n");

                    var customFile = it.Key + ".custom.hlsl";
                    if (File.Exists(customFile))
                        writer.Write("#include \"{0}\"", Path.GetFileName(customFile));
                }
            }
        }

        static Dictionary<string, ShaderTypeGenerator> s_TypeName;

        static void LoadTypes(string fileName)
        {
            using (var parser = ParserFactory.CreateParser(fileName))
            {
                // @TODO any standard preprocessor symbols we need?

                /*var uniqueSymbols = new HashSet<string>(definedSymbols.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                foreach (var symbol in uniqueSymbols)
                {
                    parser.Lexer.ConditionalCompilationSymbols.Add(symbol, string.Empty);
                }*/
                parser.Lexer.EvaluateConditionalCompilation = true;

                parser.Parse();
                try
                {
                    var visitor = new NamespaceVisitor();
                    var data = new VisitorData { typeName = s_TypeName };
                    parser.CompilationUnit.AcceptVisitor(visitor, data);

                    if (data.generators.Count > 0)
                        s_SourceGenerators[fileName] = data.generators;
                }
                catch
                {
                    // does NRefactory throw anything we can handle here?
                    throw;
                }
            }
        }

        static Dictionary<string, List<ShaderTypeGenerator>> s_SourceGenerators;

        class VisitorData
        {
            public VisitorData()
            {
                currentNamespaces = new Stack<string>();
                currentClasses = new Stack<string>();
                generators = new List<ShaderTypeGenerator>();
            }

            public string GetTypePrefix()
            {
                var fullNamespace = string.Empty;

                var separator = "";

                fullNamespace = currentClasses.Aggregate(fullNamespace, (current, ns) => ns + "+" + current);
                foreach (var ns in currentNamespaces)
                {
                    if (fullNamespace == string.Empty)
                    {
                        separator = ".";
                        fullNamespace = ns;
                    }
                    else
                        fullNamespace = ns + "." + fullNamespace;
                }

                var name = "";
                if (fullNamespace != string.Empty)
                {
                    name = fullNamespace + separator + name;
                }
                return name;
            }

            public readonly Stack<string> currentNamespaces;
            public readonly Stack<string> currentClasses;
            public readonly List<ShaderTypeGenerator> generators;
            public Dictionary<string, ShaderTypeGenerator> typeName;
        }

        class NamespaceVisitor : AbstractAstVisitor
        {
            public override object VisitNamespaceDeclaration(ICSharpCode.NRefactory.Ast.NamespaceDeclaration namespaceDeclaration, object data)
            {
                var visitorData = (VisitorData)data;
                visitorData.currentNamespaces.Push(namespaceDeclaration.Name);
                namespaceDeclaration.AcceptChildren(this, visitorData);
                visitorData.currentNamespaces.Pop();

                return null;
            }

            public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
            {
                // Structured types only
                if (typeDeclaration.Type == ClassType.Class || typeDeclaration.Type == ClassType.Struct || typeDeclaration.Type == ClassType.Enum)
                {
                    var visitorData = (VisitorData)data;

                    var name = visitorData.GetTypePrefix() + typeDeclaration.Name;

                    ShaderTypeGenerator gen;
                    if (visitorData.typeName.TryGetValue(name, out gen))
                    {
                        visitorData.generators.Add(gen);
                    }

                    visitorData.currentClasses.Push(typeDeclaration.Name);
                    typeDeclaration.AcceptChildren(this, visitorData);
                    visitorData.currentClasses.Pop();
                }

                return null;
            }
        }
    }
}
