using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture2DArray))]
    class VFXSlotTexture2DArray : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTexture2DArrayValue(null, mode);
        }
    }
}
