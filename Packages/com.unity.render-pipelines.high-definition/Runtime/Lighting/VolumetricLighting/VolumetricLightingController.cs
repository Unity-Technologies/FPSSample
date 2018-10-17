namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class VolumetricLightingController : VolumeComponent
    {
        [Tooltip("Near and far planes of camera's volumetric lighting buffers (in meters).")]
        public FloatRangeParameter depthRange = new FloatRangeParameter(new Vector2(0.5f, 64.0f), 0.01f, 10000.0f);
        [Tooltip("Controls the slice distribution: 0 = exponential (more slices near the camera, fewer slices far away), 1 = linear (uniform spacing).")]
        public ClampedFloatParameter depthDistributionUniformity = new ClampedFloatParameter(0.75f, 0, 1);
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
