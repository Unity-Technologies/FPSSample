using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Rendering;


namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture3D))]
    class VFXSlotTexture3D : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTexture3DValue(null, mode);
        }
    }
}
