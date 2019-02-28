using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class VolumetricLightingController : VolumeComponent
    {
        [Tooltip("Distance from the near plane of the camera to the back of the camera's volumetric lighting buffer (in meters).")]
        public MinFloatParameter depthExtent = new MinFloatParameter(64.0f, 0.1f);
        [Tooltip("Controls the slice distribution: 0 = exponential (more slices near the camera, fewer slices far away), 1 = linear (uniform spacing).")]
        [FormerlySerializedAs("depthDistributionUniformity")]
        public ClampedFloatParameter sliceDistributionUniformity = new ClampedFloatParameter(0.75f, 0, 1);
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
