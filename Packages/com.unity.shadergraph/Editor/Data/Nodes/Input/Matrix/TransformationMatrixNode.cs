using UnityEditor.Graphing;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public enum TransformationMatrixType
    {
        None = -1,
        ModelView,
        View,
        Projection,
        ViewProjection,
        TransposeModelView,
        InverseTransposeModelView,
        ObjectToWorld,
        WorldToObject
    };

    public enum UnityMatrixType
    {
        Model,
        InverseModel,
        View,
        InverseView,
        Projection,
        InverseProjection,
        ViewProjection,
        InverseViewProjection
    }

    [Title("Input", "Matrix", "Transformation Matrix")]
    public class TransformationMatrixNode : AbstractMaterialNode
    {
        static Dictionary<UnityMatrixType, string> m_MatrixList = new Dictionary<UnityMatrixType, string>
        {
            {UnityMatrixType.Model, "UNITY_MATRIX_M"},
            {UnityMatrixType.InverseModel, "UNITY_MATRIX_I_M"},
            {UnityMatrixType.View, "UNITY_MATRIX_V"},
            {UnityMatrixType.InverseView, "UNITY_MATRIX_I_V"},
            {UnityMatrixType.Projection, "UNITY_MATRIX_P"},
            {UnityMatrixType.InverseProjection, "UNITY_MATRIX_I_P"},
            {UnityMatrixType.ViewProjection, "UNITY_MATRIX_VP"},
            {UnityMatrixType.InverseViewProjection, "UNITY_MATRIX_I_VP"},
        };
        
        static Dictionary<TransformationMatrixType, UnityMatrixType> m_MatrixUpgrade = new Dictionary<TransformationMatrixType, UnityMatrixType>
        {
            {TransformationMatrixType.ModelView, UnityMatrixType.Model},
            {TransformationMatrixType.View, UnityMatrixType.View},
            {TransformationMatrixType.Projection, UnityMatrixType.Projection},
            {TransformationMatrixType.ViewProjection, UnityMatrixType.ViewProjection},
            {TransformationMatrixType.TransposeModelView, UnityMatrixType.Model},
            {TransformationMatrixType.InverseTransposeModelView, UnityMatrixType.Model},
            {TransformationMatrixType.ObjectToWorld, UnityMatrixType.Model},
            {TransformationMatrixType.WorldToObject, UnityMatrixType.InverseModel},
        };

        [SerializeField]
        private TransformationMatrixType m_matrix = TransformationMatrixType.ModelView;

        [SerializeField]
        private UnityMatrixType m_MatrixType = UnityMatrixType.Model;

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return false; } }

        [EnumControl("")]
        public UnityMatrixType matrixType
        {
            get { return m_MatrixType; }
            set
            {
                if (m_MatrixType == value)
                    return;

                m_MatrixType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public TransformationMatrixNode()
        {
            name = "Transformation Matrix";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Transformation-Matrix-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            if(m_matrix != TransformationMatrixType.None)
            {
                m_MatrixType = m_MatrixUpgrade[m_matrix];
                m_matrix = TransformationMatrixType.None;
            }

            AddSlot(new Matrix4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return m_MatrixList[matrixType].ToString(CultureInfo.InvariantCulture);
        }

        public bool RequiresVertexColor()
        {
            return true;
        }
    }
}
