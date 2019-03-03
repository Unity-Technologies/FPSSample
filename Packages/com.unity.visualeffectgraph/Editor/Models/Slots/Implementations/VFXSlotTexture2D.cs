using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture2D))]
    class VFXSlotTexture2D : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTexture2DValue(null, mode);
        }
    }
}
