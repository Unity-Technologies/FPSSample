using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    //Helper to isolate all class which are skipping first level of hierarchy (sealed function are important at this stage)
    class VFXSlotEncapsulated : VFXSlot
    {
        protected override sealed VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            if (expr.Length != 1)
                throw new InvalidOperationException("Incorrect VFXSlotPImpl");
            return ApplyPatchExpression(expr[0]);
        }

        protected override sealed VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[1] { expr };
        }

        virtual protected VFXExpression ApplyPatchExpression(VFXExpression expression)
        {
            return expression;
        }
    }
}
