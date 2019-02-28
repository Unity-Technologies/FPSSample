using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Saturate : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            [Tooltip("The value to be clamped.")]
            public float input = 0.0f;
        }

        protected override sealed string operatorName { get { return "Saturate"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Saturate(inputExpression[0]) };
        }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptInteger;
            }
        }
    }
}
