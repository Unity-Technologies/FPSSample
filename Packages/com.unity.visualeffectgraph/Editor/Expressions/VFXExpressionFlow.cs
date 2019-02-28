using System;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    // Must match enum in C++
    public enum VFXCondition
    {
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual,
    }

    class VFXExpressionCondition : VFXExpression
    {
        public VFXExpressionCondition()
            : this(VFXCondition.Equal, VFXValue.Constant(0.0f), VFXValue.Constant(0.0f))
        {}

        public VFXExpressionCondition(VFXCondition cond, VFXExpression left, VFXExpression right) : base(VFXExpression.Flags.None, new VFXExpression[] { left, right })
        {
            condition = cond;
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.Condition;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            bool res = false;
            float left = constParents[0].Get<float>();
            float right = constParents[1].Get<float>();

            switch (condition)
            {
                case VFXCondition.Equal:            res = left == right;    break;
                case VFXCondition.NotEqual:         res = left != right;    break;
                case VFXCondition.Less:             res = left < right;     break;
                case VFXCondition.LessOrEqual:      res = left <= right;    break;
                case VFXCondition.Greater:          res = left > right;     break;
                case VFXCondition.GreaterOrEqual:   res = left >= right;    break;
            }

            return VFXValue.Constant<bool>(res);
        }

        public override string GetCodeString(string[] parents)
        {
            string comparator = null;
            switch (condition)
            {
                case VFXCondition.Equal:            comparator = "==";  break;
                case VFXCondition.NotEqual:         comparator = "!=";  break;
                case VFXCondition.Less:             comparator = "<";   break;
                case VFXCondition.LessOrEqual:      comparator = "<=";  break;
                case VFXCondition.Greater:          comparator = ">";   break;
                case VFXCondition.GreaterOrEqual:   comparator = ">=";  break;
            }

            return string.Format("{0} {1} {2}", parents[0], comparator, parents[1]);
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var newExpression = (VFXExpressionCondition)base.Reduce(reducedParents);
            newExpression.condition = condition;
            return newExpression;
        }

        protected override int[] additionnalOperands { get { return new int[] { (int)condition }; } }
        private VFXCondition condition;
    }

    class VFXExpressionBranch : VFXExpression
    {
        public VFXExpressionBranch()
            : this(VFXValue.Constant(true), VFXValue.Constant(0.0f), VFXValue.Constant(0.0f))
        {}

        public VFXExpressionBranch(VFXExpression pred, VFXExpression trueExp, VFXExpression falseExp)
            : base(VFXExpression.Flags.None, new VFXExpression[] { pred, trueExp, falseExp })
        {
            if (parents[1].valueType != parents[2].valueType)
                throw new ArgumentException("both branch expressions must be of the same types");
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.Branch;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            bool pred = constParents[0].Get<bool>();
            return pred ? constParents[1] : constParents[2];
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("{0} ? {1} : {2}", parents);
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            if (reducedParents[0].Is(VFXExpression.Flags.Constant)) // detect static branching
                return Evaluate(reducedParents);
            return base.Reduce(reducedParents);
        }

        protected override int[] additionnalOperands { get { return new int[] { (int)parents[1].valueType }; } }
    }
}
