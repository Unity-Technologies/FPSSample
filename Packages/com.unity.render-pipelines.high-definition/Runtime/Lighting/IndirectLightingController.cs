using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class IndirectLightingController : VolumeComponent
    {
        public MinFloatParameter    indirectSpecularIntensity = new MinFloatParameter(1.0f, 0.0f);
        public MinFloatParameter    indirectDiffuseIntensity = new MinFloatParameter(1.0f, 0.0f);        
    }
}
