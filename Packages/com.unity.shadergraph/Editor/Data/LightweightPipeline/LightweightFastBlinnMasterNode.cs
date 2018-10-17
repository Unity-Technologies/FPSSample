using System;

namespace UnityEditor.ShaderGraph
{
/*    [Serializable]
    [Title("Master", "Lightweight", "Fast Blinn")]
    public class LightweightFastBlinnMasterNode : AbstractMasterNode
    {
        public const string AlbedoSlotName = "Diffuse";
        public const string SpecularSlotName = "Specular";
        public const string ShininessSlotName = "Shininess";
        public const string GlossinessSlotName = "Glossiness";
        public const string NormalSlotName = "Normal";
        public const string EmissionSlotName = "Emission";
        public const string AlphaSlotName = "Alpha";
        public const string VertexOffsetName = "VertexPosition";

        public const int AlbedoSlotId = 0;
        public const int SpecularSlotId = 1;
        public const int ShininessSlotId = 3;
        public const int GlossinessSlotId = 4;
        public const int NormalSlotId = 5;
        public const int EmissionSlotId = 6;
        public const int AlphaSlotId = 7;
        public const int VertexOffsetId = 8;

        [SerializeField]
        private SurfaceMaterialOptions m_MaterialOptions = new SurfaceMaterialOptions();

        public SurfaceMaterialOptions options
        {
            get { return m_MaterialOptions; }
        }

        public LightweightFastBlinnMasterNode()
        {
            name = "LightweightFastBlinnMasterNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(VertexOffsetId, VertexOffsetName, VertexOffsetName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Vertex));
            AddSlot(new MaterialSlot(AlbedoSlotId, AlbedoSlotName, AlbedoSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(SpecularSlotId, SpecularSlotName, SpecularSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(ShininessSlotId, ShininessSlotName, ShininessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(GlossinessSlotId, GlossinessSlotName, GlossinessSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                new[]
            {
                AlbedoSlotId,
                NormalSlotId,
                EmissionSlotId,
                SpecularSlotId,
                ShininessSlotId,
                GlossinessSlotId,
                AlphaSlotId,
                VertexOffsetId
            });
        }

        protected int[] surfaceInputs
        {
            get
            {
                return new[]
                {
                    AlbedoSlotId,
                    NormalSlotId,
                    EmissionSlotId,
                    SpecularSlotId,
                    ShininessSlotId,
                    GlossinessSlotId,
                    AlphaSlotId,
                };
            }
        }

        protected int[] vertexInputs
        {
            get
            {
                return new[]
                {
                    VertexOffsetId
                };
            }
        }

        void GenerateNodeFunctionsAndPropertyUsages(
            ShaderGenerator shaderBody,
            ShaderGenerator propertyUsages,
            ShaderGenerator nodeFunction,
            GenerationMode mode,
            int[] validNodeIds)
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, NodeUtils.IncludeSelf.Include,
                new List<int>(validNodeIds));

            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (node is IGeneratesFunction)
                    (node as IGeneratesFunction).GenerateNodeFunction(nodeFunction, mode);

                node.GeneratePropertyUsages(propertyUsages, mode);
            }

            var nodes = ListPool<INode>.Get();
            //Get the rest of the nodes for all the other slots
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, NodeUtils.IncludeSelf.Exclude, new List<int>(vertexInputs));
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, mode);
            }
            ListPool<INode>.Release(nodes);
        }

        void GenerateVertexShaderInternal(
            ShaderGenerator propertyUsages,
            ShaderGenerator shaderBody,
            ShaderGenerator nodeFunction,
            ShaderGenerator vertexShaderBlock,
            GenerationMode mode)
        {
            GenerateNodeFunctionsAndPropertyUsages(vertexShaderBlock, propertyUsages, nodeFunction, mode, vertexInputs);

            var slot = FindInputSlot<MaterialSlot>(VertexOffsetId);
            foreach (var edge in owner.GetEdges(slot.slotReference))
            {
                var outputRef = edge.outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                if (fromNode == null)
                    continue;

                var remapper = fromNode as INodeGroupRemapper;
                if (remapper != null && !remapper.IsValidSlotConnection(outputRef.slotId))
                    continue;

                vertexShaderBlock.AddShaderChunk("v.vertex.xyz += " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
            }
        }

        public override string GetSubShader(GenerationMode mode, PropertyGenerator shaderPropertiesVisitor)
        {
            var templateLocation = ShaderGenerator.GetTemplatePath("lightweightSubshaderFastBlinn.template");

            if (!File.Exists(templateLocation))
                return string.Empty;

            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this);
            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
                node.GeneratePropertyBlock(shaderPropertiesVisitor, mode);

            var templateText = File.ReadAllText(templateLocation);
            var shaderBodyVisitor = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var shaderPropertyUsagesVisitor = new ShaderGenerator();
            var shaderInputVisitor = new ShaderGenerator();
            var shaderOutputVisitor = new ShaderGenerator();
            var vertexShaderBlock = new ShaderGenerator();
            var definesVisitor = new ShaderGenerator();

            GenerateSurfaceShaderInternal(
                shaderPropertyUsagesVisitor,
                shaderBodyVisitor,
                shaderFunctionVisitor,
                shaderInputVisitor,
                shaderOutputVisitor,
                vertexShaderBlock,
                definesVisitor,
                mode);

            GenerateVertexShaderInternal(
                shaderPropertyUsagesVisitor,
                shaderBodyVisitor,
                shaderFunctionVisitor,
                vertexShaderBlock,
                mode);

            var tagsVisitor = new ShaderGenerator();
            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();

            m_MaterialOptions.GetTags(tagsVisitor);
            m_MaterialOptions.GetBlend(blendingVisitor);
            m_MaterialOptions.GetCull(cullingVisitor);
            m_MaterialOptions.GetDepthTest(zTestVisitor);
            m_MaterialOptions.GetDepthWrite(zWriteVisitor);

            GetDefines(definesVisitor);

            var resultShader = templateText.Replace("${ShaderPropertyUsages}", shaderPropertyUsagesVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ShaderFunctions}", shaderFunctionVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${VertexInputs}", shaderInputVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${VertexOutputs}", shaderOutputVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${PixelShaderBody}", shaderBodyVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${LOD}", "" + m_MaterialOptions.lod);

            resultShader = resultShader.Replace("${Defines}", definesVisitor.GetShaderString(2));

            resultShader = resultShader.Replace("${VertexShaderBody}", vertexShaderBlock.GetShaderString(3));

            return resultShader;
        }

        public void GetDefines(ShaderGenerator visitor)
        {
            visitor.AddShaderChunk("#define _GLOSSYREFLECTIONS_ON", true);
            visitor.AddShaderChunk("#define _SPECULARHIGHLIGHTS_ON", true);
        }

        public override string GetFullShader(GenerationMode mode, string name, out List<PropertyGenerator.TextureInfo> configuredTextures)
        {
            var templateLocation = ShaderGenerator.GetTemplatePath("shader.template");

            if (!File.Exists(templateLocation))
            {
                configuredTextures = new List<PropertyGenerator.TextureInfo>();
                return string.Empty;
            }

            var templateText = File.ReadAllText(templateLocation);

            var shaderPropertiesVisitor = new PropertyGenerator();
            var resultShader = templateText.Replace("${ShaderName}", name);
            resultShader = resultShader.Replace("${SubShader}", GetSubShader(mode, shaderPropertiesVisitor));
            resultShader = resultShader.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(2));
            configuredTextures = shaderPropertiesVisitor.GetConfiguredTexutres();

            Debug.Log(resultShader);

            return Regex.Replace(resultShader, @"\r\n|\n\r|\n|\r", Environment.NewLine);
        }

        private void GenerateSurfaceShaderInternal(
            ShaderGenerator propertyUsages,
            ShaderGenerator shaderBody,
            ShaderGenerator nodeFunction,
            ShaderGenerator shaderInputVisitor,
            ShaderGenerator shaderOutputVisitor,
            ShaderGenerator vertexShaderBlock,
            ShaderGenerator definesVisitor,
            GenerationMode mode)
        {
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, this, NodeUtils.IncludeSelf.Include,
                new List<int>(surfaceInputs));

            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (node is IGeneratesFunction)
                {
                    ((IGeneratesFunction)node).GenerateNodeFunction(nodeFunction, mode);
                }

                node.GeneratePropertyUsages(propertyUsages, mode);
            }

            int vertInputIndex = 2;
            int vertOutputIndex = 5;

            shaderInputVisitor.AddShaderChunk("half4 texcoord1 : TEXCOORD1;", true);

            bool requiresBitangent = activeNodeList.OfType<IMayRequireBitangent>().Any(x => x.RequiresBitangent());
            bool requiresTangent = activeNodeList.OfType<IMayRequireTangent>().Any(x => x.RequiresTangent());
            bool requiresViewDirTangentSpace = activeNodeList.OfType<IMayRequireViewDirectionTangentSpace>().Any(x => x.RequiresViewDirectionTangentSpace());
            bool requiresViewDir = activeNodeList.OfType<IMayRequireViewDirection>().Any(x => x.RequiresViewDirection());
            bool requiresWorldPos = activeNodeList.OfType<IMayRequireWorldPosition>().Any(x => x.RequiresWorldPosition());
            bool requiresNormal = activeNodeList.OfType<IMayRequireNormal>().Any(x => x.RequiresNormal());
            bool requiresScreenPosition = activeNodeList.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition());
            bool requiresVertexColor = activeNodeList.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor());

            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                if (surfaceInputs.Contains(slot.id))
                {
                    foreach (var edge in owner.GetEdges(slot.slotReference))
                    {
                        var outputRef = edge.outputSlot;
                        var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                        if (fromNode == null)
                            continue;

                        var remapper = fromNode as INodeGroupRemapper;
                        if (remapper != null && !remapper.IsValidSlotConnection(outputRef.slotId))
                            continue;

                        if (slot.id == NormalSlotId)
                        {
                            requiresBitangent = true;
                            requiresTangent = true;
                            definesVisitor.AddShaderChunk("#define _NORMALMAP 1", true);
                        }
                    }
                }
            }

            for (int uvIndex = 0; uvIndex < ShaderGeneratorNames.UVCount; ++uvIndex)
            {
                var channel = (UVChannel)uvIndex;
                if (activeNodeList.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel)))
                {
                    if(uvIndex != 0)
                    {
                        shaderInputVisitor.AddShaderChunk(string.Format("half4 texcoord{0} : TEXCOORD{1};", uvIndex, vertInputIndex), true);
                        shaderOutputVisitor.AddShaderChunk(string.Format("half4 meshUV{0} : TEXCOORD{1};", uvIndex, vertOutputIndex), true);
                        vertexShaderBlock.AddShaderChunk(string.Format("o.meshUV{0} = v.texcoord{1};", uvIndex, uvIndex), true);
                        vertInputIndex++;
                        vertOutputIndex++;
                    }
                    shaderBody.AddShaderChunk(string.Format("half4 {0} = i.meshUV{1};", channel.GetUVName(), uvIndex), true);
                }
            }

            if (requiresViewDir || requiresViewDirTangentSpace)
            {
                shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceViewDirection + " = i.viewDir;", true);
            }

            if (requiresWorldPos)
            {
                shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpacePosition + " = i.posWS;", true);
            }

            if (requiresScreenPosition)
            {
                shaderOutputVisitor.AddShaderChunk(string.Format("half4 screenPos : TEXCOORD{0};", vertOutputIndex), true);
                vertexShaderBlock.AddShaderChunk("o.screenPos = ComputeScreenPos(v.vertex);", true);
                shaderBody.AddShaderChunk("float4 " + ShaderGeneratorNames.ScreenPosition + " = i.screenPos;", true);
                vertOutputIndex++;
            }

            if (requiresBitangent || requiresTangent || requiresViewDirTangentSpace)
            {
                shaderOutputVisitor.AddShaderChunk(string.Format("half3 tangent : TEXCOORD{0}; \\", vertOutputIndex), true);
                vertexShaderBlock.AddShaderChunk("o.tangent = normalize(UnityObjectToWorldDir(v.tangent)); \\", true);
                shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceTangent + " = normalize(i.tangent.xyz);", true);
                vertOutputIndex++;
            }

            if (requiresBitangent || requiresNormal || requiresViewDirTangentSpace)
            {
                shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceNormal + " = normalize(i.normal);", true);
            }

            if (requiresBitangent || requiresViewDirTangentSpace)
            {
                shaderOutputVisitor.AddShaderChunk(string.Format("half3 binormal : TEXCOORD{0};", vertOutputIndex), true);
                vertexShaderBlock.AddShaderChunk("o.binormal = cross(o.normal, o.tangent) * v.tangent.w;", true);
                shaderBody.AddShaderChunk("float3 " + ShaderGeneratorNames.WorldSpaceBitangent + " = i.binormal;", true);
                vertOutputIndex++;
            }

            if (requiresViewDirTangentSpace)
            {
                shaderBody.AddShaderChunk(
                    "float3 " + ShaderGeneratorNames.TangentSpaceViewDirection + ";", true);

                shaderBody.AddShaderChunk(
                    ShaderGeneratorNames.TangentSpaceViewDirection + ".x = dot(" +
                    ShaderGeneratorNames.WorldSpaceViewDirection + "," +
                    ShaderGeneratorNames.WorldSpaceTangent + ");", true);

                shaderBody.AddShaderChunk(
                    ShaderGeneratorNames.TangentSpaceViewDirection + ".y = dot(" +
                    ShaderGeneratorNames.WorldSpaceViewDirection + "," +
                    ShaderGeneratorNames.WorldSpaceBitangent + ");", true);

                shaderBody.AddShaderChunk(
                    ShaderGeneratorNames.TangentSpaceViewDirection + ".z = dot(" +
                    ShaderGeneratorNames.WorldSpaceViewDirection + "," +
                    ShaderGeneratorNames.WorldSpaceNormal + ");", true);
            }

            if (requiresVertexColor)
            {
                shaderOutputVisitor.AddShaderChunk(string.Format("half4 color : TEXCOORD{0};", vertOutputIndex), true);
                shaderBody.AddShaderChunk("float4 " + ShaderGeneratorNames.VertexColor + " = i.color;", true);
                vertInputIndex++;
                vertOutputIndex++;
            }

            GenerateNodeCode(shaderBody, propertyUsages, mode);
        }

        public void GenerateNodeCode(ShaderGenerator shaderBody, ShaderGenerator propertyUsages, GenerationMode generationMode)
        {
            var nodes = ListPool<INode>.Get();

            //Get the rest of the nodes for all the other slots
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, NodeUtils.IncludeSelf.Exclude, new List<int>(surfaceInputs));
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }
            ListPool<INode>.Release(nodes);

            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                if (surfaceInputs.Contains(slot.id))
                {
                    foreach (var edge in owner.GetEdges(slot.slotReference))
                    {
                        var outputRef = edge.outputSlot;
                        var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                        if (fromNode == null)
                            continue;

                        var remapper = fromNode as INodeGroupRemapper;
                        if (remapper != null && !remapper.IsValidSlotConnection(outputRef.slotId))
                            continue;

                        shaderBody.AddShaderChunk("o." + slot.shaderOutputName + " = " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);

                        if (slot.id == NormalSlotId)
                            shaderBody.AddShaderChunk("o." + slot.shaderOutputName + " += 1e-6;", true);

                        if (slot.id == AlphaSlotId)
                            propertyUsages.AddShaderChunk("#define _ALPHAPREMULTIPLY_ON", true);
                    }
                }
            }
        }
    }*/
}
