using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionCos : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionCos() : this(VFXValue<float>.Default) {}

        public VFXExpressionCos(VFXExpression parent) : base(parent, VFXExpressionOperation.Cos)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("cos({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Cos(input);
        }
    }

    class VFXExpressionSin : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionSin() : this(VFXValue<float>.Default) {}

        public VFXExpressionSin(VFXExpression parent) : base(parent, VFXExpressionOperation.Sin)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("sin({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Sin(input);
        }
    }

    class VFXExpressionTan : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionTan() : this(VFXValue<float>.Default) {}

        public VFXExpressionTan(VFXExpression parent) : base(parent, VFXExpressionOperation.Tan)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("tan({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Tan(input);
        }
    }

    class VFXExpressionACos : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionACos() : this(VFXValue<float>.Default) {}

        public VFXExpressionACos(VFXExpression parent) : base(parent, VFXExpressionOperation.ACos)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("acos({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Acos(input);
        }
    }

    class VFXExpressionASin : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionASin() : this(VFXValue<float>.Default) {}

        public VFXExpressionASin(VFXExpression parent) : base(parent, VFXExpressionOperation.ASin)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("asin({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Asin(input);
        }
    }

    class VFXExpressionATan : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionATan() : this(VFXValue<float>.Default) {}

        public VFXExpressionATan(VFXExpression parent) : base(parent, VFXExpressionOperation.ATan)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("atan({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Atan(input);
        }
    }

    class VFXExpressionLog2 : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionLog2() : this(VFXValue<float>.Default) {}

        public VFXExpressionLog2(VFXExpression parent) : base(parent, VFXExpressionOperation.Log2)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("log2({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Log(input, 2.0f);
        }
    }

    class VFXExpressionAbs : VFXExpressionUnaryNumericOperation
    {
        public VFXExpressionAbs() : this(VFXValue<float>.Default) {}

        public VFXExpressionAbs(VFXExpression parent) : base(parent, VFXExpressionOperation.Abs)
        {
            if (parent.valueType == VFXValueType.Uint32)
                throw new NotImplementedException("Unexpected type for VFXExpressionAbs");
        }

        sealed protected override string GetUnaryOperationCode(string x, VFXValueType type)
        {
            if (type == VFXValueType.Uint32)
                throw new NotImplementedException("Unexpected type for VFXExpressionAbs");
            return string.Format("abs({0})", x);
        }

        protected override uint ProcessUnaryOperation(uint input)
        {
            throw new NotImplementedException();
        }

        protected override int ProcessUnaryOperation(int input)
        {
            return Mathf.Abs(input);
        }

        protected override bool ProcessUnaryOperation(bool input)
        {
            throw new NotImplementedException();
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Abs(input);
        }
    }

    class VFXExpressionSign : VFXExpressionUnaryNumericOperation
    {
        public VFXExpressionSign() : this(VFXValue<float>.Default) {}

        public VFXExpressionSign(VFXExpression parent) : base(parent, VFXExpressionOperation.Sign)
        {
            if (parent.valueType == VFXValueType.Uint32)
                throw new NotImplementedException("Unexpected type for VFXExpressionSign");
        }

        sealed protected override string GetUnaryOperationCode(string x, VFXValueType type)
        {
            if (type == VFXValueType.Uint32)
                throw new NotImplementedException("Unexpected type for VFXExpressionSign");
            return string.Format("sign({0})", x);
        }

        protected override uint ProcessUnaryOperation(uint input)
        {
            throw new NotImplementedException();
        }

        protected override int ProcessUnaryOperation(int input)
        {
            return (input > 0 ? 1 : 0) - (input < 0 ? 1 : 0);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Sign(input);
        }

        protected override bool ProcessUnaryOperation(bool input)
        {
            throw new NotImplementedException();
        }
    }

    class VFXExpressionFloor : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionFloor() : this(VFXValue<float>.Default) {}

        public VFXExpressionFloor(VFXExpression parent) : base(parent, VFXExpressionOperation.Floor)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("floor({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Floor(input);
        }
    }

    class VFXExpressionAdd : VFXExpressionBinaryNumericOperation
    {
        public VFXExpressionAdd() : this(VFXValue<float>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionAdd(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.Add)
        {
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var zero = VFXOperatorUtility.ZeroExpression[reducedParents[0].valueType];
            if (zero.Equals(reducedParents[0]))
                return reducedParents[1];
            if (zero.Equals(reducedParents[1]))
                return reducedParents[0];

            return base.Reduce(reducedParents);
        }

        sealed protected override string GetBinaryOperationCode(string left, string right, VFXValueType type)
        {
            return string.Format("{0} + {1}", left, right);
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return left + right;
        }

        sealed protected override int ProcessBinaryOperation(int left, int right)
        {
            return left + right;
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left + right;
        }

        protected override bool ProcessBinaryOperation(bool left, bool right)
        {
            throw new NotImplementedException();
        }
    }

    class VFXExpressionMul : VFXExpressionBinaryNumericOperation
    {
        public VFXExpressionMul() : this(VFXValue<float>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionMul(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.Mul)
        {
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var zero  = VFXOperatorUtility.ZeroExpression[reducedParents[0].valueType];
            if (zero.Equals(reducedParents[0]) || zero.Equals(reducedParents[1]))
                return zero;

            var one = VFXOperatorUtility.OneExpression[reducedParents[0].valueType];
            if (one.Equals(reducedParents[0]))
                return reducedParents[1];
            if (one.Equals(reducedParents[1]))
                return reducedParents[0];

            return base.Reduce(reducedParents);
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return left * right;
        }

        sealed protected override int ProcessBinaryOperation(int left, int right)
        {
            return left * right;
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left * right;
        }

        protected override bool ProcessBinaryOperation(bool left, bool right)
        {
            throw new NotImplementedException();
        }

        sealed protected override string GetBinaryOperationCode(string left, string right, VFXValueType type)
        {
            return string.Format("{0} * {1}", left, right);
        }
    }

    class VFXExpressionDivide : VFXExpressionBinaryNumericOperation
    {
        public VFXExpressionDivide() : this(VFXValue<float>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionDivide(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.Divide)
        {
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var zero = VFXOperatorUtility.ZeroExpression[reducedParents[0].valueType];
            if (zero.Equals(reducedParents[0]))
                return zero;

            var one = VFXOperatorUtility.OneExpression[reducedParents[0].valueType];
            if (one.Equals(reducedParents[1]))
                return reducedParents[0];

            return base.Reduce(reducedParents);
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return left / right;
        }

        sealed protected override int ProcessBinaryOperation(int left, int right)
        {
            if (right == 0)
                return 0;

            return left / right;
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            if (right == 0u)
                return 0u;

            return left / right;
        }

        protected override bool ProcessBinaryOperation(bool left, bool right)
        {
            throw new NotImplementedException();
        }

        sealed protected override string GetBinaryOperationCode(string left, string right, VFXValueType type)
        {
            return string.Format("{0} / {1}", left, right);
        }
    }

    class VFXExpressionSubtract : VFXExpressionBinaryNumericOperation
    {
        public VFXExpressionSubtract() : this(VFXValue<float>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSubtract(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.Subtract)
        {
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var zero = VFXOperatorUtility.ZeroExpression[reducedParents[0].valueType];
            if (zero.Equals(reducedParents[1]))
                return reducedParents[0];

            return base.Reduce(reducedParents);
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return left - right;
        }

        sealed protected override int ProcessBinaryOperation(int left, int right)
        {
            return left - right;
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left - right;
        }

        protected override bool ProcessBinaryOperation(bool left, bool right)
        {
            throw new NotImplementedException();
        }

        sealed protected override string GetBinaryOperationCode(string left, string right, VFXValueType type)
        {
            return string.Format("{0} - {1}", left, right);
        }
    }

    class VFXExpressionMin : VFXExpressionBinaryNumericOperation
    {
        public VFXExpressionMin() : this(VFXValue<float>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionMin(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.Min)
        {
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return Mathf.Min(left, right);
        }

        protected override int ProcessBinaryOperation(int left, int right)
        {
            return Mathf.Min(left, right);
        }

        protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left < right ? left : right;
        }

        protected override bool ProcessBinaryOperation(bool left, bool right)
        {
            return left ? right : left;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right, VFXValueType type)
        {
            if (type == VFXValueType.Uint32)
                return string.Format("{0} < {1} ? {0} : {1}", left, right);
            return string.Format("min({0}, {1})", left, right);
        }
    }

    class VFXExpressionMax : VFXExpressionBinaryNumericOperation
    {
        public VFXExpressionMax() : this(VFXValue<float>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionMax(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.Max)
        {
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return Mathf.Max(left, right);
        }

        protected override int ProcessBinaryOperation(int left, int right)
        {
            return Mathf.Max(left, right);
        }

        protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left > right ? left : right;
        }

        protected override bool ProcessBinaryOperation(bool left, bool right)
        {
            return left ? left : right;
        }

        protected override string GetBinaryOperationCode(string left, string right, VFXValueType type)
        {
            if (type == VFXValueType.Uint32)
                return string.Format("{0} > {1} ? {0} : {1}", left, right);
            return string.Format("max({0}, {1})", left, right);
        }
    }

    class VFXExpressionPow : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionPow() : this(VFXValue<float>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionPow(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.Pow)
        {
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return Mathf.Pow(left, right);
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("pow({0}, {1})", left, right);
        }
    }

    class VFXExpressionATan2 : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionATan2() : this(VFXValue<float>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionATan2(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.ATan2)
        {
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return Mathf.Atan2(left, right);
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("atan2({0}, {1})", left, right);
        }
    }

    class VFXExpressionBitwiseLeftShift : VFXExpressionBinaryUIntOperation
    {
        public VFXExpressionBitwiseLeftShift()
            : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseLeftShift(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.BitwiseLeftShift)
        {
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left << (int)right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} << {1}", left, right);
        }
    }

    class VFXExpressionBitwiseRightShift : VFXExpressionBinaryUIntOperation
    {
        public VFXExpressionBitwiseRightShift() : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseRightShift(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.BitwiseRightShift)
        {
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left >> (int)right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} >> {1}", left, right);
        }
    }

    class VFXExpressionBitwiseOr : VFXExpressionBinaryUIntOperation
    {
        public VFXExpressionBitwiseOr() : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseOr(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.BitwiseOr)
        {
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left | right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} | {1}", left, right);
        }
    }

    class VFXExpressionBitwiseAnd : VFXExpressionBinaryUIntOperation
    {
        public VFXExpressionBitwiseAnd() : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseAnd(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.BitwiseAnd)
        {
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left & right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} & {1}", left, right);
        }
    }

    class VFXExpressionBitwiseXor : VFXExpressionBinaryUIntOperation
    {
        public VFXExpressionBitwiseXor() : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseXor(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.BitwiseXor)
        {
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left ^ right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} ^ {1}", left, right);
        }
    }

    class VFXExpressionBitwiseComplement : VFXExpressionUnaryUIntOperation
    {
        public VFXExpressionBitwiseComplement() : this(VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseComplement(VFXExpression parent) : base(parent, VFXExpressionOperation.BitwiseComplement)
        {
        }

        sealed protected override uint ProcessUnaryOperation(uint input)
        {
            return ~input;
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("~{0}", x);
        }
    }

    class VFXExpressionLogicalAnd : VFXExpressionBinaryBoolOperation
    {
        public VFXExpressionLogicalAnd() : this(VFXValue<bool>.Default, VFXValue<bool>.Default)
        {
        }

        public VFXExpressionLogicalAnd(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.LogicalAnd)
        {
        }

        sealed protected override bool ProcessBinaryOperation(bool left, bool right)
        {
            return left && right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} && {1}", left, right);
        }
    }

    class VFXExpressionLogicalOr : VFXExpressionBinaryBoolOperation
    {
        public VFXExpressionLogicalOr() : this(VFXValue<bool>.Default, VFXValue<bool>.Default)
        {
        }

        public VFXExpressionLogicalOr(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOperation.LogicalOr)
        {
        }

        sealed protected override bool ProcessBinaryOperation(bool left, bool right)
        {
            return left || right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} || {1}", left, right);
        }
    }

    class VFXExpressionLogicalNot : VFXExpressionUnaryBoolOperation
    {
        public VFXExpressionLogicalNot() : this(VFXValue<bool>.Default)
        {
        }

        public VFXExpressionLogicalNot(VFXExpression parent) : base(parent, VFXExpressionOperation.LogicalNot)
        {
        }

        sealed protected override bool ProcessUnaryOperation(bool input)
        {
            return !input;
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("!{0}", x);
        }
    }
}
