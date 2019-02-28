using System;
using UnityEditor.VFX;
namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Round : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 0.5f;
        }

        protected override sealed string operatorName { get { return "Round"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Round(inputExpression[0]) };
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
