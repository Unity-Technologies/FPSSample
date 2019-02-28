using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionCastUintToFloat : VFXExpression
    {
        public VFXExpressionCastUintToFloat() : this(VFXValue<uint>.Default)
        {
        }

        public VFXExpressionCastUintToFloat(VFXExpression from) : base(Flags.None, new VFXExpression[1] { from })
        {
            if (from.valueType != VFXValueType.Uint32)
                throw new InvalidCastException("Invalid VFXExpressionCastUintToFloat");
        }

        sealed public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.CastUintToFloat;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            return VFXValue.Constant((float)reducedParents[0].Get<uint>());
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return string.Format("(float){0}", parents[0]);
        }
    }

    class VFXExpressionCastIntToFloat : VFXExpression
    {
        public VFXExpressionCastIntToFloat() : this(VFXValue<int>.Default)
        {
        }

        public VFXExpressionCastIntToFloat(VFXExpression from) : base(Flags.None, new VFXExpression[1] { from })
        {
            if (from.valueType != VFXValueType.Int32)
                throw new InvalidCastException("Invalid VFXExpressionCastIntToFloat");
        }

        sealed public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.CastIntToFloat;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            return VFXValue.Constant((float)reducedParents[0].Get<int>());
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return string.Format("(float){0}", parents[0]);
        }
    }

    class VFXExpressionCastFloatToUint : VFXExpression
    {
        public VFXExpressionCastFloatToUint() : this(VFXValue<float>.Default)
        {
        }

        public VFXExpressionCastFloatToUint(VFXExpression from) : base(Flags.None, new VFXExpression[1] { from })
        {
            if (from.valueType != VFXValueType.Float)
                throw new InvalidCastException("Invalid VFXExpressionCastFloatToUint");
        }

        sealed public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.CastFloatToUint;
            }
        }
        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            return VFXValue.Constant((uint)reducedParents[0].Get<float>());
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return string.Format("(uint){0}", parents[0]);
        }
    }

    class VFXExpressionCastIntToUint : VFXExpression
    {
        public VFXExpressionCastIntToUint() : this(VFXValue<int>.Default)
        {
        }

        public VFXExpressionCastIntToUint(VFXExpression from) : base(Flags.None, new VFXExpression[1] { from })
        {
            if (from.valueType != VFXValueType.Int32)
                throw new InvalidCastException("Invalid VFXExpressionCastIntToUint");
        }

        sealed public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.CastIntToUint;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            return VFXValue.Constant((uint)reducedParents[0].Get<int>());
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return string.Format("(uint){0}", parents[0]);
        }
    }

    class VFXExpressionCastFloatToInt : VFXExpression
    {
        public VFXExpressionCastFloatToInt() : this(VFXValue<float>.Default)
        {
        }

        public VFXExpressionCastFloatToInt(VFXExpression from) : base(Flags.None, new VFXExpression[1] { from })
        {
            if (from.valueType != VFXValueType.Float)
                throw new InvalidCastException("Invalid VFXExpressionCastFloatToInt");
        }

        sealed public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.CastFloatToInt;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            return VFXValue.Constant((int)reducedParents[0].Get<float>());
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return string.Format("(int){0}", parents[0]);
        }
    }

    class VFXExpressionCastUintToInt : VFXExpression
    {
        public VFXExpressionCastUintToInt() : this(VFXValue<uint>.Default)
        {
        }

        public VFXExpressionCastUintToInt(VFXExpression from) : base(Flags.None, new VFXExpression[1] { from })
        {
            if (from.valueType != VFXValueType.Uint32)
                throw new InvalidCastException("Invalid VFXExpressionCastUintToInt");
        }

        sealed public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.CastUintToInt;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            return VFXValue.Constant((int)reducedParents[0].Get<uint>());
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return string.Format("(int){0}", parents[0]);
        }
    }
}
