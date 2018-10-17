using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Matrix", "Matrix 3x3")]
    public class Matrix3Node : AbstractMaterialNode, IGeneratesBodyCode
    {
        public const int OutputSlotId = 0;
        const string kOutputSlotName = "Out";

        [SerializeField]
        Vector3 m_Row0;

        [SerializeField]
        Vector3 m_Row1;

        [SerializeField]
        Vector3 m_Row2;

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector3 row0
        {
            get { return m_Row0; }
            set { SetRow(ref m_Row0, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector3 row1
        {
            get { return m_Row1; }
            set { SetRow(ref m_Row1, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector3 row2
        {
            get { return m_Row2; }
            set { SetRow(ref m_Row2, value); }
        }

        void SetRow(ref Vector3 row, Vector3 value)
        {
            if (value == row)
                return;
            row = value;
            Dirty(ModificationScope.Node);
        }

        public Matrix3Node()
        {
            name = "Matrix 3x3";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Matrix-3x3-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Matrix3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            properties.AddShaderProperty(new Vector3ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_m0", GetVariableNameForNode()),
                generatePropertyBlock = false,
                value = m_Row0
            });

            properties.AddShaderProperty(new Vector3ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_m1", GetVariableNameForNode()),
                generatePropertyBlock = false,
                value = m_Row1
            });

            properties.AddShaderProperty(new Vector3ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_m2", GetVariableNameForNode()),
                generatePropertyBlock = false,
                value = m_Row2
            });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();
            if (!generationMode.IsPreview())
            {
                sb.AppendLine("{0}3 _{1}_m0 = {0}3 ({2}, {3}, {4});", precision, GetVariableNameForNode(),
                    NodeUtils.FloatToShaderValue(m_Row0.x),
                    NodeUtils.FloatToShaderValue(m_Row0.y),
                    NodeUtils.FloatToShaderValue(m_Row0.z));
                sb.AppendLine("{0}3 _{1}_m1 = {0}3 ({2}, {3}, {4});", precision, GetVariableNameForNode(),
                    NodeUtils.FloatToShaderValue(m_Row1.x),
                    NodeUtils.FloatToShaderValue(m_Row1.y),
                    NodeUtils.FloatToShaderValue(m_Row1.z));
                sb.AppendLine("{0}3 _{1}_m2 = {0}3 ({2}, {3}, {4});", precision, GetVariableNameForNode(),
                    NodeUtils.FloatToShaderValue(m_Row2.x),
                    NodeUtils.FloatToShaderValue(m_Row2.y),
                    NodeUtils.FloatToShaderValue(m_Row2.z));
            }
            sb.AppendLine("{0}3x3 {1} = {0}3x3 (_{1}_m0.x, _{1}_m0.y, _{1}_m0.z, _{1}_m1.x, _{1}_m1.y, _{1}_m1.z, _{1}_m2.x, _{1}_m2.y, _{1}_m2.z);",
                precision, GetVariableNameForNode());
            visitor.AddShaderChunk(sb.ToString(), false);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Vector3)
            {
                name = string.Format("_{0}_m0", GetVariableNameForNode()),
                vector4Value = m_Row0
            });

            properties.Add(new PreviewProperty(PropertyType.Vector3)
            {
                name = string.Format("_{0}_m1", GetVariableNameForNode()),
                vector4Value = m_Row1
            });

            properties.Add(new PreviewProperty(PropertyType.Vector3)
            {
                name = string.Format("_{0}_m2", GetVariableNameForNode()),
                vector4Value = m_Row2
            });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }
    }
}
