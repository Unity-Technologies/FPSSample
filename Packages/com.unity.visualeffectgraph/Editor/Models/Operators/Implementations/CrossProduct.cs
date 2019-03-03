using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class CrossProduct : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            [Tooltip("The first operand.")]
            public Vector3 a = Vector3.right;
            [Tooltip("The second operand.")]
            public Vector3 b = Vector3.up;
        }

        protected override sealed string operatorName { get { return "Cross Product"; } }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowVector3Type | ValidTypeRule.allowSpaceable;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Cross(inputExpression[0], inputExpression[1]) };
        }
    }
}
