using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXExpressionNumericOperation : VFXExpression
    {
        protected VFXExpressionNumericOperation(VFXExpression[] parents)
            : base(Flags.None, parents)
        {
            m_additionnalOperands = new int[] {};
        }

        static private object[] ToObjectArray(float input) { return new object[] { input }; }
        static private object[] ToObjectArray(Vector2 input) { return new object[] { input.x, input.y }; }
        static private object[] ToObjectArray(Vector3 input) { return new object[] { input.x, input.y, input.z }; }
        static private object[] ToObjectArray(Vector4 input) { return new object[] { input.x, input.y, input.z, input.w }; }
        static protected object[] ToObjectArray(VFXExpression input)
        {
            switch (input.valueType)
            {
                case VFXValueType.Float: return ToObjectArray(input.Get<float>());
                case VFXValueType.Float2: return ToObjectArray(input.Get<Vector2>());
                case VFXValueType.Float3: return ToObjectArray(input.Get<Vector3>());
                case VFXValueType.Float4: return ToObjectArray(input.Get<Vector4>());
                case VFXValueType.Int32: return new object[] { input.Get<int>() };
                case VFXValueType.Uint32: return new object[] { input.Get<uint>() };
                case VFXValueType.Boolean: return new object[] { input.Get<bool>() };
            }
            return null;
        }

        static protected VFXExpression ToVFXValue(object[] input, VFXValue.Mode mode)
        {
            if (input[0] is int)
            {
                if (input.Length != 1)
                    throw new InvalidOperationException("VFXExpressionMathOperation : Unexpected size of int");
                return new VFXValue<int>((int)input[0], mode);
            }
            else if (input[0] is uint)
            {
                if (input.Length != 1)
                    throw new InvalidOperationException("VFXExpressionMathOperation : Unexpected size of uint");
                return new VFXValue<uint>((uint)input[0], mode);
            }
            else if (input[0] is bool)
            {
                if (input.Length != 1)
                    throw new InvalidOperationException("VFXExpressionMathOperation : Unexpected size of bool");
                return new VFXValue<bool>((bool)input[0], mode);
            }
            else if (input[0] is float)
            {
                if (input.OfType<float>().Count() != input.Length)
                    throw new InvalidOperationException("VFXExpressionMathOperation : Unexpected type of float among other float");

                switch (input.Length)
                {
                    case 1: return new VFXValue<float>((float)input[0], mode);
                    case 2: return new VFXValue<Vector2>(new Vector2((float)input[0], (float)input[1]), mode);
                    case 3: return new VFXValue<Vector3>(new Vector3((float)input[0], (float)input[1], (float)input[2]), mode);
                    case 4: return new VFXValue<Vector4>(new Vector4((float)input[0], (float)input[1], (float)input[2], (float)input[3]), mode);
                }
            }
            return null;
        }

        static protected bool IsNumeric(VFXValueType type)
        {
            return IsFloatValueType(type) || IsUIntValueType(type) || IsIntValueType(type) || IsBoolValueType(type);
        }

        sealed public override VFXExpressionOperation operation { get { return m_Operation; } }
        sealed protected override int[] additionnalOperands { get { return m_additionnalOperands; } }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var newExpression = (VFXExpressionNumericOperation)base.Reduce(reducedParents);
            newExpression.m_additionnalOperands = m_additionnalOperands.Select(o => o).ToArray();
            newExpression.m_Operation = m_Operation;
            return newExpression;
        }

        protected int[] m_additionnalOperands;
        protected VFXExpressionOperation m_Operation;
    }

    abstract class VFXExpressionUnaryNumericOperation : VFXExpressionNumericOperation
    {
        protected VFXExpressionUnaryNumericOperation(VFXExpression parent, VFXExpressionOperation operation) : base(new VFXExpression[1] { parent })
        {
            if (!IsNumeric(parent.valueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionUnaryMathOperation");
            }
            m_additionnalOperands = new int[] { (int)parent.valueType };
            m_Operation = operation;
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            var source = ToObjectArray(reducedParents[0]);
            var result = new object[source.Length];

            if (source[0] is float)
            {
                result = source.Select(o => (object)ProcessUnaryOperation((float)o)).ToArray();
            }
            else if (source[0] is int)
            {
                result = source.Select(o => (object)ProcessUnaryOperation((int)o)).ToArray();
            }
            else if (source[0] is uint)
            {
                result = source.Select(o => (object)ProcessUnaryOperation((uint)o)).ToArray();
            }
            else if (source[0] is bool)
            {
                result = source.Select(o => (object)ProcessUnaryOperation((bool)o)).ToArray();
            }
            else
            {
                throw new InvalidOperationException("Unexpected type in VFXExpressionUnaryMathOperation");
            }
            return ToVFXValue(result, VFXValue.Mode.Constant);
        }

        abstract protected float ProcessUnaryOperation(float input);
        abstract protected int ProcessUnaryOperation(int input);
        abstract protected uint ProcessUnaryOperation(uint input);
        abstract protected bool ProcessUnaryOperation(bool input);

        sealed public override string GetCodeString(string[] parents)
        {
            var valueType = this.parents.First().valueType;
            valueType = IsFloatValueType(valueType) ? VFXValueType.Float : valueType;
            return GetUnaryOperationCode(parents[0], valueType);
        }

        abstract protected string GetUnaryOperationCode(string x, VFXValueType type);
    }


    abstract class VFXExpressionBinaryNumericOperation : VFXExpressionNumericOperation
    {
        protected VFXExpressionBinaryNumericOperation(VFXExpression parentLeft, VFXExpression parentRight, VFXExpressionOperation operation)
            : base(new VFXExpression[2] { parentLeft, parentRight })
        {
            if (!IsNumeric(parentLeft.valueType) || !IsNumeric(parentRight.valueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionBinaryMathOperation (not numeric type)");
            }

            if (parentRight.valueType != parentLeft.valueType)
            {
                throw new ArgumentException("Incorrect VFXExpressionBinaryFloatOperation (incompatible numeric type)");
            }

            m_additionnalOperands = new int[] { (int)parentLeft.valueType };
            m_Operation = operation;
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            var parentLeft = reducedParents[0];
            var parentRight = reducedParents[1];

            var sourceLeft = ToObjectArray(parentLeft);
            var sourceRight = ToObjectArray(parentRight);

            var result = new object[sourceLeft.Length];
            if (sourceLeft[0] is float)
            {
                for (int iChannel = 0; iChannel < sourceLeft.Length; ++iChannel)
                {
                    result[iChannel] = ProcessBinaryOperation((float)sourceLeft[iChannel], (float)sourceRight[iChannel]);
                }
            }
            else if (sourceLeft[0] is int)
            {
                for (int iChannel = 0; iChannel < sourceLeft.Length; ++iChannel)
                {
                    result[iChannel] = ProcessBinaryOperation((int)sourceLeft[iChannel], (int)sourceRight[iChannel]);
                }
            }
            else if (sourceLeft[0] is uint)
            {
                for (int iChannel = 0; iChannel < sourceLeft.Length; ++iChannel)
                {
                    result[iChannel] = ProcessBinaryOperation((uint)sourceLeft[iChannel], (uint)sourceRight[iChannel]);
                }
            }
            else if (sourceLeft[0] is bool)
            {
                for (int iChannel = 0; iChannel < sourceLeft.Length; ++iChannel)
                {
                    result[iChannel] = ProcessBinaryOperation((bool)sourceLeft[iChannel], (bool)sourceRight[iChannel]);
                }
            }
            else
            {
                throw new InvalidOperationException("Unexpected type in VFXExpressionUnaryMathOperation");
            }
            return ToVFXValue(result, VFXValue.Mode.Constant);
        }

        abstract protected float ProcessBinaryOperation(float x, float y);
        abstract protected int ProcessBinaryOperation(int x, int y);
        abstract protected uint ProcessBinaryOperation(uint x, uint y);
        abstract protected bool ProcessBinaryOperation(bool x, bool y);

        sealed public override string GetCodeString(string[] parents)
        {
            var valueType = this.parents.First().valueType;
            valueType = IsFloatValueType(valueType) ? VFXValueType.Float : valueType;
            return GetBinaryOperationCode(parents[0], parents[1], valueType);
        }

        abstract protected string GetBinaryOperationCode(string x, string y, VFXValueType type);
    }
}
