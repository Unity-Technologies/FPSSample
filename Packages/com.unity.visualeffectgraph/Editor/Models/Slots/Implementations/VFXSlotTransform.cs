using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Transform))]
    class VFXSlotTransform : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Matrix4x4>(Matrix4x4.identity, mode);
        }

        protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type) || type == typeof(Matrix4x4) || type == typeof(Transform) || type == typeof(OrientedBox);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            if (expression.valueType == VFXValueType.Matrix4x4)
                return expression;

            throw new Exception("Unexpected type of expression " + expression);
        }

        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionTRSToMatrix(expr);
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[3]
            {
                new VFXExpressionExtractPositionFromMatrix(expr),
                new VFXExpressionExtractAnglesFromMatrix(expr),
                new VFXExpressionExtractScaleFromMatrix(expr)
            };
        }
    }
}
