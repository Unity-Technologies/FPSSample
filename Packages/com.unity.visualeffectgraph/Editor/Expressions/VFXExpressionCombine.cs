using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;


namespace UnityEditor.VFX
{
    class VFXExpressionCombine : VFXExpressionNumericOperation
    {
        public VFXExpressionCombine() : this(VFXValue<float>.Default, VFXValue<float>.Default)
        {}

        public VFXExpressionCombine(params VFXExpression[] parents)
            : base(parents)
        {
            if (parents.Length <= 1 || parents.Length > 4 || parents.Any(o => !IsFloatValueType(o.valueType)))
            {
                throw new ArgumentException("Incorrect VFXExpressionCombine");
            }

            switch (parents.Length)
            {
                case 2:
                    m_Operation = VFXExpressionOperation.Combine2f;
                    break;
                case 3:
                    m_Operation = VFXExpressionOperation.Combine3f;
                    break;
                case 4:
                    m_Operation = VFXExpressionOperation.Combine4f;
                    break;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            var constParentFloat = reducedParents.Cast<VFXValue<float>>().Select(o => o.Get()).ToArray();
            if (constParentFloat.Length != parents.Length)
            {
                throw new ArgumentException("Incorrect VFXExpressionCombine.ExecuteConstantOperation");
            }

            switch (parents.Length)
            {
                case 2: return VFXValue.Constant(new Vector2(constParentFloat[0], constParentFloat[1]));
                case 3: return VFXValue.Constant(new Vector3(constParentFloat[0], constParentFloat[1], constParentFloat[2]));
                case 4: return VFXValue.Constant(new Vector4(constParentFloat[0], constParentFloat[1], constParentFloat[2], constParentFloat[3]));
            }
            return null;
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return string.Format("{0}({1})", TypeToCode(valueType), parents.Aggregate((a, b) => string.Format("{0}, {1}", a, b)));
        }
    }
}
