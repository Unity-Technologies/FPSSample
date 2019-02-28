using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionBakeGradient : VFXExpression
    {
        public VFXExpressionBakeGradient() : this(VFXValue<Gradient>.Default)
        {
        }

        public VFXExpressionBakeGradient(VFXExpression curve)
            : base(Flags.InvalidOnGPU, new VFXExpression[1] { curve })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.BakeGradient; } }
    }
}
