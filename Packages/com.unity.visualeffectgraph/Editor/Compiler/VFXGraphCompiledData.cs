using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.Experimental.VFX;

using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    struct VFXContextCompiledData
    {
        public VFXExpressionMapper cpuMapper;
        public VFXExpressionMapper gpuMapper;
        public VFXUniformMapper uniformMapper;
        public VFXMapping[] parameters;
        public int indexInShaderSource;
    }

    enum VFXCompilationMode
    {
        Edition,
        Runtime,
    }

    class VFXGraphCompiledData
    {
        public VFXGraphCompiledData(VFXGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException("VFXGraph cannot be null");
            m_Graph = graph;
        }

        private struct GeneratedCodeData
        {
            public VFXContext context;
            public bool computeShader;
            public System.Text.StringBuilder content;
            public VFXCompilationMode compilMode;
        }

        private static VFXExpressionValueContainerDesc<T> CreateValueDesc<T>(VFXExpression exp, int expIndex)
        {
            var desc = new VFXExpressionValueContainerDesc<T>();
            desc.value = exp.Get<T>();
            return desc;
        }

        private void SetValueDesc<T>(VFXExpressionValueContainerDesc desc, VFXExpression exp)
        {
            ((VFXExpressionValueContainerDesc<T>)desc).value = exp.Get<T>();
        }

        public uint FindReducedExpressionIndexFromSlotCPU(VFXSlot slot)
        {
            if (m_ExpressionGraph == null)
            {
                return uint.MaxValue;
            }
            var targetExpression = slot.GetExpression();
            if (targetExpression == null)
            {
                return uint.MaxValue;
            }

            if (!m_ExpressionGraph.CPUExpressionsToReduced.ContainsKey(targetExpression))
            {
                return uint.MaxValue;
            }

            var ouputExpression = m_ExpressionGraph.CPUExpressionsToReduced[targetExpression];
            return (uint)m_ExpressionGraph.GetFlattenedIndex(ouputExpression);
        }

        private static void FillExpressionDescs(List<VFXExpressionDesc> outExpressionDescs, List<VFXExpressionValueContainerDesc> outValueDescs, VFXExpressionGraph graph)
        {
            var flatGraph = graph.FlattenedExpressions;
            var numFlattenedExpressions = flatGraph.Count;

            for (int i = 0; i < numFlattenedExpressions; ++i)
            {
                var exp = flatGraph[i];

                // Must match data in C++ expression
                if (exp.Is(VFXExpression.Flags.Value))
                {
                    VFXExpressionValueContainerDesc value;
                    switch (exp.valueType)
                    {
                        case VFXValueType.Float: value = CreateValueDesc<float>(exp, i); break;
                        case VFXValueType.Float2: value = CreateValueDesc<Vector2>(exp, i); break;
                        case VFXValueType.Float3: value = CreateValueDesc<Vector3>(exp, i); break;
                        case VFXValueType.Float4: value = CreateValueDesc<Vector4>(exp, i); break;
                        case VFXValueType.Int32: value = CreateValueDesc<int>(exp, i); break;
                        case VFXValueType.Uint32: value = CreateValueDesc<uint>(exp, i); break;
                        case VFXValueType.Texture2D:
                        case VFXValueType.Texture2DArray:
                        case VFXValueType.Texture3D:
                        case VFXValueType.TextureCube:
                        case VFXValueType.TextureCubeArray:
                            value = CreateValueDesc<Texture>(exp, i);
                            break;
                        case VFXValueType.Matrix4x4: value = CreateValueDesc<Matrix4x4>(exp, i); break;
                        case VFXValueType.Curve: value = CreateValueDesc<AnimationCurve>(exp, i); break;
                        case VFXValueType.ColorGradient: value = CreateValueDesc<Gradient>(exp, i); break;
                        case VFXValueType.Mesh: value = CreateValueDesc<Mesh>(exp, i); break;
                        case VFXValueType.Boolean: value = CreateValueDesc<bool>(exp, i); break;
                        default: throw new InvalidOperationException("Invalid type");
                    }
                    value.expressionIndex = (uint)i;
                    outValueDescs.Add(value);
                }

                outExpressionDescs.Add(new VFXExpressionDesc
                {
                    op = exp.operation,
                    data = exp.GetOperands(graph).ToArray(),
                });
            }
        }

        private static void CollectExposedDesc(List<VFXMapping> outExposedParameters, string name, VFXSlot slot, VFXExpressionGraph graph)
        {
            var expression = slot.valueType != VFXValueType.None ? slot.GetInExpression() : null;
            if (expression != null)
            {
                var exprIndex = graph.GetFlattenedIndex(expression);
                if (exprIndex == -1)
                    throw new InvalidOperationException("Unable to retrieve value from exposed for " + name);

                outExposedParameters.Add(new VFXMapping()
                {
                    name = name,
                    index = exprIndex
                });
            }
            else
            {
                foreach (var child in slot.children)
                {
                    CollectExposedDesc(outExposedParameters, name + "_" + child.name, child, graph);
                }
            }
        }

        private static void FillExposedDescs(List<VFXMapping> outExposedParameters, VFXExpressionGraph graph, IEnumerable<VFXParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.exposed)
                {
                    CollectExposedDesc(outExposedParameters, parameter.exposedName, parameter.GetOutputSlot(0), graph);
                }
            }
        }

        private static void FillEventAttributeDescs(List<VFXLayoutElementDesc> eventAttributeDescs, VFXExpressionGraph graph, IEnumerable<VFXContext> contexts)
        {
            foreach (var context in contexts.Where(o => o.contextType == VFXContextType.kSpawner))
            {
                foreach (var linked in context.outputContexts)
                {
                    var data = linked.GetData();
                    if (data)
                    {
                        foreach (var attribute in data.GetAttributes())
                        {
                            if ((attribute.mode & VFXAttributeMode.ReadSource) != 0 && !eventAttributeDescs.Any(o => o.name == attribute.attrib.name))
                            {
                                eventAttributeDescs.Add(new VFXLayoutElementDesc()
                                {
                                    name = attribute.attrib.name,
                                    type = attribute.attrib.type
                                });
                            }
                        }
                    }
                }
            }

            var structureLayoutTotalSize = (uint)eventAttributeDescs.Sum(e => (long)VFXExpression.TypeToSize(e.type));
            var currentLayoutSize = 0u;
            var listWithOffset = new List<VFXLayoutElementDesc>();
            eventAttributeDescs.ForEach(e =>
            {
                e.offset.element = currentLayoutSize;
                e.offset.structure = structureLayoutTotalSize;
                currentLayoutSize += (uint)VFXExpression.TypeToSize(e.type);
                listWithOffset.Add(e);
            });

            eventAttributeDescs.Clear();
            eventAttributeDescs.AddRange(listWithOffset);
        }

        private static List<VFXContext> CollectContextParentRecursively(List<VFXContext> inputList)
        {
            var contextList = inputList.SelectMany(o => o.inputContexts).Distinct().ToList();
            if (contextList.Any(o => o.inputContexts.Any()))
            {
                var parentContextList = CollectContextParentRecursively(contextList);
                foreach (var context in parentContextList)
                {
                    if (!contextList.Contains(context))
                    {
                        contextList.Add(context);
                    }
                }
            }
            return contextList;
        }

        private static VFXContext[] CollectSpawnersHierarchy(IEnumerable<VFXContext> vfxContext)
        {
            var initContext = vfxContext.Where(o => o.contextType == VFXContextType.kInit).ToList();
            var spawnerList = CollectContextParentRecursively(initContext);
            return spawnerList.Where(o => o.contextType == VFXContextType.kSpawner).Reverse().ToArray();
        }

        struct SpawnInfo
        {
            public int bufferIndex;
            public int systemIndex;
        }

        private static VFXCPUBufferData ComputeArrayOfStructureInitialData(IEnumerable<VFXLayoutElementDesc> layout)
        {
            var data = new VFXCPUBufferData();
            foreach (var element in layout)
            {
                var attribute = VFXAttribute.AllAttribute.FirstOrDefault(o => o.name == element.name);
                bool useAttribute = attribute.name == element.name;
                if (element.type == VFXValueType.Boolean)
                {
                    var v = useAttribute ? attribute.value.Get<bool>() : default(bool);
                    data.PushBool(v);
                }
                else if (element.type == VFXValueType.Float)
                {
                    var v = useAttribute ? attribute.value.Get<float>() : default(float);
                    data.PushFloat(v);
                }
                else if (element.type == VFXValueType.Float2)
                {
                    var v = useAttribute ? attribute.value.Get<Vector2>() : default(Vector2);
                    data.PushFloat(v.x);
                    data.PushFloat(v.y);
                }
                else if (element.type == VFXValueType.Float3)
                {
                    var v = useAttribute ? attribute.value.Get<Vector3>() : default(Vector3);
                    data.PushFloat(v.x);
                    data.PushFloat(v.y);
                    data.PushFloat(v.z);
                }
                else if (element.type == VFXValueType.Float4)
                {
                    var v = useAttribute ? attribute.value.Get<Vector4>() : default(Vector4);
                    data.PushFloat(v.x);
                    data.PushFloat(v.y);
                    data.PushFloat(v.z);
                    data.PushFloat(v.w);
                }
                else if (element.type == VFXValueType.Int32)
                {
                    var v = useAttribute ? attribute.value.Get<int>() : default(int);
                    data.PushInt(v);
                }
                else if (element.type == VFXValueType.Uint32)
                {
                    var v = useAttribute ? attribute.value.Get<uint>() : default(uint);
                    data.PushUInt(v);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            return data;
        }

        private static void FillSpawner(Dictionary<VFXContext, SpawnInfo> outContextSpawnToSpawnInfo, List<VFXCPUBufferDesc> outCpuBufferDescs, List<VFXEditorSystemDesc> outSystemDescs, IEnumerable<VFXContext> contexts, VFXExpressionGraph graph, List<VFXLayoutElementDesc> globalEventAttributeDescs, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData)
        {
            var spawners = CollectSpawnersHierarchy(contexts);
            foreach (var it in spawners.Select((spawner, index) => new { spawner, index }))
            {
                outContextSpawnToSpawnInfo.Add(it.spawner, new SpawnInfo() { bufferIndex = outCpuBufferDescs.Count, systemIndex = it.index });
                outCpuBufferDescs.Add(new VFXCPUBufferDesc()
                {
                    capacity = 1u,
                    stride = globalEventAttributeDescs.First().offset.structure,
                    layout = globalEventAttributeDescs.ToArray(),
                    initialData = ComputeArrayOfStructureInitialData(globalEventAttributeDescs)
                });
            }
            foreach (var spawnContext in spawners)
            {
                var buffers = new List<VFXMapping>();
                buffers.Add(new VFXMapping()
                {
                    index = outContextSpawnToSpawnInfo[spawnContext].bufferIndex,
                    name = "spawner_output"
                });

                for (int indexSlot = 0; indexSlot < 2; ++indexSlot)
                {
                    foreach (var input in spawnContext.inputFlowSlot[indexSlot].link)
                    {
                        var inputContext = input.context as VFXContext;
                        if (outContextSpawnToSpawnInfo.ContainsKey(inputContext))
                        {
                            buffers.Add(new VFXMapping()
                            {
                                index = outContextSpawnToSpawnInfo[inputContext].bufferIndex,
                                name = "spawner_input_" + (indexSlot == 0 ? "OnPlay" : "OnStop")
                            });
                        }
                    }
                }

                var contextData = contextToCompiledData[spawnContext];
                outSystemDescs.Add(new VFXEditorSystemDesc()
                {
                    buffers = buffers.ToArray(),
                    capacity = 0u,
                    flags = VFXSystemFlag.SystemDefault,
                    layer = uint.MaxValue,
                    tasks = spawnContext.activeChildrenWithImplicit.Select((b, index) =>
                    {
                        var spawnerBlock = b as VFXAbstractSpawner;
                        if (spawnerBlock == null)
                        {
                            throw new InvalidCastException("Unexpected block type in spawnerContext");
                        }
                        if (spawnerBlock.spawnerType == VFXTaskType.CustomCallbackSpawner && spawnerBlock.customBehavior == null)
                        {
                            throw new InvalidOperationException("VFXAbstractSpawner excepts a custom behavior for custom callback type");
                        }
                        if (spawnerBlock.spawnerType != VFXTaskType.CustomCallbackSpawner && spawnerBlock.customBehavior != null)
                        {
                            throw new InvalidOperationException("VFXAbstractSpawner only expects a custom behavior for custom callback type");
                        }

                        var cpuExpression = contextData.cpuMapper.CollectExpression(index, false).Select(o =>
                        {
                            return new VFXMapping
                            {
                                index = graph.GetFlattenedIndex(o.exp),
                                name = o.name
                            };
                        }).ToArray();

                        Object processor = null;
                        if (spawnerBlock.customBehavior != null)
                        {
                            var assets = AssetDatabase.FindAssets("t:TextAsset " + spawnerBlock.customBehavior.Name);
                            if (assets.Length != 1)
                            {
                                // AssetDatabase.FindAssets will not search in package by default. Search in our package explicitely
                                assets = AssetDatabase.FindAssets("t:TextAsset " + spawnerBlock.customBehavior.Name, new string[] { VisualEffectGraphPackageInfo.assetPackagePath });
                                if (assets.Length != 1)
                                {
                                    throw new InvalidOperationException("Unable to find the definition .cs file for " + spawnerBlock.customBehavior + " Make sure that the class name and file name match");
                                }
                            }

                            var assetPath = AssetDatabase.GUIDToAssetPath(assets[0]);
                            processor = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                        }

                        return new VFXEditorTaskDesc
                        {
                            type = spawnerBlock.spawnerType,
                            buffers = new VFXMapping[0],
                            values = cpuExpression.ToArray(),
                            parameters = contextData.parameters,
                            externalProcessor = processor
                        };
                    }).ToArray()
                });
            }
        }

        private static void FillEvent(List<VFXEventDesc> outEventDesc, Dictionary<VFXContext, SpawnInfo> contextSpawnToSpawnInfo, IEnumerable<VFXContext> contexts)
        {
            var allPlayNotLinked = contextSpawnToSpawnInfo.Where(o => !o.Key.inputFlowSlot[0].link.Any()).Select(o => (uint)o.Value.systemIndex).ToList();
            var allStopNotLinked = contextSpawnToSpawnInfo.Where(o => !o.Key.inputFlowSlot[1].link.Any()).Select(o => (uint)o.Value.systemIndex).ToList();

            var eventDescTemp = new[]
            {
                new { eventName = "OnPlay", playSystems = allPlayNotLinked, stopSystems = new List<uint>() },
                new { eventName = "OnStop", playSystems = new List<uint>(), stopSystems = allStopNotLinked },
            }.ToList();

            var events = contexts.Where(o => o.contextType == VFXContextType.kEvent);
            foreach (var evt in events)
            {
                var eventName = (evt as VFXBasicEvent).eventName;
                foreach (var link in evt.outputFlowSlot[0].link)
                {
                    if (contextSpawnToSpawnInfo.ContainsKey(link.context))
                    {
                        var eventIndex = eventDescTemp.FindIndex(o => o.eventName == eventName);
                        if (eventIndex == -1)
                        {
                            eventIndex = eventDescTemp.Count;
                            eventDescTemp.Add(new
                            {
                                eventName = eventName,
                                playSystems = new List<uint>(),
                                stopSystems = new List<uint>(),
                            });
                        }

                        var startSystem = link.slotIndex == 0;
                        var spawnerIndex = (uint)contextSpawnToSpawnInfo[link.context].systemIndex;
                        if (startSystem)
                        {
                            eventDescTemp[eventIndex].playSystems.Add(spawnerIndex);
                        }
                        else
                        {
                            eventDescTemp[eventIndex].stopSystems.Add(spawnerIndex);
                        }
                    }
                }
            }
            outEventDesc.Clear();
            outEventDesc.AddRange(eventDescTemp.Select(o => new VFXEventDesc() { name = o.eventName, startSystems = o.playSystems.ToArray(), stopSystems = o.stopSystems.ToArray() }));
        }

        private static void GenerateShaders(List<GeneratedCodeData> outGeneratedCodeData, VFXExpressionGraph graph, IEnumerable<VFXContext> contexts, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData, VFXCompilationMode compilationMode)
        {
            Profiler.BeginSample("VFXEditor.GenerateShaders");
            try
            {
                foreach (var context in contexts)
                {
                    var gpuMapper = graph.BuildGPUMapper(context);
                    var uniformMapper = new VFXUniformMapper(gpuMapper, context.doesGenerateShader);

                    // Add gpu and uniform mapper
                    var contextData = contextToCompiledData[context];
                    contextData.gpuMapper = gpuMapper;
                    contextData.uniformMapper = uniformMapper;
                    contextToCompiledData[context] = contextData;

                    if (context.doesGenerateShader)
                    {
                        var generatedContent = VFXCodeGenerator.Build(context, compilationMode, contextData);

                        outGeneratedCodeData.Add(new GeneratedCodeData()
                        {
                            context = context,
                            computeShader = context.codeGeneratorCompute,
                            compilMode = compilationMode,
                            content = generatedContent
                        });
                    }
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private static void SaveShaderFiles(VisualEffectResource resource, List<GeneratedCodeData> generatedCodeData, Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData)
        {
            Profiler.BeginSample("VFXEditor.SaveShaderFiles");
            try
            {
                VFXShaderSourceDesc[] descs = new VFXShaderSourceDesc[generatedCodeData.Count];

                for (int i = 0; i < generatedCodeData.Count; ++i)
                {
                    var generated = generatedCodeData[i];
                    var fileName = generated.context.fileName;

                    if( ! generated.computeShader)
                    {
                        generated.content.Insert(0,"Shader \""+generated.context.shaderName + "\"\n") ;
                    }
                    descs[i].source = generated.content.ToString();
                    descs[i].name = fileName;
                    descs[i].compute = generated.computeShader;
                }

                resource.shaderSources = descs;

                for (int i = 0; i < generatedCodeData.Count; ++i)
                {
                    var generated = generatedCodeData[i];
                    var contextData = contextToCompiledData[generated.context];
                    contextData.indexInShaderSource = i;
                    contextToCompiledData[generated.context] = contextData;
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        public void FillDependentBuffer(IEnumerable<VFXData> compilableData, List<VFXGPUBufferDesc> bufferDescs, Dictionary<VFXData, int> attributeBufferDictionnary, Dictionary<VFXData, int> eventGpuBufferDictionnary)
        {
            foreach (var data in compilableData.OfType<VFXDataParticle>())
            {
                int attributeBufferIndex = -1;
                if (data.attributeBufferSize > 0)
                {
                    attributeBufferIndex = bufferDescs.Count;
                    bufferDescs.Add(data.attributeBufferDesc);
                }
                attributeBufferDictionnary.Add(data, attributeBufferIndex);
            }

            //Prepare GPU event buffer
            foreach (var data in compilableData.SelectMany(o => o.dependenciesOut).Distinct().OfType<VFXDataParticle>())
            {
                var eventBufferIndex = -1;
                if (data.capacity > 0)
                {
                    eventBufferIndex = bufferDescs.Count;
                    bufferDescs.Add(new VFXGPUBufferDesc() { type = ComputeBufferType.Append, size = data.capacity, stride = 4 });
                }
                eventGpuBufferDictionnary.Add(data, eventBufferIndex);
            }
        }

        VFXRendererSettings GetRendererSettings(VFXRendererSettings initialSettings, IEnumerable<IVFXSubRenderer> subRenderers)
        {
            var settings = initialSettings;
            settings.shadowCastingMode = subRenderers.Any(r => r.hasShadowCasting) ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return settings;
        }

        private class VFXImplicitContextOfExposedExpression : VFXContext
        {
            private VFXExpressionMapper mapper;

            public VFXImplicitContextOfExposedExpression() : base(VFXContextType.kNone, VFXDataType.kNone, VFXDataType.kNone) {}

            private static void CollectExposedExpression(List<VFXExpression> expressions, VFXSlot slot)
            {
                var expression = slot.valueType != VFXValueType.None ? slot.GetInExpression() : null;
                if (expression != null)
                    expressions.Add(expression);
                else
                {
                    foreach (var child in slot.children)
                        CollectExposedExpression(expressions, child);
                }
            }

            public void FillExpression(VFXGraph graph)
            {
                var allExposedParameter = graph.children.OfType<VFXParameter>().Where(o => o.exposed);
                var expressionsList = new List<VFXExpression>();
                foreach (var parameter in allExposedParameter)
                    CollectExposedExpression(expressionsList, parameter.outputSlots[0]);

                mapper = new VFXExpressionMapper();
                for (int i = 0; i < expressionsList.Count; ++i)
                    mapper.AddExpression(expressionsList[i], "ImplicitExposedExpression", i);
            }

            public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
            {
                return target == VFXDeviceTarget.CPU ? mapper : null;
            }
        }


        public void Compile(VFXCompilationMode compilationMode)
        {
            // Prevent doing anything ( and especially showing progesses ) in an empty graph.
            if (m_Graph.children.Count() < 1)
            {
                // Cleaning
                if (m_Graph.visualEffectResource != null)
                    m_Graph.visualEffectResource.ClearRuntimeData();

                m_ExpressionGraph = new VFXExpressionGraph();
                m_ExpressionValues = new VFXExpressionValueContainerDesc[] {};
                return;
            }

            Profiler.BeginSample("VFXEditor.CompileAsset");
            try
            {
                float nbSteps = 12.0f;
                string assetPath = AssetDatabase.GetAssetPath(visualEffectResource);
                string progressBarTitle = "Compiling " + assetPath;

                EditorUtility.DisplayProgressBar(progressBarTitle, "Collecting dependencies", 0 / nbSteps);
                var models = new HashSet<ScriptableObject>();
                m_Graph.CollectDependencies(models);

                var contexts = models.OfType<VFXContext>().ToArray();

                foreach (var c in contexts) // Unflag all contexts
                    c.MarkAsCompiled(false);

                var compilableContexts = models.OfType<VFXContext>().Where(c => c.CanBeCompiled());
                var compilableData = models.OfType<VFXData>().Where(d => d.CanBeCompiled());

                IEnumerable<VFXContext> implicitContexts = Enumerable.Empty<VFXContext>();
                foreach (var d in compilableData) // Flag compiled contexts
                    implicitContexts = implicitContexts.Concat(d.InitImplicitContexts());
                compilableContexts = compilableContexts.Concat(implicitContexts.ToArray());

                foreach (var c in compilableContexts) // Flag compiled contexts
                    c.MarkAsCompiled(true);

                EditorUtility.DisplayProgressBar(progressBarTitle, "Collecting attributes", 1 / nbSteps);
                foreach (var data in compilableData)
                    data.CollectAttributes();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Process dependencies", 2 / nbSteps);
                foreach (var data in compilableData)
                    data.ProcessDependencies();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Compiling expression Graph", 3 / nbSteps);
                m_ExpressionGraph = new VFXExpressionGraph();
                var exposedExpressionContext = ScriptableObject.CreateInstance<VFXImplicitContextOfExposedExpression>();
                exposedExpressionContext.FillExpression(m_Graph); //Force all exposed expression to be visible, only for registering in CompileExpressions

                var expressionContextOptions = compilationMode == VFXCompilationMode.Runtime ? VFXExpressionContextOption.ConstantFolding : VFXExpressionContextOption.Reduction;
                m_ExpressionGraph.CompileExpressions(compilableContexts.Concat(new VFXContext[] { exposedExpressionContext }), expressionContextOptions);

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generating bytecode", 4 / nbSteps);
                var expressionDescs = new List<VFXExpressionDesc>();
                var valueDescs = new List<VFXExpressionValueContainerDesc>();
                FillExpressionDescs(expressionDescs, valueDescs, m_ExpressionGraph);

                Dictionary<VFXContext, VFXContextCompiledData> contextToCompiledData = new Dictionary<VFXContext, VFXContextCompiledData>();
                foreach (var context in compilableContexts)
                    contextToCompiledData.Add(context, new VFXContextCompiledData());

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generating mappings", 5 / nbSteps);
                foreach (var context in compilableContexts)
                {
                    var cpuMapper = m_ExpressionGraph.BuildCPUMapper(context);
                    var contextData = contextToCompiledData[context];
                    contextData.cpuMapper = cpuMapper;
                    contextData.parameters = context.additionalMappings.ToArray();
                    contextToCompiledData[context] = contextData;
                }

                var exposedParameterDescs = new List<VFXMapping>();
                FillExposedDescs(exposedParameterDescs, m_ExpressionGraph, models.OfType<VFXParameter>());
                var globalEventAttributeDescs = new List<VFXLayoutElementDesc>() { new VFXLayoutElementDesc() { name = "spawnCount", type = VFXValueType.Float } };
                FillEventAttributeDescs(globalEventAttributeDescs, m_ExpressionGraph, compilableContexts);

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generating Attribute layouts", 6 / nbSteps);
                foreach (var data in compilableData)
                    data.GenerateAttributeLayout();

                var generatedCodeData = new List<GeneratedCodeData>();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generating shaders", 7 / nbSteps);
                GenerateShaders(generatedCodeData, m_ExpressionGraph, compilableContexts, contextToCompiledData, compilationMode);

                EditorUtility.DisplayProgressBar(progressBarTitle, "Saving shaders", 8 / nbSteps);
                SaveShaderFiles(m_Graph.visualEffectResource, generatedCodeData, contextToCompiledData);

                var bufferDescs = new List<VFXGPUBufferDesc>();
                var cpuBufferDescs = new List<VFXCPUBufferDesc>();
                var systemDescs = new List<VFXEditorSystemDesc>();

                EditorUtility.DisplayProgressBar(progressBarTitle, "Generating systems", 9 / nbSteps);
                cpuBufferDescs.Add(new VFXCPUBufferDesc()
                {
                    capacity = 1u,
                    layout = globalEventAttributeDescs.ToArray(),
                    stride = globalEventAttributeDescs.First().offset.structure,
                    initialData = ComputeArrayOfStructureInitialData(globalEventAttributeDescs)
                });
                var contextSpawnToSpawnInfo = new Dictionary<VFXContext, SpawnInfo>();
                FillSpawner(contextSpawnToSpawnInfo, cpuBufferDescs, systemDescs, compilableContexts, m_ExpressionGraph, globalEventAttributeDescs, contextToCompiledData);

                var eventDescs = new List<VFXEventDesc>();
                FillEvent(eventDescs, contextSpawnToSpawnInfo, compilableContexts);

                var attributeBufferDictionnary = new Dictionary<VFXData, int>();
                var eventGpuBufferDictionnary = new Dictionary<VFXData, int>();
                FillDependentBuffer(compilableData, bufferDescs, attributeBufferDictionnary, eventGpuBufferDictionnary);

                var contextSpawnToBufferIndex = contextSpawnToSpawnInfo.Select(o => new { o.Key, o.Value.bufferIndex }).ToDictionary(o => o.Key, o => o.bufferIndex);
                foreach (var data in compilableData)
                {
                    data.FillDescs(bufferDescs,
                        systemDescs,
                        m_ExpressionGraph,
                        contextToCompiledData,
                        contextSpawnToBufferIndex,
                        attributeBufferDictionnary,
                        eventGpuBufferDictionnary);
                }

                // Update renderer settings
                VFXRendererSettings rendererSettings = GetRendererSettings(m_Graph.visualEffectResource.rendererSettings, compilableContexts.OfType<IVFXSubRenderer>());
                m_Graph.visualEffectResource.rendererSettings = rendererSettings;

                EditorUtility.DisplayProgressBar(progressBarTitle, "Setting up systems", 10 / nbSteps);
                var expressionSheet = new VFXExpressionSheet();
                expressionSheet.expressions = expressionDescs.ToArray();
                expressionSheet.values = valueDescs.OrderBy(o => o.expressionIndex).ToArray();
                expressionSheet.exposed = exposedParameterDescs.OrderBy(o => o.name).ToArray();

                m_Graph.visualEffectResource.SetRuntimeData(expressionSheet, systemDescs.ToArray(), eventDescs.ToArray(), bufferDescs.ToArray(), cpuBufferDescs.ToArray());
                m_ExpressionValues = expressionSheet.values;

                EditorUtility.DisplayProgressBar(progressBarTitle, "Importing VFX", 11 / nbSteps);
                Profiler.BeginSample("VFXEditor.CompileAsset:ImportAsset");
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate); //This should compile the shaders on the C++ size
                Profiler.EndSample();
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("Exception while compiling expression graph: {0}: {1}", e, e.StackTrace));

                // Cleaning
                if (m_Graph.visualEffectResource != null)
                    m_Graph.visualEffectResource.ClearRuntimeData();

                m_ExpressionGraph = new VFXExpressionGraph();
                m_ExpressionValues = new VFXExpressionValueContainerDesc[] {};
            }
            finally
            {
                Profiler.EndSample();
                EditorUtility.ClearProgressBar();
            }
        }

        public void UpdateValues()
        {
            var flatGraph = m_ExpressionGraph.FlattenedExpressions;
            var numFlattenedExpressions = flatGraph.Count;

            int descIndex = 0;
            for (int i = 0; i < numFlattenedExpressions; ++i)
            {
                var exp = flatGraph[i];
                if (exp.Is(VFXExpression.Flags.Value))
                {
                    var desc = m_ExpressionValues[descIndex++];
                    if (desc.expressionIndex != i)
                        throw new InvalidOperationException();

                    switch (exp.valueType)
                    {
                        case VFXValueType.Float: SetValueDesc<float>(desc, exp); break;
                        case VFXValueType.Float2: SetValueDesc<Vector2>(desc, exp); break;
                        case VFXValueType.Float3: SetValueDesc<Vector3>(desc, exp); break;
                        case VFXValueType.Float4: SetValueDesc<Vector4>(desc, exp); break;
                        case VFXValueType.Int32: SetValueDesc<int>(desc, exp); break;
                        case VFXValueType.Uint32: SetValueDesc<uint>(desc, exp); break;
                        case VFXValueType.Texture2D:
                        case VFXValueType.Texture2DArray:
                        case VFXValueType.Texture3D:
                        case VFXValueType.TextureCube:
                        case VFXValueType.TextureCubeArray:
                            SetValueDesc<Texture>(desc, exp);
                            break;
                        case VFXValueType.Matrix4x4: SetValueDesc<Matrix4x4>(desc, exp); break;
                        case VFXValueType.Curve: SetValueDesc<AnimationCurve>(desc, exp); break;
                        case VFXValueType.ColorGradient: SetValueDesc<Gradient>(desc, exp); break;
                        case VFXValueType.Mesh: SetValueDesc<Mesh>(desc, exp); break;
                        case VFXValueType.Boolean: SetValueDesc<bool>(desc, exp); break;
                        default: throw new InvalidOperationException("Invalid type");
                    }
                }
            }

            m_Graph.visualEffectResource.SetValueSheet(m_ExpressionValues);
        }

        public VisualEffectResource visualEffectResource
        {
            get
            {
                if (m_Graph != null)
                {
                    return m_Graph.visualEffectResource;
                }
                return null;
            }
        }

        private VFXGraph m_Graph;

        [NonSerialized]
        private VFXExpressionGraph m_ExpressionGraph;
        [NonSerialized]
        private VFXExpressionValueContainerDesc[] m_ExpressionValues;
    }
}
