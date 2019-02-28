using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;


namespace UnityEditor.VFX
{
    abstract class VFXExpressionUnaryFloatOperation : VFXExpressionUnaryNumericOperation
    {
        public VFXExpressionUnaryFloatOperation(VFXExpression parent, VFXExpressionOperation operation) : base(parent, operation)
        {
            if (!IsFloatValueType(parent.valueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionUnaryFloatOperation");
            }
        }

        sealed protected override int ProcessUnaryOperation(int input)
        {
            throw new NotImplementedException();
        }

        sealed protected override uint ProcessUnaryOperation(uint input)
        {
            throw new NotImplementedException();
        }

        sealed protected override bool ProcessUnaryOperation(bool input)
        {
            throw new NotImplementedException();
        }

        sealed protected override string GetUnaryOperationCode(string x, VFXValueType type)
        {
            if (type != VFXValueType.Float)
                throw new InvalidOperationException("VFXExpressionUnaryFloatOperation : Unexpected type");

            return GetUnaryOperationCode(x);
        }

        abstract protected string GetUnaryOperationCode(string x);
    }

    abstract class VFXExpressionBinaryFloatOperation : VFXExpressionBinaryNumericOperation
    {
        protected VFXExpressionBinaryFloatOperation(VFXExpression parentLeft, VFXExpression parentRight, VFXExpressionOperation operation)
            : base(parentLeft, parentRight, operation)
        {
            if (!IsFloatValueType(parentLeft.valueType) || !IsFloatValueType(parentRight.valueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionBinaryFloatOperation (not float type)");
            }
        }

        sealed protected override int ProcessBinaryOperation(int x, int y)
        {
            throw new NotImplementedException();
        }

        sealed protected override uint ProcessBinaryOperation(uint x, uint y)
        {
            throw new NotImplementedException();
        }

        sealed protected override bool ProcessBinaryOperation(bool x, bool y)
        {
            throw new NotImplementedException();
        }

        sealed protected override string GetBinaryOperationCode(string x, string y, VFXValueType type)
        {
            if (type != VFXValueType.Float)
            {
                throw new InvalidOperationException("Invalid VFXExpressionBinaryFloatOperation");
            }

            return GetBinaryOperationCode(x, y);
        }

        protected abstract string GetBinaryOperationCode(string x, string y);
    }
}
