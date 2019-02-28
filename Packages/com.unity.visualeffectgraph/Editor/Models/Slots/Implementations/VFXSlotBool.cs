using System;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(bool))]
    class VFXSlotBool : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<bool>(false, mode);
        }
    }
}
