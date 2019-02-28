using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Vector4))]
    class VFXSlotFloat4 : VFXSlot
    {
        protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                || type == typeof(float)
                || type == typeof(uint)
                || type == typeof(int)
                || type == typeof(Color);
        }

        protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            if (expression.valueType == VFXValueType.Float4)
                return expression;

            if (expression.valueType == VFXValueType.Float)
                return new VFXExpressionCombine(expression, expression, expression, expression);

            if (expression.valueType == VFXValueType.Uint32)
            {
                var floatExpression = new VFXExpressionCastUintToFloat(expression);
                return new VFXExpressionCombine(floatExpression, floatExpression, floatExpression, floatExpression);
            }

            if (expression.valueType == VFXValueType.Int32)
            {
                var floatExpression = new VFXExpressionCastIntToFloat(expression);
                return new VFXExpressionCombine(floatExpression, floatExpression, floatExpression, floatExpression);
            }

            throw new Exception("Unexpected type of expression " + expression);
        }

        sealed public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Vector4>(Vector4.zero, mode);
        }

        sealed protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionCombine(
                expr[0],
                expr[1],
                expr[2],
                expr[3]);
        }

        sealed protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[4]
            {
                expr.x,
                expr.y,
                expr.z,
                expr.w
            };
        }
    }
}
