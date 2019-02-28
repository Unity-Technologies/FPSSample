using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleCurve : VFXExpression
    {
        public VFXExpressionSampleCurve() : this(VFXValue<AnimationCurve>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSampleCurve(VFXExpression curve, VFXExpression time)
            : base(Flags.None, new VFXExpression[2] { curve, time })
        {}

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleCurve; } }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var timeReduce = constParents[1];
            var curveReduce = constParents[0];

            var curve = curveReduce.Get<AnimationCurve>();
            var time = timeReduce.Get<float>();
            return VFXValue.Constant(curve.Evaluate(time));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleCurve({0},{1})", parents[0], parents[1]);
        }
    }
}
