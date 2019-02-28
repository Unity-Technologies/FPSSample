using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling")]
    class SampleCurve : VFXOperator
    {
        override public string name { get { return "Sample Curve"; } }

        public class InputProperties
        {
            [Tooltip("The curve to sample from.")]
            public AnimationCurve curve = VFXResources.defaultResources.animationCurve;
            [Tooltip("The time along the curve to take a sample from.")]
            public float time = 0.0f;
        }

        public class OutputProperties
        {
            public float s = 0;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSampleCurve(inputExpression[0], inputExpression[1]) };
        }
    }
}
