using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class Normalize : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public Vector3 x = Vector3.one;
        }

        protected override sealed string operatorName { get { return "Normalize"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Normalize(inputExpression[0]) };
        }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowVectorType;
            }
        }
    }
}
