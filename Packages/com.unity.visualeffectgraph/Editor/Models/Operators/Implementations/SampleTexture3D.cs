using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling")]
    class SampleTexture3D : VFXOperator
    {
        override public string name { get { return "Sample Texture3D"; } }

        public class InputProperties
        {
            [Tooltip("The texture to sample from.")]
            public Texture3D texture = VFXResources.defaultResources.vectorField;
            [Tooltip("The texture coordinate used for the sampling.")]
            public Vector3 uvw = Vector3.zero;
            [Min(0), Tooltip("The mip level to sample from.")]
            public float mipLevel = 0.0f;
        }

        public class OutputProperties
        {
            public Vector4 s = Vector4.zero;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSampleTexture3D(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
