using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(CubemapArray))]
    class VFXSlotTextureCubeArray : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTextureCubeArrayValue(null, mode);
        }
    }
}
