using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;
namespace UnityEditor.VFX
{
    class VFXExpressionExtractComponent : VFXExpressionNumericOperation
    {
        public VFXExpressionExtractComponent() : this(VFXValue<Vector4>.Default, 0) {}

        public VFXExpressionExtractComponent(VFXExpression parent, int iChannel)
            : base(new VFXExpression[1] { parent })
        {
            if (parent.valueType == VFXValueType.Float || !IsFloatValueType(parent.valueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionExtractComponent");
            }

            m_Operation = VFXExpressionOperation.ExtractComponent;
            m_additionnalOperands = new int[] { iChannel, TypeToSize(parent.valueType) };
        }

        private int channel { get { return m_additionnalOperands[0]; } }

        static private float GetChannel(Vector2 input, int iChannel)
        {
            switch (iChannel)
            {
                case 0: return input.x;
                case 1: return input.y;
            }
            Debug.LogError("Incorrect channel (Vector2)");
            return 0.0f;
        }

        static private float GetChannel(Vector3 input, int iChannel)
        {
            switch (iChannel)
            {
                case 0: return input.x;
                case 1: return input.y;
                case 2: return input.z;
            }
            Debug.LogError("Incorrect channel (Vector2)");
            return 0.0f;
        }

        static private float GetChannel(Vector4 input, int iChannel)
        {
            switch (iChannel)
            {
                case 0: return input.x;
                case 1: return input.y;
                case 2: return input.z;
                case 3: return input.w;
            }
            Debug.LogError("Incorrect channel (Vector2)");
            return 0.0f;
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            float readValue = 0.0f;
            var parent = reducedParents[0];
            switch (reducedParents[0].valueType)
            {
                case VFXValueType.Float: readValue = parent.Get<float>(); break;
                case VFXValueType.Float2: readValue = GetChannel(parent.Get<Vector2>(), channel); break;
                case VFXValueType.Float3: readValue = GetChannel(parent.Get<Vector3>(), channel); break;
                case VFXValueType.Float4: readValue = GetChannel(parent.Get<Vector4>(), channel); break;
            }
            return VFXValue.Constant(readValue);
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var parent = reducedParents[0];
            if (parent is VFXExpressionCombine)
                return parent.parents[channel];
            else if (parent.valueType == VFXValueType.Float && channel == 0)
                return parent;
            else
                return base.Reduce(reducedParents);
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return string.Format("{0}[{1}]", parents[0], channel);
        }
    }
}
