using System;
using System.Linq;
using UnityEngine;
using UnityEditor.VFX;
using System.Collections.Generic;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Clamp : VFXOperatorNumericUnified, IVFXOperatorNumericUnifiedConstrained
    {
        public class InputProperties
        {
            [Tooltip("The value to be clamped.")]
            public float input = 0.0f;
            [Tooltip("The lower bound to clamp the input to.")]
            public float min = 0.0f;
            [Tooltip("The upper bound to clamp the input to.")]
            public float max = 1.0f;
        }

        protected override sealed string operatorName { get { return "Clamp"; } }

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
                return Enumerable.Range(1, 2);
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Clamp(inputExpression[0], inputExpression[1], inputExpression[2], false) };
        }
    }
}
