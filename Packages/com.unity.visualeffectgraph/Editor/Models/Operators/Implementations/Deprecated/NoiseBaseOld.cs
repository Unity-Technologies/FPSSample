using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    abstract class NoiseBaseOld : VFXOperator
    {
        public class InputProperties1D
        {
            [Tooltip("The coordinate in the noise field to take the sample from.")]
            public float coordinate = 0.0f;
        }

        public class InputProperties2D
        {
            [Tooltip("The coordinate in the noise field to take the sample from.")]
            public Vector2 coordinate = Vector2.zero;
        }

        public class InputProperties3D
        {
            [Tooltip("The coordinate in the noise field to take the sample from.")]
            public Vector3 coordinate = Vector3.zero;
        }

        public class InputPropertiesCommon
        {
            [Tooltip("The magnitude of the noise.")]
            public float amplitude = 1.0f;
            [Min(0.0f), Tooltip("The frequency of the noise.")]
            public float frequency = 1.0f;
            [/*Range(1, 8),*/ Tooltip("The number of layers of noise.")]
            public int octaves = 1;
            [Range(0.0f, 1.0f), Tooltip("The scaling factor applied to each octave.")]
            public float persistence = 0.5f;
        }

        public class InputPropertiesRange
        {
            [Tooltip("The noise will be calculated within the specified range. The amplitude is multiplied into the noise after fitting the noise into this range.")]
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

        public class OutputPropertiesCurl2D
        {
            [Tooltip("The calculated noise vector.")]
            public Vector2 Noise = Vector2.zero;
        }

        public class OutputPropertiesCurl3D
        {
            [Tooltip("The calculated noise vector.")]
            public Vector3 Noise = Vector3.zero;
        }

        public enum NoiseType
        {
            Default,
            Curl
        }

        public enum DimensionCount
        {
            One,
            Two,
            Three
        }

        public enum CurlDimensionCount
        {
            Two,
            Three
        }

        [VFXSetting, Tooltip("Generate basic noise in a specified number of dimensions, or generate Curl noise vectors.")]
        public NoiseType type = NoiseType.Default;

        [VFXSetting, Tooltip("Output noise in 1, 2 or 3 dinmensions.")]
        public DimensionCount dimensions = DimensionCount.Two;

        [VFXSetting, Tooltip("Output curl noise in 2 or 3 dinmensions.")]
        public CurlDimensionCount curlDimensions = CurlDimensionCount.Two;

        override public string name
        {
            get
            {
                if (type == NoiseType.Curl)
                    return noiseName + " Curl Noise " + (((int)curlDimensions) + 2) + "D";
                return noiseName + " Noise " + (((int)dimensions) + 1) + "D";
            }
        }

        override public string libraryName
        {
            get
            {
                if (type == NoiseType.Curl)
                    return noiseName + " Curl Noise";
                return noiseName + " Noise";
            }
        }

        protected abstract string noiseName { get; }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (type == NoiseType.Curl)
                    yield return "dimensions";
                else
                    yield return "curlDimensions";

                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = null;

                if (type == NoiseType.Curl)
                {
                    if (curlDimensions == CurlDimensionCount.Two)
                        properties = PropertiesFromType("InputProperties2D");
                    else
                        properties = PropertiesFromType("InputProperties3D");

                    properties = properties.Concat(PropertiesFromType("InputPropertiesCommon"));

                }
                else
                {
                    if (dimensions == DimensionCount.One)
                        properties = PropertiesFromType("InputProperties1D");
                    else if (dimensions == DimensionCount.Two)
                        properties = PropertiesFromType("InputProperties2D");
                    else
                        properties = PropertiesFromType("InputProperties3D");

                    properties = properties.Concat(PropertiesFromType("InputPropertiesCommon"));
                    properties = properties.Concat(PropertiesFromType("InputPropertiesRange"));
                }

                return properties;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                if (type == NoiseType.Curl)
                {
                    if (curlDimensions == CurlDimensionCount.Two)
                        return PropertiesFromType("OutputPropertiesCurl2D");
                    else
                        return PropertiesFromType("OutputPropertiesCurl3D");
                }

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

        protected void Sanitize(NoiseBase.NoiseType noiseType)
        {
            if (type == NoiseType.Default)
            {
                var noise = CreateInstance<Noise>();

                noise.SetSettingValue("type", noiseType);
                noise.SetSettingValue("dimensions", (Noise.DimensionCount)dimensions);

                // Transfer links
                VFXSlot.CopyLinksAndValue(noise.GetInputSlot(0), GetInputSlot(0), true);
                VFXSlot.CopyLinksAndValue(noise.GetInputSlot(1), GetInputSlot(2), true);
                VFXSlot.CopyLinksAndValue(noise.GetInputSlot(2), GetInputSlot(3), true);
                VFXSlot.CopyLinksAndValue(noise.GetInputSlot(3), GetInputSlot(4), true);
                VFXSlot.CopyLinksAndValue(noise.GetInputSlot(5), GetInputSlot(5), true);

                VFXSlot.CopyLinksAndValue(noise.GetOutputSlot(0), GetOutputSlot(0), true);
                VFXSlot.CopyLinksAndValue(noise.GetOutputSlot(1), GetOutputSlot(1), true);

                ReplaceModel(noise, this);
            }
            else
            {
                var noise = CreateInstance<CurlNoise>();

                noise.SetSettingValue("type", noiseType);
                noise.SetSettingValue("dimensions", (CurlNoise.DimensionCount)curlDimensions);

                // Transfer links
                VFXSlot.CopyLinksAndValue(noise.GetInputSlot(0), GetInputSlot(0), true);
                VFXSlot.CopyLinksAndValue(noise.GetInputSlot(1), GetInputSlot(2), true);
                VFXSlot.CopyLinksAndValue(noise.GetInputSlot(2), GetInputSlot(3), true);
                VFXSlot.CopyLinksAndValue(noise.GetInputSlot(3), GetInputSlot(4), true);
                VFXSlot.CopyLinksAndValue(noise.GetInputSlot(5), GetInputSlot(1), true);

                VFXSlot.CopyLinksAndValue(noise.GetOutputSlot(0), GetOutputSlot(0), true);

                ReplaceModel(noise, this);
            }
        }
    }
}
