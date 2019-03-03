using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXExpressionUnaryUIntOperation : VFXExpressionUnaryNumericOperation
    {
        public VFXExpressionUnaryUIntOperation(VFXExpression parent, VFXExpressionOperation operation) : base(parent, operation)
        {
            if (!IsUIntValueType(parent.valueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionUnaryUIntOperation");
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

        sealed protected override bool ProcessUnaryOperation(bool input)
        {
            throw new NotImplementedException();
        }

        sealed protected override string GetUnaryOperationCode(string x, VFXValueType type)
        {
            if (!IsUIntValueType(type))
                throw new InvalidOperationException("VFXExpressionUnaryUIntOperation : Unexpected type");

            return GetUnaryOperationCode(x);
        }

        abstract protected string GetUnaryOperationCode(string x);
    }

    abstract class VFXExpressionBinaryUIntOperation : VFXExpressionBinaryNumericOperation
    {
        protected VFXExpressionBinaryUIntOperation(VFXExpression parentLeft, VFXExpression parentRight, VFXExpressionOperation operation)
            : base(parentLeft, parentRight, operation)
        {
            if (!IsUIntValueType(parentLeft.valueType) || !IsUIntValueType(parentRight.valueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionBinaryUIntOperation");
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

        sealed protected override bool ProcessBinaryOperation(bool x, bool y)
        {
            throw new NotImplementedException();
        }

        sealed protected override string GetBinaryOperationCode(string x, string y, VFXValueType type)
        {
            if (!IsUIntValueType(type))
            {
                throw new InvalidOperationException("Invalid VFXExpressionBinaryUIntOperation");
            }

            return GetBinaryOperationCode(x, y);
        }

        protected abstract string GetBinaryOperationCode(string x, string y);
    }
}
