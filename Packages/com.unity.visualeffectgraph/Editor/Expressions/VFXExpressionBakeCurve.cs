using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionBakeCurve : VFXExpression
    {
        public VFXExpressionBakeCurve() : this(VFXValue<AnimationCurve>.Default)
        {
        }

        public VFXExpressionBakeCurve(VFXExpression curve) : base(Flags.InvalidOnGPU, new VFXExpression[1] { curve })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.BakeCurve; } }
    }
}
