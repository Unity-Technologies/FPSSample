using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // This enum is just here to centralize UniqueID values for skies provided with HDRP
    public enum SkyType
    {
        HDRISky = 1,
        ProceduralSky = 2,
        Gradient = 3,
    }

    // Keep this class first in the file. Otherwise it seems that the script type is not registered properly.
    [Serializable]
    public sealed class VisualEnvironment : VolumeComponent
    {
        public IntParameter skyType = new IntParameter(0);
        public FogTypeParameter fogType = new FogTypeParameter(FogType.None);

        public void PushFogShaderParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.enableAtmosphericScattering)
            {
                AtmosphericScattering.PushNeutralShaderParameters(hdCamera, cmd);
                return;
            }

            switch (fogType.value)
            {
                case FogType.None:
                {
                    AtmosphericScattering.PushNeutralShaderParameters(hdCamera, cmd);
                    break;
                }
                case FogType.Linear:
                {
                    var fogSettings = VolumeManager.instance.stack.GetComponent<LinearFog>();
                    fogSettings.PushShaderParameters(hdCamera, cmd);
                    break;
                }
                case FogType.Exponential:
                {
                    var fogSettings = VolumeManager.instance.stack.GetComponent<ExponentialFog>();
                    fogSettings.PushShaderParameters(hdCamera, cmd);
                    break;
                }
                case FogType.Volumetric:
                {
                    var fogSettings = VolumeManager.instance.stack.GetComponent<VolumetricFog>();
                    fogSettings.PushShaderParameters(hdCamera, cmd);
                    break;
                }
            }
        }
    }
}
