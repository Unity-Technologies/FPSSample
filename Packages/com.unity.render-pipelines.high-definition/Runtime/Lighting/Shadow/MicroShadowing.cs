using System;
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class MicroShadowing : VolumeComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter opacity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
    }
}
