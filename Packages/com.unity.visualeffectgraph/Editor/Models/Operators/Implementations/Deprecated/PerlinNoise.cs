using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    // DEPRECATED
    class PerlinNoise : NoiseBaseOld
    {
        override protected string noiseName { get { return "Perlin"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression parameters = new VFXExpressionCombine(inputExpression[1], inputExpression[2], inputExpression[4]);

            if (type == NoiseType.Curl)
            {
                if (curlDimensions == CurlDimensionCount.Two)
                {
                    return new[] { new VFXExpressionPerlinCurlNoise2D(inputExpression[0], parameters, inputExpression[3]) };
                }
                else
                {
                    return new[] { new VFXExpressionPerlinCurlNoise3D(inputExpression[0], parameters, inputExpression[3]) };
                }
            }
            else
            {
                VFXExpression rangeMultiplier = (inputExpression[5].y - inputExpression[5].x);

                if (dimensions == DimensionCount.One)
                {
                    VFXExpression noise = new VFXExpressionPerlinNoise1D(inputExpression[0], parameters, inputExpression[3]);
                    VFXExpression x = VFXOperatorUtility.Fit(noise.x, VFXValue.Constant(0.0f), VFXValue.Constant(1.0f), inputExpression[5].x, inputExpression[5].y);
                    VFXExpression y = noise.y * rangeMultiplier;
                    return new[] { x, y };
                }
                else if (dimensions == DimensionCount.Two)
                {
                    VFXExpression noise = new VFXExpressionPerlinNoise2D(inputExpression[0], parameters, inputExpression[3]);
                    VFXExpression x = VFXOperatorUtility.Fit(noise.x, VFXValue.Constant(0.0f), VFXValue.Constant(1.0f), inputExpression[5].x, inputExpression[5].y);
                    VFXExpression y = noise.y * rangeMultiplier;
                    VFXExpression z = noise.z * rangeMultiplier;
                    return new[] { x, new VFXExpressionCombine(y, z) };
                }
                else
                {
                    VFXExpression noise = new VFXExpressionPerlinNoise3D(inputExpression[0], parameters, inputExpression[3]);
                    VFXExpression x = VFXOperatorUtility.Fit(noise.x, VFXValue.Constant(0.0f), VFXValue.Constant(1.0f), inputExpression[5].x, inputExpression[5].y);
                    VFXExpression y = noise.y * rangeMultiplier;
                    VFXExpression z = noise.z * rangeMultiplier;
                    VFXExpression w = noise.w * rangeMultiplier;
                    return new[] { x, new VFXExpressionCombine(y, z, w) };
                }
            }
        }

        public override void Sanitize(int version)
        {
            Debug.Log("Sanitizing Graph: Automatically replace PerlinNoise with Noise or CurlNoise");
            Sanitize(NoiseBase.NoiseType.Perlin);
            base.Sanitize(version);
        }
    }
}
