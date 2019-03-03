using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Wave")]
    class SineWave : VFXOperatorNumericUnified, IVFXOperatorNumericUnifiedConstrained
    {
        public class InputProperties
        {
            public float input = 0.5f;
            public float frequency = 1.0f;
            public float min = 0.0f;
            public float max = 1.0f;
        }

        protected override sealed string operatorName { get { return "Sine Wave"; } }

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

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            //(1-cos(F*2*pi*x))/2
            var type = inputExpression[0].valueType;
            var one = VFXOperatorUtility.OneExpression[type];
            var tau = VFXOperatorUtility.TauExpression[type];
            var two = VFXOperatorUtility.TwoExpression[type];

            var res = new VFXExpressionDivide(one - new VFXExpressionCos(inputExpression[0] * inputExpression[1] * tau), two);
            return new[] { VFXOperatorUtility.Lerp(inputExpression[2], inputExpression[3], res) };
        }
    }
}
