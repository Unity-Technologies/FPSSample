using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleGradient : VFXExpression
    {
        public VFXExpressionSampleGradient() : this(VFXValue<Gradient>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSampleGradient(VFXExpression gradient, VFXExpression time)
            : base(Flags.None, new VFXExpression[2] { gradient, time })
        {}

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.SampleGradient; } }
        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var timeReduce = constParents[1];
            var gradientReduce = constParents[0];

            var gradient = gradientReduce.Get<Gradient>();
            var time = timeReduce.Get<float>();
            return VFXValue.Constant((Vector4)gradient.Evaluate(time));
        }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleGradient({0},{1})", parents[0], parents[1]);
        }
    }
}
