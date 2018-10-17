using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum MetalMaterialType
    {
        Iron,
        Silver,
        Aluminium,
        Gold,
        Copper,
        Chromium,
        Nickel,
        Titanium,
        Cobalt,
        Platinum
    };

    [Title("Input", "PBR", "Metal Reflectance")]
    public class MetalReflectanceNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public MetalReflectanceNode()
        {
            name = "Metal Reflectance";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Metal-Reflectance-Node"; }
        }

        [SerializeField]
        private MetalMaterialType m_Material = MetalMaterialType.Iron;

        [EnumControl("Material")]
        public MetalMaterialType material
        {
            get { return m_Material; }
            set
            {
                if (m_Material == value)
                    return;

                m_Material = value;
                Dirty(ModificationScope.Graph);
            }
        }

        static Dictionary<MetalMaterialType, string> m_MaterialList = new Dictionary<MetalMaterialType, string>
        {
            {MetalMaterialType.Iron, "(0.560, 0.570, 0.580)"},
            {MetalMaterialType.Silver, "(0.972, 0.960, 0.915)"},
            {MetalMaterialType.Aluminium, "(0.913, 0.921, 0.925)"},
            {MetalMaterialType.Gold, "(1.000, 0.766, 0.336)"},
            {MetalMaterialType.Copper, "(0.955, 0.637, 0.538)"},
            {MetalMaterialType.Chromium, "(0.550, 0.556, 0.554)"},
            {MetalMaterialType.Nickel, "(0.660, 0.609, 0.526)"},
            {MetalMaterialType.Titanium, "(0.542, 0.497, 0.449)"},
            {MetalMaterialType.Cobalt, "(0.662, 0.655, 0.634)"},
            {MetalMaterialType.Platinum, "(0.672, 0.637, 0.585)"}
        };

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            visitor.AddShaderChunk(string.Format("{0}3 {1} = {0}3{2};", precision, GetVariableNameForSlot(kOutputSlotId), m_MaterialList[material].ToString(CultureInfo.InvariantCulture)), true);
        }
    }
}
