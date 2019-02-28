using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Position))]
    class VFXSlotPosition : VFXSlotEncapsulated
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Vector3>(Vector3.zero, mode);
        }

        protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type) || type == typeof(Vector4) || type == typeof(Vector3) || type == typeof(Vector);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expresssion, VFXSlot sourceSlot)
        {
            if (expresssion.valueType == VFXValueType.Float3)
                return expresssion;

            return VFXOperatorUtility.CastFloat(expresssion, VFXValueType.Float3);
        }
    }
}
