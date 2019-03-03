using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class BuiltInVariant : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "m_expressionOp", VFXBuiltInExpression.All.Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "BuiltIn", variantProvider = typeof(BuiltInVariant))]
    class VFXBuiltInParameter : VFXOperator
    {
        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.None)]
        protected VFXExpressionOperation m_expressionOp;

        override public string name { get { return m_expressionOp.ToString(); } }

        private Type GetOutputType()
        {
            switch (m_expressionOp)
            {
                case VFXExpressionOperation.LocalToWorld:
                case VFXExpressionOperation.WorldToLocal:
                    return typeof(Transform);
                default:
                {
                    var exp = VFXBuiltInExpression.Find(m_expressionOp);
                    if (exp != null)
                        return VFXExpression.TypeToType(VFXBuiltInExpression.Find(m_expressionOp).valueType);
                    return null;
                }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                Type outputType = GetOutputType();
                if (outputType != null)
                    yield return new VFXPropertyWithValue(new VFXProperty(outputType, m_expressionOp.ToString()));
            }
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var expression = VFXBuiltInExpression.Find(m_expressionOp);
            if (expression == null)
                return new VFXExpression[] {};
            return new VFXExpression[] { expression };
        }
    }
}
