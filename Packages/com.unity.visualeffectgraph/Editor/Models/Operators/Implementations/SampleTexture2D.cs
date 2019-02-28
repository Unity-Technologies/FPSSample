using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling")]
    class SampleTexture2D : VFXOperator
    {
        override public string name { get { return "Sample Texture2D"; } }

        public class InputProperties
        {
            [Tooltip("The texture to sample from.")]
            public Texture2D texture = null;
            [Tooltip("The texture coordinate used for the sampling.")]
            public Vector2 uv = Vector2.zero;
            [Min(0), Tooltip("The mip level to sample from.")]
            public float mipLevel = 0.0f;
        }

        public class OutputProperties
        {
            public Vector4 s = Vector4.zero;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSampleTexture2D(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
