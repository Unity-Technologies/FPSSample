using System;
namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Absolute : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 0;
        }

        protected override sealed string operatorName { get { return "Absolute"; } }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptUnsignedInteger;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionAbs(inputExpression[0]) };
        }
    }
}
