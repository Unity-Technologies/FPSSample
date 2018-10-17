using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Matrix", "Matrix 2x2")]
    public class Matrix2Node : AbstractMaterialNode, IGeneratesBodyCode
    {
        public const int OutputSlotId = 0;
        const string kOutputSlotName = "Out";

        [SerializeField]
        Vector2 m_Row0;

        [SerializeField]
        Vector2 m_Row1;

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector2 row0
        {
            get { return m_Row0; }
            set { SetRow(ref m_Row0, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector2 row1
        {
            get { return m_Row1; }
            set { SetRow(ref m_Row1, value); }
        }

        void SetRow(ref Vector2 row, Vector2 value)
        {
            if (value == row)
                return;
            row = value;
            Dirty(ModificationScope.Node);
        }

        public Matrix2Node()
        {
            name = "Matrix 2x2";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Matrix-2x2-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Matrix2MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            properties.AddShaderProperty(new Vector2ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_m0", GetVariableNameForNode()),
                generatePropertyBlock = false,
                value = m_Row0
            });

            properties.AddShaderProperty(new Vector2ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_m1", GetVariableNameForNode()),
                generatePropertyBlock = false,
                value = m_Row1
            });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();
            if (!generationMode.IsPreview())
            {
                sb.AppendLine("{0}2 _{1}_m0 = {0}2 ({2}, {3});", precision, GetVariableNameForNode(),
                    NodeUtils.FloatToShaderValue(m_Row0.x),
                    NodeUtils.FloatToShaderValue(m_Row0.y));
                sb.AppendLine("{0}2 _{1}_m1 = {0}2 ({2}, {3});", precision, GetVariableNameForNode(),
                    NodeUtils.FloatToShaderValue(m_Row1.x),
                    NodeUtils.FloatToShaderValue(m_Row1.y));
            }
            sb.AppendLine("{0}2x2 {1} = {0}2x2 (_{1}_m0.x, _{1}_m0.y, _{1}_m1.x, _{1}_m1.y);",
                precision, GetVariableNameForNode());
            visitor.AddShaderChunk(sb.ToString(), false);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Vector2)
            {
                name = string.Format("_{0}_m0", GetVariableNameForNode()),
                vector4Value = m_Row0
            });

            properties.Add(new PreviewProperty(PropertyType.Vector2)
            {
                name = string.Format("_{0}_m1", GetVariableNameForNode()),
                vector4Value = m_Row1
            });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }
    }
}
