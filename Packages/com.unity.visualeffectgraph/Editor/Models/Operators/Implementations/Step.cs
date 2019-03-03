using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Step : VFXOperatorNumericUnified, IVFXOperatorNumericUnifiedConstrained
    {
        protected override sealed string operatorName { get { return "Step"; } }

        public class InputProperties
        {
            [Tooltip("The value to compare")]
            public float Value = 0.0f;
            [Tooltip("The threshold from which the function will return one")]
            public float Threshold = 0.5f;
        }

        public IEnumerable<int> slotIndicesThatMustHaveSameType
        {
            get
            {
                return Enumerable.Range(0, 2);
            }
        }

        public IEnumerable<int> slotIndicesThatCanBeScalar
        {
            get
            {
                yield return 1;
            }
        }

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptInteger; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[]
            {
                VFXOperatorUtility.Saturate(VFXOperatorUtility.Ceil(inputExpression[0] - inputExpression[1])),

                // TODO : It would be nice to have inverted step output (1 if below threshold), but we need to be able to define multiple output slots.
                //VFXOperatorUtility.Clamp( new VFXExpressionFloor(inputExpression[0])-inputExpression[1], VFXValue.Constant(0.0f), VFXValue.Constant(1.0f)),
            };
        }
    }
}
