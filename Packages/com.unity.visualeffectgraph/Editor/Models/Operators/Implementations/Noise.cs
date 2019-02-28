using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class NoiseVariantProvider : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "type", Enum.GetValues(typeof(NoiseBase.NoiseType)).Cast<object>().ToArray() },
                    { "dimensions", Enum.GetValues(typeof(Noise.DimensionCount)).Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "Noise", variantProvider = typeof(NoiseVariantProvider))]
    class Noise : NoiseBase
    {
        public class InputPropertiesRange
        {
            [Tooltip("The noise will be calculated within the specified range.")]
            public Vector2 range = new Vector2(-1.0f, 1.0f);
        }

        public class OutputPropertiesCommon
        {
            [Tooltip("The calculated noise.")]
            public float Noise = 0.0f;
        }

        public class OutputProperties1D
        {
            [Tooltip("The rate of change of the noise.")]
            public float Derivatives = 0.0f;
        }

        public class OutputProperties2D
        {
            [Tooltip("The rate of change of the noise.")]
            public Vector2 Derivatives = Vector2.zero;
        }

        public class OutputProperties3D
        {
            [Tooltip("The rate of change of the noise.")]
            public Vector3 Derivatives = Vector3.zero;
        }

        public enum DimensionCount
        {
            One,
            Two,
            Three
        }

        [VFXSetting, Tooltip("Output noise in 1, 2 or 3 dimensions.")]
        public DimensionCount dimensions = DimensionCount.Two;

        override public string name
        {
            get
            {
                return type.ToString() + " Noise " + (((int)dimensions) + 1) + "D";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = null;

                if (dimensions == DimensionCount.One)
                    properties = PropertiesFromType("InputProperties1D");
                else if (dimensions == DimensionCount.Two)
                    properties = PropertiesFromType("InputProperties2D");
                else
                    properties = PropertiesFromType("InputProperties3D");

                properties = properties.Concat(PropertiesFromType("InputPropertiesCommon"));
                properties = properties.Concat(PropertiesFromType("InputPropertiesRange"));

                return properties;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = PropertiesFromType("OutputPropertiesCommon");
                if (dimensions == DimensionCount.One)
                    properties = properties.Concat(PropertiesFromType("OutputProperties1D"));
                else if (dimensions == DimensionCount.Two)
                    properties = properties.Concat(PropertiesFromType("OutputProperties2D"));
                else
                    properties = properties.Concat(PropertiesFromType("OutputProperties3D"));

                return properties;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression parameters = new VFXExpressionCombine(inputExpression[1], inputExpression[3], inputExpression[4]);
            VFXExpression rangeMultiplier = (inputExpression[5].y - inputExpression[5].x);

            VFXExpression result;
            VFXExpression rangeMin = VFXValue.Constant(0.0f);
            VFXExpression rangeMax = VFXValue.Constant(1.0f);

            if (dimensions == DimensionCount.One)
            {
                if (type == NoiseType.Value)
                {
                    result = new VFXExpressionValueNoise1D(inputExpression[0], parameters, inputExpression[2]);
                }
                else if (type == NoiseType.Perlin)
                {
                    result = new VFXExpressionPerlinNoise1D(inputExpression[0], parameters, inputExpression[2]);
                    rangeMin = VFXValue.Constant(-1.0f);
                }
                else
                {
                    result = new VFXExpressionCellularNoise1D(inputExpression[0], parameters, inputExpression[2]);
                }

                VFXExpression x = VFXOperatorUtility.Fit(result.x, rangeMin, rangeMax, inputExpression[5].x, inputExpression[5].y);
                VFXExpression y = result.y * rangeMultiplier;
                return new[] { x, y };
            }
            else if (dimensions == DimensionCount.Two)
            {
                if (type == NoiseType.Value)
                {
                    result = new VFXExpressionValueNoise2D(inputExpression[0], parameters, inputExpression[2]);
                }
                else if (type == NoiseType.Perlin)
                {
                    result = new VFXExpressionPerlinNoise2D(inputExpression[0], parameters, inputExpression[2]);
                    rangeMin = VFXValue.Constant(-1.0f);
                }
                else
                {
                    result = new VFXExpressionCellularNoise2D(inputExpression[0], parameters, inputExpression[2]);
                }

                VFXExpression x = VFXOperatorUtility.Fit(result.x, rangeMin, rangeMax, inputExpression[5].x, inputExpression[5].y);
                VFXExpression y = result.y * rangeMultiplier;
                VFXExpression z = result.z * rangeMultiplier;
                return new[] { x, new VFXExpressionCombine(y, z) };
            }
            else
            {
                if (type == NoiseType.Value)
                {
                    result = new VFXExpressionValueNoise3D(inputExpression[0], parameters, inputExpression[2]);
                }
                else if (type == NoiseType.Perlin)
                {
                    result = new VFXExpressionPerlinNoise3D(inputExpression[0], parameters, inputExpression[2]);
                    rangeMin = VFXValue.Constant(-1.0f);
                }
                else
                {
                    result = new VFXExpressionCellularNoise3D(inputExpression[0], parameters, inputExpression[2]);
                }

                VFXExpression x = VFXOperatorUtility.Fit(result.x, rangeMin, rangeMax, inputExpression[5].x, inputExpression[5].y);
                VFXExpression y = result.y * rangeMultiplier;
                VFXExpression z = result.z * rangeMultiplier;
                VFXExpression w = result.w * rangeMultiplier;
                return new[] { x, new VFXExpressionCombine(y, z, w) };
            }
        }
    }
}
