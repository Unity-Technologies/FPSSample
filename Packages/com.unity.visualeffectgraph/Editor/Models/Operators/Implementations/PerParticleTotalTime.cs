using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Time")]
    class PerParticleTotalTime : VFXOperator
    {
        public class OutputProperties
        {
            public float t = 0;
        }

        public override string name
        {
            get
            {
                return "Total Time (Per-Particle)";
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] output = new VFXExpression[]
            {
                VFXBuiltInExpression.TotalTime + (VFXBuiltInExpression.DeltaTime * VFXOperatorUtility.FixedRandom(0xc43388e9, true)),
            };
            return output;
        }
    }
}
