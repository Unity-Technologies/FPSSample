using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    // DEPRECATED
    class SimplexNoise : NoiseBaseOld
    {
        override protected string noiseName { get { return "Simplex"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression parameters = new VFXExpressionCombine(inputExpression[1], inputExpression[2], inputExpression[4]);

            if (dimensions == DimensionCount.One)
            {
                VFXExpression noise = new VFXExpressionPerlinNoise1D(inputExpression[0], parameters, inputExpression[3]);
                noise = VFXOperatorUtility.Fit(noise, VFXValue.Constant(new Vector2(-1.0f, -1.0f)), VFXValue.Constant(Vector2.one), VFXOperatorUtility.CastFloat(inputExpression[5].x, noise.valueType), VFXOperatorUtility.CastFloat(inputExpression[5].y, noise.valueType));
                return new[] { noise.x, noise.y };
            }
            else if (dimensions == DimensionCount.Two)
            {
                VFXExpression noise = new VFXExpressionPerlinNoise2D(inputExpression[0], parameters, inputExpression[3]);
                noise = VFXOperatorUtility.Fit(noise, VFXValue.Constant(new Vector3(-1.0f, -1.0f, -1.0f)), VFXValue.Constant(Vector3.one), VFXOperatorUtility.CastFloat(inputExpression[5].x, noise.valueType), VFXOperatorUtility.CastFloat(inputExpression[5].y, noise.valueType));
                return new[] { noise.x, new VFXExpressionCombine(noise.y, noise.z) };
            }
            else
            {
                VFXExpression noise = new VFXExpressionPerlinNoise3D(inputExpression[0], parameters, inputExpression[3]);
                noise = VFXOperatorUtility.Fit(noise, VFXValue.Constant(new Vector4(-1.0f, -1.0f, -1.0f, -1.0f)), VFXValue.Constant(Vector4.one), VFXOperatorUtility.CastFloat(inputExpression[5].x, noise.valueType), VFXOperatorUtility.CastFloat(inputExpression[5].y, noise.valueType));
                return new[] { noise.x, new VFXExpressionCombine(noise.y, noise.z, noise.w) };
            }
        }

        public override void Sanitize(int version)
        {
            Debug.Log("Sanitizing Graph: Automatically replace SimplexNoise with PerlinNoise");

            var perlinNoise = CreateInstance<PerlinNoise>();

            perlinNoise.SetSettingValue("dimensions", dimensions);

            // Transfer links
            for (int i=0; i<6; i++)
                VFXSlot.CopyLinksAndValue(perlinNoise.GetInputSlot(i), GetInputSlot(i), true);

            VFXSlot.CopyLinksAndValue(perlinNoise.GetOutputSlot(0), GetOutputSlot(0), true);

            ReplaceModel(perlinNoise, this);

            base.Sanitize(version);
        }
    }
}
