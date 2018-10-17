using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Flipbook")]
    public class FlipbookNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV
    {
        public FlipbookNode()
        {
            name = "Flipbook";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Flipbook-Node"; }
        }

        const int UVSlotId = 0;
        const int WidthSlotId = 1;
        const int HeightSlotId = 2;
        const int TileSlotId = 3;
        const int OutputSlotId = 4;
        const string kUVSlotName = "UV";
        const string kWidthSlotName = "Width";
        const string kHeightSlotName = "Height";
        const string kTileSlotName = "Tile";
        const string kOutputSlotName = "Out";

        public override bool hasPreview
        {
            get { return true; }
        }

        string GetFunctionName()
        {
            return "Unity_Flipbook_" + precision;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(UVSlotId, kUVSlotName, kUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(WidthSlotId, kWidthSlotName, kWidthSlotName, SlotType.Input, 1));
            AddSlot(new Vector1MaterialSlot(HeightSlotId, kHeightSlotName, kHeightSlotName, SlotType.Input, 1));
            AddSlot(new Vector1MaterialSlot(TileSlotId, kTileSlotName, kTileSlotName, SlotType.Input, 0));
            AddSlot(new Vector2MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector2.zero));
            RemoveSlotsNameNotMatching(new[] { UVSlotId, WidthSlotId, HeightSlotId, TileSlotId, OutputSlotId });
        }

        [SerializeField]
        private bool m_InvertX = false;

        [ToggleControl("Invert X")]
        public ToggleData invertX
        {
            get { return new ToggleData(m_InvertX); }
            set
            {
                if (m_InvertX == value.isOn)
                    return;
                m_InvertX = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        private bool m_InvertY = true;

        [ToggleControl("Invert Y")]
        public ToggleData invertY
        {
            get { return new ToggleData(m_InvertY); }
            set
            {
                if (m_InvertY == value.isOn)
                    return;
                m_InvertY = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();
            var uvValue = GetSlotValue(UVSlotId, generationMode);
            var widthValue = GetSlotValue(WidthSlotId, generationMode);
            var heightValue = GetSlotValue(HeightSlotId, generationMode);
            var tileValue = GetSlotValue(TileSlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);

            sb.AppendLine("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId));
            if (!generationMode.IsPreview())
            {
                sb.AppendLine("{0}2 _{1}_Invert = {0}2 ({2}, {3});", precision, GetVariableNameForNode(), invertX.isOn ? 1 : 0, invertY.isOn ? 1 : 0);
            }
            sb.AppendLine("{0}({1}, {2}, {3}, {4}, _{5}_Invert, {6});", GetFunctionName(), uvValue, widthValue, heightValue, tileValue, GetVariableNameForNode(), outputValue);

            visitor.AddShaderChunk(sb.ToString(), false);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            properties.Add(new PreviewProperty(PropertyType.Vector2)
            {
                name = string.Format("_{0}_Invert", GetVariableNameForNode()),
                vector4Value = new Vector2(invertX.isOn ? 1 : 0, invertY.isOn ? 1 : 0)
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);

            properties.AddShaderProperty(new Vector2ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_Invert", GetVariableNameForNode()),
                generatePropertyBlock = false
            });
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0} ({1} UV, {2} Width, {3} Height, {4} Tile, {5}2 Invert, out {6} Out)",
                        GetFunctionName(),
                        FindInputSlot<MaterialSlot>(UVSlotId).concreteValueType.ToString(precision),
                        FindInputSlot<MaterialSlot>(WidthSlotId).concreteValueType.ToString(precision),
                        FindInputSlot<MaterialSlot>(HeightSlotId).concreteValueType.ToString(precision),
                        FindInputSlot<MaterialSlot>(TileSlotId).concreteValueType.ToString(precision),
                        precision,
                        FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision));
                    using (s.BlockScope())
                    {
                        s.AppendLine("Tile = fmod(Tile, Width*Height);");
                        s.AppendLine("{0}2 tileCount = {0}2(1.0, 1.0) / {0}2(Width, Height);", precision);
                        s.AppendLine("{0} tileY = abs(Invert.y * Height - (floor(Tile * tileCount.x) + Invert.y * 1));", precision);
                        s.AppendLine("{0} tileX = abs(Invert.x * Width - ((Tile - Width * floor(Tile * tileCount.x)) + Invert.x * 1));", precision);
                        s.AppendLine("Out = (UV + {0}2(tileX, tileY)) * tileCount;", precision);
                    }
                });
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var slot in s_TempSlots)
            {
                if (slot.RequiresMeshUV(channel))
                    return true;
            }
            return false;
        }
    }
}
