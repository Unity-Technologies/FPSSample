using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXExpressionUnaryBoolOperation : VFXExpressionUnaryNumericOperation
    {
        public VFXExpressionUnaryBoolOperation(VFXExpression parent, VFXExpressionOperation operation) : base(parent, operation)
        {
            if (!IsBoolValueType(parent.valueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionUnaryBoolOperation");
            }
        }

        sealed protected override int ProcessUnaryOperation(int input)
        {
            throw new NotImplementedException();
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            throw new NotImplementedException();
        }

        sealed protected override uint ProcessUnaryOperation(uint input)
        {
            throw new NotImplementedException();
        }

        sealed protected override string GetUnaryOperationCode(string x, VFXValueType type)
        {
            if (!IsBoolValueType(type))
                throw new InvalidOperationException("VFXExpressionUnaryBoolOperation : Unexpected type");

            return GetUnaryOperationCode(x);
        }

        abstract protected string GetUnaryOperationCode(string x);
    }

    abstract class VFXExpressionBinaryBoolOperation : VFXExpressionBinaryNumericOperation
    {
        protected VFXExpressionBinaryBoolOperation(VFXExpression parentLeft, VFXExpression parentRight, VFXExpressionOperation operation)
            : base(parentLeft, parentRight, operation)
        {
            if (!IsBoolValueType(parentLeft.valueType) || !IsBoolValueType(parentRight.valueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionBinaryBoolOperation");
            }
        }

        sealed protected override int ProcessBinaryOperation(int x, int y)
        {
            throw new NotImplementedException();
        }

        sealed protected override float ProcessBinaryOperation(float x, float y)
        {
            throw new NotImplementedException();
        }

        sealed protected override uint ProcessBinaryOperation(uint x, uint y)
        {
            throw new NotImplementedException();
        }

        sealed protected override string GetBinaryOperationCode(string x, string y, VFXValueType type)
        {
            if (!IsBoolValueType(type))
            {
                throw new InvalidOperationException("Invalid VFXExpressionBinaryBoolOperation");
            }

            return GetBinaryOperationCode(x, y);
        }

        protected abstract string GetBinaryOperationCode(string x, string y);
    }
}
