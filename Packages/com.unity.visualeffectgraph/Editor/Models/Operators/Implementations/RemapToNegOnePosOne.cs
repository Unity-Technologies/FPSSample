using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Remap")]
    class RemapToNegOnePosOne : VFXOperatorNumericUniform
    {
        [VFXSetting, SerializeField, Tooltip("Whether the values are clamped to the input/output range")]
        private bool m_Clamp = false;

        public class InputProperties
        {
            [Tooltip("The value to be remapped into the new range.")]
            public float input = 0.5f;
        }

        protected override sealed string operatorName { get { return "Remap [0..1] => [-1..1]"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var type = inputExpression[0].valueType;

            VFXExpression input;
            if (m_Clamp)
                input = VFXOperatorUtility.Saturate(inputExpression[0]);
            else
                input = inputExpression[0];

            return new[] { VFXOperatorUtility.Mad(input, VFXOperatorUtility.TwoExpression[type], VFXOperatorUtility.Negate(VFXOperatorUtility.OneExpression[type])) };
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
