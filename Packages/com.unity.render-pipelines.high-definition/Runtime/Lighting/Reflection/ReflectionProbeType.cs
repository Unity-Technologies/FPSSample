using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Flags]
    public enum ReflectionProbeType
    {
        None = 0,
        ReflectionProbe = 1 << 0,
        PlanarReflection = 1 << 1
    }
}
