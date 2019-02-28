using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class DotProduct : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            [Tooltip("The first operand.")]
            public Vector3 a = Vector3.zero;
            [Tooltip("The second operand.")]
            public Vector3 b = Vector3.zero;
        }

        public class OutputProperties
        {
            [Tooltip("The dot product between a and b.")]
            public float d;
        }

        public override string libraryName { get { return "Dot Product"; } }
        public override string name { get { return "Dot Product"; } }

        protected sealed override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptOneDimension;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Dot(inputExpression[0], inputExpression[1]) };
        }
    }
}
