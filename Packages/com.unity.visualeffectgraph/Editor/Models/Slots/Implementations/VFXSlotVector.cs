using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Vector))]
    class VFXSlotVector : VFXSlotEncapsulated
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type) || type == typeof(Vector3) || type == typeof(Position) || type == typeof(DirectionType);
        }

        sealed public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Vector3>(Vector3.zero, mode);
        }
    }
}
