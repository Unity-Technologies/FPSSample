using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Sign : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 0.0f;
        }

        protected override sealed string operatorName { get { return "Sign"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSign(inputExpression[0]) };
        }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptUnsignedInteger;
            }
        }
    }
}
