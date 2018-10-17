using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Sub-graph")]
    public class SubGraphNode : AbstractMaterialNode
        , IGeneratesBodyCode
        , IOnAssetEnabled
        , IGeneratesFunction
        , IMayRequireNormal
        , IMayRequireTangent
        , IMayRequireBitangent
        , IMayRequireMeshUV
        , IMayRequireScreenPosition
        , IMayRequireViewDirection
        , IMayRequirePosition
        , IMayRequireVertexColor
        , IMayRequireTime
    {
        [SerializeField]
        private string m_SerializedSubGraph = string.Empty;

        [NonSerialized]
        MaterialSubGraphAsset m_SubGraph;

        [Serializable]
        private class SubGraphHelper
        {
            public MaterialSubGraphAsset subGraph;
        }

        protected SubGraph referencedGraph
        {
            get
            {
                if (subGraphAsset == null)
                    return null;

                return subGraphAsset.subGraph;
            }
        }

        public MaterialSubGraphAsset subGraphAsset
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerializedSubGraph))
                    return null;

                if (m_SubGraph == null)
                {
                    var helper = new SubGraphHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedSubGraph, helper);
                    m_SubGraph = helper.subGraph;
                }

                return m_SubGraph;
            }
            set
            {
                if (subGraphAsset == value)
                    return;

                var helper = new SubGraphHelper();
                helper.subGraph = value;
                m_SerializedSubGraph = EditorJsonUtility.ToJson(helper, true);
                m_SubGraph = null;
                UpdateSlots();

                Dirty(ModificationScope.Topological);
            }
        }

        public INode outputNode
        {
            get
            {
                if (subGraphAsset != null && subGraphAsset.subGraph != null)
                    return subGraphAsset.subGraph.outputNode;
                return null;
            }
        }

        public override bool hasPreview
        {
            get { return referencedGraph != null; }
        }

        public override PreviewMode previewMode
        {
            get
            {
                if (referencedGraph == null)
                    return PreviewMode.Preview2D;

                return PreviewMode.Preview3D;
            }
        }

        public SubGraphNode()
        {
            name = "Sub-graph";
        }

        public override bool allowedInSubGraph
        {
            get { return false; }
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Sub-graph-Node"; }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            if (referencedGraph == null)
                return;

            foreach (var outSlot in referencedGraph.graphOutputs)
                visitor.AddShaderChunk(string.Format("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(precision, outSlot.concreteValueType), GetVariableNameForSlot(outSlot.id)), true);

            var arguments = new List<string>();
            foreach (var prop in referencedGraph.graphInputs)
            {
                var inSlotId = prop.guid.GetHashCode();

                if (prop is TextureShaderProperty)
                    arguments.Add(string.Format("TEXTURE2D_PARAM({0}, sampler{0})", GetSlotValue(inSlotId, generationMode)));
                else if (prop is Texture2DArrayShaderProperty)
                    arguments.Add(string.Format("TEXTURE2D_ARRAY_PARAM({0}, sampler{0})", GetSlotValue(inSlotId, generationMode)));
                else if (prop is Texture3DShaderProperty)
                    arguments.Add(string.Format("TEXTURE3D_PARAM({0}, sampler{0})", GetSlotValue(inSlotId, generationMode)));
                else if (prop is CubemapShaderProperty)
                    arguments.Add(string.Format("TEXTURECUBE_PARAM({0}, sampler{0})", GetSlotValue(inSlotId, generationMode)));
                else
                    arguments.Add(GetSlotValue(inSlotId, generationMode));
            }

            // pass surface inputs through
            arguments.Add("IN");

            foreach (var outSlot in referencedGraph.graphOutputs)
                arguments.Add(GetVariableNameForSlot(outSlot.id));

            visitor.AddShaderChunk(
                string.Format("{0}({1});"
                    , SubGraphFunctionName(graphContext)
                    , arguments.Aggregate((current, next) => string.Format("{0}, {1}", current, next)))
                , false);
        }

        public void OnEnable()
        {
            UpdateSlots();
        }

        public virtual void UpdateSlots()
        {
            var validNames = new List<int>();
            if (referencedGraph == null)
            {
                RemoveSlotsNameNotMatching(validNames, true);
                return;
            }

            var props = referencedGraph.properties;
            foreach (var prop in props)
            {
                var propType = prop.propertyType;
                SlotValueType slotType;

                switch (propType)
                {
                    case PropertyType.Color:
                        slotType = SlotValueType.Vector4;
                        break;
                    case PropertyType.Texture2D:
                        slotType = SlotValueType.Texture2D;
                        break;
                    case PropertyType.Texture2DArray:
                        slotType = SlotValueType.Texture2DArray;
                        break;
                    case PropertyType.Texture3D:
                        slotType = SlotValueType.Texture3D;
                        break;
                    case PropertyType.Cubemap:
                        slotType = SlotValueType.Cubemap;
                        break;
                    case PropertyType.Gradient:
                        slotType = SlotValueType.Gradient;
                        break;
                    case PropertyType.Vector1:
                        slotType = SlotValueType.Vector1;
                        break;
                    case PropertyType.Vector2:
                        slotType = SlotValueType.Vector2;
                        break;
                    case PropertyType.Vector3:
                        slotType = SlotValueType.Vector3;
                        break;
                    case PropertyType.Vector4:
                        slotType = SlotValueType.Vector4;
                        break;
                    case PropertyType.Boolean:
                        slotType = SlotValueType.Boolean;
                        break;
                    case PropertyType.Matrix2:
                        slotType = SlotValueType.Matrix2;
                        break;
                    case PropertyType.Matrix3:
                        slotType = SlotValueType.Matrix3;
                        break;
                    case PropertyType.Matrix4:
                        slotType = SlotValueType.Matrix4;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var id = prop.guid.GetHashCode();
                MaterialSlot slot = MaterialSlot.CreateMaterialSlot(slotType, id, prop.displayName, prop.referenceName, SlotType.Input, prop.defaultValue, ShaderStageCapability.All);
                // copy default for texture for niceness
                if (slotType == SlotValueType.Texture2D && propType == PropertyType.Texture2D)
                {
                    var tSlot = slot as Texture2DInputMaterialSlot;
                    var tProp = prop as TextureShaderProperty;
                    if (tSlot != null && tProp != null)
                        tSlot.texture = tProp.value.texture;
                }
                // copy default for texture array for niceness
                else if (slotType == SlotValueType.Texture2DArray && propType == PropertyType.Texture2DArray)
                {
                    var tSlot = slot as Texture2DArrayInputMaterialSlot;
                    var tProp = prop as Texture2DArrayShaderProperty;
                    if (tSlot != null && tProp != null)
                        tSlot.textureArray = tProp.value.textureArray;
                }
                // copy default for texture 3d for niceness
                else if (slotType == SlotValueType.Texture3D && propType == PropertyType.Texture3D)
                {
                    var tSlot = slot as Texture3DInputMaterialSlot;
                    var tProp = prop as Texture3DShaderProperty;
                    if (tSlot != null && tProp != null)
                        tSlot.texture = tProp.value.texture;
                }
                // copy default for cubemap for niceness
                else if (slotType == SlotValueType.Cubemap && propType == PropertyType.Cubemap)
                {
                    var tSlot = slot as CubemapInputMaterialSlot;
                    var tProp = prop as CubemapShaderProperty;
                    if (tSlot != null && tProp != null)
                        tSlot.cubemap = tProp.value.cubemap;
                }
                AddSlot(slot);
                validNames.Add(id);
            }

            if (outputNode != null)
            {
                var outputStage = ((SubGraphOutputNode)outputNode).effectiveShaderStage;

                foreach (var slot in NodeExtensions.GetInputSlots<MaterialSlot>(outputNode))
                {
                    AddSlot(MaterialSlot.CreateMaterialSlot(slot.valueType, slot.id, slot.RawDisplayName(), 
                        slot.shaderOutputName, SlotType.Output, Vector4.zero, outputStage));
                    validNames.Add(slot.id);
                }
            }

            RemoveSlotsNameNotMatching(validNames);
        }

        private void ValidateShaderStage()
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetInputSlots(slots);
            GetOutputSlots(slots);

            var subGraphOutputNode = outputNode;
            if (outputNode != null)
            {
                var outputStage = ((SubGraphOutputNode)subGraphOutputNode).effectiveShaderStage;
                foreach(MaterialSlot slot in slots)
                    slot.stageCapability = outputStage;
            }

            ShaderStageCapability effectiveStage = ShaderStageCapability.All;

            foreach(MaterialSlot slot in slots)
            {
                ShaderStageCapability stage = NodeUtils.GetEffectiveShaderStageCapability(slot, slot.slotType == SlotType.Output);

                if(stage != ShaderStageCapability.All)
                {
                    effectiveStage = stage;
                    break;
                }
            }
            
            foreach(MaterialSlot slot in slots)
                slot.stageCapability = effectiveStage;
        }

        public override void ValidateNode()
        {
            if (referencedGraph != null)
            {
                referencedGraph.OnEnable();
                referencedGraph.ValidateGraph();

                if (referencedGraph.GetNodes<INode>().Any(x => x.hasError))
                    hasError = true;
            }

            ValidateShaderStage();

            base.ValidateNode();
        }

        public override void CollectShaderProperties(PropertyCollector visitor, GenerationMode generationMode)
        {
            base.CollectShaderProperties(visitor, generationMode);

            if (referencedGraph == null)
                return;

            referencedGraph.CollectShaderProperties(visitor, generationMode);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            if (referencedGraph == null)
                return;

            properties.AddRange(referencedGraph.GetPreviewProperties());
        }

        private string SubGraphFunctionName(GraphContext graphContext)
        {
            var functionName = subGraphAsset != null ? NodeUtils.GetHLSLSafeName(subGraphAsset.name) : "ERROR";
            return string.Format("sg_{0}_{1}_{2}", functionName, graphContext.graphInputStructName, GuidEncoder.Encode(referencedGraph.guid));
        }

        public virtual void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            if (subGraphAsset == null || referencedGraph == null)
                return;

            referencedGraph.GenerateNodeFunction(registry, graphContext, generationMode);
            referencedGraph.GenerateSubGraphFunction(SubGraphFunctionName(graphContext), registry, graphContext, ShaderGraphRequirements.FromNodes(new List<INode> {this}), generationMode);
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            if (referencedGraph == null)
                return NeededCoordinateSpace.None;

            return referencedGraph.activeNodes.OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
                {
                    mask |= node.RequiresNormal(stageCapability);
                    return mask;
                });
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            if (referencedGraph == null)
                return false;

            return referencedGraph.activeNodes.OfType<IMayRequireMeshUV>().Any(x => x.RequiresMeshUV(channel, stageCapability));
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            if (referencedGraph == null)
                return false;

            return referencedGraph.activeNodes.OfType<IMayRequireScreenPosition>().Any(x => x.RequiresScreenPosition(stageCapability));
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            if (referencedGraph == null)
                return NeededCoordinateSpace.None;

            return referencedGraph.activeNodes.OfType<IMayRequireViewDirection>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
                {
                    mask |= node.RequiresViewDirection(stageCapability);
                    return mask;
                });
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            if (referencedGraph == null)
                return NeededCoordinateSpace.None;

            return referencedGraph.activeNodes.OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
                {
                    mask |= node.RequiresPosition(stageCapability);
                    return mask;
                });
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            if (referencedGraph == null)
                return NeededCoordinateSpace.None;

            return referencedGraph.activeNodes.OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
                {
                    mask |= node.RequiresTangent(stageCapability);
                    return mask;
                });
        }

        public bool RequiresTime()
        {
            if (referencedGraph == null)
                return false;

            return referencedGraph.activeNodes.OfType<IMayRequireTime>().Any(x => x.RequiresTime());
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            if (referencedGraph == null)
                return NeededCoordinateSpace.None;

            return referencedGraph.activeNodes.OfType<IMayRequireBitangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) =>
                {
                    mask |= node.RequiresBitangent(stageCapability);
                    return mask;
                });
        }

        public bool RequiresVertexColor(ShaderStageCapability stageCapability)
        {
            if (referencedGraph == null)
                return false;

            return referencedGraph.activeNodes.OfType<IMayRequireVertexColor>().Any(x => x.RequiresVertexColor(stageCapability));
        }

        public override void GetSourceAssetDependencies(List<string> paths)
        {
            base.GetSourceAssetDependencies(paths);
            if (subGraphAsset != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(subGraphAsset);
                paths.Add(assetPath);
                foreach (var dependencyPath in AssetDatabase.GetDependencies(assetPath))
                    paths.Add(dependencyPath);
            }
        }
    }
}
