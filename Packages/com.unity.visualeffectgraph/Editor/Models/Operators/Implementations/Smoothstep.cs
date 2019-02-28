using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Smoothstep : VFXOperatorNumericUnified, IVFXOperatorNumericUnifiedConstrained
    {
        public class InputProperties
        {
            [Tooltip("The start value.")]
            public float x = 0.0f;
            [Tooltip("The end value.")]
            public float y = 1.0f;
            [Tooltip("Smoothstep returns a value between 0 and 1, and s is clamped between x and y.")]
            public float s = 0.5f;
        }

        protected override sealed string operatorName { get { return "Smoothstep"; } }

        public IEnumerable<int> slotIndicesThatMustHaveSameType
        {
            get
            {
                return Enumerable.Range(0, 3);
            }
        }

        public IEnumerable<int> slotIndicesThatCanBeScalar
        {
            get
            {
                yield return 2;
            }
        }

        protected override sealed double defaultValueDouble
        {
            get
            {
                return 0.5;
            }
        }

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptInteger; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Smoothstep(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
