using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Color))]
    class VFXSlotColor : VFXSlotFloat4
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                || type == typeof(Vector3)
                || type == typeof(Vector4);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            if (expression.valueType == VFXValueType.Float3)
            {
                return VFXOperatorUtility.CastFloat(expression, VFXValueType.Float4, 1.0f);
            }
            return base.ConvertExpression(expression, sourceSlot);
        }
    }
}
