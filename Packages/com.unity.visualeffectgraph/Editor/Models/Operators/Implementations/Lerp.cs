using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Lerp : VFXOperatorNumericUnified, IVFXOperatorNumericUnifiedConstrained
    {
        public class InputProperties
        {
            [Tooltip("The start value.")]
            public float x = 0.0f;
            [Tooltip("The end value.")]
            public float y = 1.0f;
            [Tooltip("The amount to interpolate between x and y (0-1).")]
            public float s = 0.5f;
        }

        protected override sealed string operatorName { get { return "Lerp"; } }

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

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptInteger; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Lerp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
