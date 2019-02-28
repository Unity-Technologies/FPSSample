using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Noise")]
    class VoroNoise2D : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The coordinate in the noise field to take the sample from.")]
            public Vector2 coordinate = Vector2.zero;
            [Tooltip("The magnitude of the noise.")]
            public float amplitude = 1.0f;
            [Min(0.0f), Tooltip("The frequency of the noise.")]
            public float frequency = 1.0f;
            [Range(0.0f, 1.0f), Tooltip("Warp the shape of the cells.")]
            public float warp = 0.0f;
            [Range(0.0f, 1.0f), Tooltip("Smooth or hard edges between cells.")]
            public float smoothness = 0.0f;
        }

        public class OutputProperties
        {
            public float o = 0.0f;
        }

        override public string name { get { return "VoroNoise 2D"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression parameters = new VFXExpressionCombine(inputExpression[2], inputExpression[3], inputExpression[4]);
            return new[] { new VFXExpressionVoroNoise2D(inputExpression[0], parameters) * inputExpression[1] };
        }
    }
}
