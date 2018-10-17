using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public enum DielectricMaterialType
    {
        Common,
        RustedMetal,
        Water,
        Ice,
        Glass,
        Custom
    };

    [Title("Input", "PBR", "Dielectric Specular")]
    public class DielectricSpecularNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public DielectricSpecularNode()
        {
            name = "Dielectric Specular";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Dielectric-Specular-Node"; }
        }

        [SerializeField]
        DielectricMaterial m_Material = new DielectricMaterial(DielectricMaterialType.Common, 0.5f, 1.0f);

        [Serializable]
        public struct DielectricMaterial
        {
            public DielectricMaterialType type;
            public float range;
            public float indexOfRefraction;

            public DielectricMaterial(DielectricMaterialType type, float range, float indexOfRefraction)
            {
                this.type = type;
                this.range = range;
                this.indexOfRefraction = indexOfRefraction;
            }
        }

        [DielectricSpecularControl()]
        public DielectricMaterial material
        {
            get { return m_Material; }
            set
            {
                if ((value.type == m_Material.type) && (value.range == m_Material.range) && (value.indexOfRefraction == m_Material.indexOfRefraction))
                    return;
                DielectricMaterialType previousType = m_Material.type;
                m_Material = value;
                if (value.type != previousType)
                    Dirty(ModificationScope.Graph);
                else
                    Dirty(ModificationScope.Node);
            }
        }

        static Dictionary<DielectricMaterialType, string> m_MaterialList = new Dictionary<DielectricMaterialType, string>
        {
            {DielectricMaterialType.RustedMetal, "(0.030, 0.030, 0.030)"},
            {DielectricMaterialType.Water, "(0.020, 0.020, 0.020)"},
            {DielectricMaterialType.Ice, "(0.018, 0.018, 0.018)"},
            {DielectricMaterialType.Glass, "(0.040, 0.040, 0.040)"}
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
            var sb = new ShaderStringBuilder();
            if (!generationMode.IsPreview())
            {
                switch (material.type)
                {
                    case DielectricMaterialType.Custom:
                        sb.AppendLine("{0} _{1}_IOR = {2};", precision, GetVariableNameForNode(), material.indexOfRefraction);
                        break;
                    case DielectricMaterialType.Common:
                        sb.AppendLine("{0} _{1}_Range = {2};", precision, GetVariableNameForNode(), material.range);
                        break;
                    default:
                        break;
                }
            }
            switch (material.type)
            {
                case DielectricMaterialType.Common:
                    sb.AppendLine("{0}3 {1} = lerp(0.034, 0.048, _{2}_Range);", precision, GetVariableNameForSlot(kOutputSlotId), GetVariableNameForNode());
                    break;
                case DielectricMaterialType.Custom:
                    sb.AppendLine("{0}3 {1} = pow(_{2}_IOR - 1, 2) / pow(_{2}_IOR + 1, 2);", precision, GetVariableNameForSlot(kOutputSlotId), GetVariableNameForNode());
                    break;
                default:
                    sb.AppendLine("{0}3 {1} = {0}3{2};", precision, GetVariableNameForSlot(kOutputSlotId), m_MaterialList[material.type].ToString(CultureInfo.InvariantCulture));
                    break;
            }
            visitor.AddShaderChunk(sb.ToString(), false);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            if (material.type == DielectricMaterialType.Common)
            {
                properties.Add(new PreviewProperty(PropertyType.Vector1)
                {
                    name = string.Format("_{0}_Range", GetVariableNameForNode()),
                    floatValue = material.range
                });
            }
            else if (material.type == DielectricMaterialType.Custom)
            {
                properties.Add(new PreviewProperty(PropertyType.Vector1)
                {
                    name = string.Format("_{0}_IOR", GetVariableNameForNode()),
                    floatValue = material.indexOfRefraction
                });
            }
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);

            if (material.type == DielectricMaterialType.Common)
            {
                properties.AddShaderProperty(new Vector1ShaderProperty()
                {
                    overrideReferenceName = string.Format("_{0}_Range", GetVariableNameForNode()),
                    generatePropertyBlock = false
                });
            }
            else if (material.type == DielectricMaterialType.Custom)
            {
                properties.AddShaderProperty(new Vector1ShaderProperty()
                {
                    overrideReferenceName = string.Format("_{0}_IOR", GetVariableNameForNode()),
                    generatePropertyBlock = false
                });
            }
        }
    }
}
