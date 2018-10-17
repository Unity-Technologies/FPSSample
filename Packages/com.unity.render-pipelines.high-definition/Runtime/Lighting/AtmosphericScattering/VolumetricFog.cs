using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class VolumetricFog : AtmosphericScattering
    {
        public ColorParameter        albedo                 = new ColorParameter(Color.white);
        public MinFloatParameter     meanFreePath           = new MinFloatParameter(1000000.0f, 1.0f);
        public ClampedFloatParameter anisotropy             = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        // Override the volume blending function.
        public override void Override(VolumeComponent state, float lerpFactor)
        {
            VolumetricFog other = state as VolumetricFog;

            float   thisExtinction  = VolumeRenderingUtils.ExtinctionFromMeanFreePath(meanFreePath);
            Vector3 thisScattering  = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(thisExtinction, (Vector3)(Vector4)albedo.value);

            float   otherExtinction = VolumeRenderingUtils.ExtinctionFromMeanFreePath(other.meanFreePath);
            Vector3 otherScattering = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(otherExtinction, (Vector3)(Vector4)other.albedo.value);

            float   blendExtinction =   Mathf.Lerp(otherExtinction,  thisExtinction, lerpFactor);
            Vector3 blendScattering = Vector3.Lerp(otherScattering,  thisScattering, lerpFactor);
            float   blendAsymmetry  =   Mathf.Lerp(other.anisotropy, anisotropy,     lerpFactor);

            float   blendMeanFreePath = VolumeRenderingUtils.MeanFreePathFromExtinction(blendExtinction);
            Color   blendAlbedo       = (Color)(Vector4)VolumeRenderingUtils.AlbedoFromMeanFreePathAndScattering(blendMeanFreePath, blendScattering);
            float   blendDimmer       = Mathf.Lerp(other.globalLightProbeDimmer, globalLightProbeDimmer, lerpFactor);

            blendAlbedo.a     = 1.0f;

            if (meanFreePath.overrideState)
            {
                other.meanFreePath.value = blendMeanFreePath;
            }

            if (albedo.overrideState)
            {
                other.albedo.value = blendAlbedo;
            }

            if (anisotropy.overrideState)
            {
                other.anisotropy.value = blendAsymmetry;
            }

            if (globalLightProbeDimmer.overrideState)
            {
                other.globalLightProbeDimmer.value = blendDimmer;
            }
        }

        public override void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
            DensityVolumeArtistParameters param = new DensityVolumeArtistParameters(albedo, meanFreePath, anisotropy);

            DensityVolumeEngineData data = param.ConvertToEngineData();

            cmd.SetGlobalInt(HDShaderIDs._AtmosphericScatteringType, (int)FogType.Volumetric);

            cmd.SetGlobalVector(HDShaderIDs._GlobalScattering, data.scattering);
            cmd.SetGlobalFloat(HDShaderIDs._GlobalExtinction, data.extinction);
            cmd.SetGlobalFloat(HDShaderIDs._GlobalAnisotropy, anisotropy);
        }
    }
}
