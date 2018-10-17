using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // For multiple importance sampling
    // TODO: not working currently, will be updated later
    [GenerateHLSL(PackingRules.Exact)]
    public enum LightSamplingParameters
    {
        TextureHeight = 256,
        TextureWidth = 512
    }
}
