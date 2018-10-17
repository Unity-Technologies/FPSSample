using System;
using System.Diagnostics;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // Keep this class first in the file. Otherwise it seems that the script type is not registered properly.
    public abstract class AtmosphericScattering : VolumeComponent
    {
        // Fog Color
        static readonly int m_ColorModeParam = Shader.PropertyToID("_FogColorMode");
        static readonly int m_FogColorDensityParam = Shader.PropertyToID("_FogColorDensity");
        static readonly int m_MipFogParam = Shader.PropertyToID("_MipFogParameters");

        // Fog Color
        public FogColorParameter       colorMode = new FogColorParameter(FogColorMode.SkyColor);
        [Tooltip("Constant Fog Color")]
        public ColorParameter          color = new ColorParameter(Color.grey);
        public ClampedFloatParameter   density = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        [Tooltip("Maximum mip map used for mip fog (0 being lowest and 1 highest mip).")]
        public ClampedFloatParameter   mipFogMaxMip = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        [Tooltip("Distance at which minimum mip of blurred sky texture is used as fog color.")]
        public MinFloatParameter       mipFogNear = new MinFloatParameter(0.0f, 0.0f);
        [Tooltip("Distance at which maximum mip of blurred sky texture is used as fog color.")]
        public MinFloatParameter       mipFogFar = new MinFloatParameter(1000.0f, 0.0f);

        public abstract void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd);

        public static void PushNeutralShaderParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
            cmd.SetGlobalInt(HDShaderIDs._AtmosphericScatteringType, (int)FogType.None);

            // In case volumetric lighting is enabled, we need to make sure that all rendering passes
            // (not just the atmospheric scattering one) receive neutral parameters.
            if (hdCamera.frameSettings.enableVolumetrics)
            {
                var data = DensityVolumeEngineData.GetNeutralValues();

                cmd.SetGlobalVector(HDShaderIDs._GlobalScattering, data.scattering);
                cmd.SetGlobalFloat(HDShaderIDs._GlobalExtinction, data.extinction);
                cmd.SetGlobalFloat(HDShaderIDs._GlobalAnisotropy, 0.0f);
            }
        }

        // Not used by the volumetric fog.
        public void PushShaderParametersCommon(HDCamera hdCamera, CommandBuffer cmd, FogType type)
        {
            Debug.Assert(hdCamera.frameSettings.enableAtmosphericScattering);

            cmd.SetGlobalInt(HDShaderIDs._AtmosphericScatteringType, (int)type);

            // Fog Color
            cmd.SetGlobalFloat(m_ColorModeParam, (float)colorMode.value);
            cmd.SetGlobalColor(m_FogColorDensityParam, new Color(color.value.r, color.value.g, color.value.b, density));
            cmd.SetGlobalVector(m_MipFogParam, new Vector4(mipFogNear, mipFogFar, mipFogMaxMip, 0.0f));
        }
    }

    [GenerateHLSL]
    public enum FogType
    {
        None,
        Linear,
        Exponential,
        Volumetric
    }

    [GenerateHLSL]
    public enum FogColorMode
    {
        ConstantColor,
        SkyColor,
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class FogTypeParameter : VolumeParameter<FogType>
    {
        public FogTypeParameter(FogType value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class FogColorParameter : VolumeParameter<FogColorMode>
    {
        public FogColorParameter(FogColorMode value, bool overrideState = false)
            : base(value, overrideState) {}
    }
}
