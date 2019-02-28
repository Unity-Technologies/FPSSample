using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Wave")]
    class SquareWave : VFXOperatorNumericUnified, IVFXOperatorNumericUnifiedConstrained
    {
        public class InputProperties
        {
            public float input = 0.5f;
            public float frequency = 1.0f;
            public float min = 0.0f;
            public float max = 1.0f;
        }

        protected override sealed string operatorName { get { return "Square Wave"; } }

        public IEnumerable<int> slotIndicesThatMustHaveSameType
        {
            get
            {
                return Enumerable.Range(0, 4);
            }
        }

        public IEnumerable<int> slotIndicesThatCanBeScalar
        {
            get
            {
                return Enumerable.Range(1, 4);
            }
        }

        protected sealed override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptInteger;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            //round(frac(x*F))
            var expression = inputExpression[0] * inputExpression[1];
            var res = VFXOperatorUtility.Round(VFXOperatorUtility.Frac(expression));
            return new[] { VFXOperatorUtility.Lerp(inputExpression[2], inputExpression[3], res) };
        }
    }
}
