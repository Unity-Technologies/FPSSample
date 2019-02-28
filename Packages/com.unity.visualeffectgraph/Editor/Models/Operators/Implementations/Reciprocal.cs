using System;
namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Reciprocal : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 1.0f;
        }

        protected override sealed string operatorName { get { return "Reciprocal (1/x)"; } }

        protected override double defaultValueDouble
        {
            get
            {
                return 1.0;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expression = inputExpression[0];
            return new[] { VFXOperatorUtility.Reciprocal(expression) };
        }

        protected sealed override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptInteger;
            }
        }
    }
}
