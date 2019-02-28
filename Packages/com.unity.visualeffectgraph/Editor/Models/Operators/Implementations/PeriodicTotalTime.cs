using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Time")]
    class PeriodicTotalTime : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The period of time being looped over (in seconds)")]
            [Min(0.001f)]
            public float Period = 5.0f;
            [Tooltip("The output value range interpolated over the period of time")]
            public Vector2 Range = new Vector2(0.0f, 1.0f);
        }

        public class OutputProperties
        {
            public float t = 0;
        }

        public override string name
        {
            get
            {
                return "Periodic Total Time";
            }
        }
        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] output = new VFXExpression[]
            {
                VFXOperatorUtility.Lerp(inputExpression[1].x, inputExpression[1].y, VFXOperatorUtility.Frac(VFXBuiltInExpression.TotalTime / inputExpression[0])),
            };
            return output;
        }
    }
}
