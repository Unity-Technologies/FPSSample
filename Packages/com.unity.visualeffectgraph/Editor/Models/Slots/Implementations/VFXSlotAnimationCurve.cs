using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(AnimationCurve))]
    class VFXSlotAnimationCurve : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<AnimationCurve>(new AnimationCurve(), mode);
        }
    }
}
