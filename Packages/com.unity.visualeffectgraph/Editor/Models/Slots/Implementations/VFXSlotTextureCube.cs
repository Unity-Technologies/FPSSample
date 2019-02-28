using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Cubemap))]
    class VFXSlotTextureCube : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTextureCubeValue(null, mode);
        }
    }
}
