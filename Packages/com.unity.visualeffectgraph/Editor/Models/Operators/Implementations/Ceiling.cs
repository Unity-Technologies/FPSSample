using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Ceiling : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 0;
        }

        protected override sealed string operatorName { get { return "Ceiling"; } }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptInteger;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Ceil(inputExpression[0]) };
        }
    }
}
