using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    #pragma warning disable 0659
    sealed class VFXBuiltInExpression : VFXExpression
    {
        public static readonly VFXExpression TotalTime = new VFXBuiltInExpression(VFXExpressionOperation.TotalTime);
        public static readonly VFXExpression DeltaTime = new VFXBuiltInExpression(VFXExpressionOperation.DeltaTime);
        public static readonly VFXExpression SystemSeed = new VFXBuiltInExpression(VFXExpressionOperation.SystemSeed);
        public static readonly VFXExpression LocalToWorld = new VFXBuiltInExpression(VFXExpressionOperation.LocalToWorld);
        public static readonly VFXExpression WorldToLocal = new VFXBuiltInExpression(VFXExpressionOperation.WorldToLocal);

        private static readonly VFXExpression[] AllExpressions = VFXReflectionHelper.CollectStaticReadOnlyExpression<VFXExpression>(typeof(VFXBuiltInExpression));
        public static readonly VFXExpressionOperation[] All = AllExpressions.Select(e => e.operation).ToArray();

        public static VFXExpression Find(VFXExpressionOperation op)
        {
            var expression = AllExpressions.FirstOrDefault(e => e.operation == op);
            return expression;
        }

        private VFXExpressionOperation m_Operation;

        private VFXBuiltInExpression(VFXExpressionOperation op)
            : base(Flags.None)
        {
            m_Operation = op;
        }

        public sealed override VFXExpressionOperation operation
        {
            get
            {
                return m_Operation;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VFXBuiltInExpression))
                return false;

            var other = (VFXBuiltInExpression)obj;
            return valueType == other.valueType && operation == other.operation;
        }

        protected override int GetInnerHashCode()
        {
            return operation.GetHashCode();
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }
    }
    #pragma warning restore 0659
}
