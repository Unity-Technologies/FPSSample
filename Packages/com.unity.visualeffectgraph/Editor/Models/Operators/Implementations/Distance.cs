using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class Distance : VFXOperatorNumericUniform
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
            [Tooltip("The distance between a and b.")]
            public float d;
        }

        protected override sealed string operatorName { get { return "Distance"; } }

        protected override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptInteger;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Distance(inputExpression[0], inputExpression[1]) };
        }
    }
}
