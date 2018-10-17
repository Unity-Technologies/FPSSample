using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // RenderRenderPipelineSettings represent settings that are immutable at runtime.
    // There is a dedicated RenderRenderPipelineSettings for each platform

    [Serializable]
    public class GlobalDecalSettings
    {
        public int drawDistance = 1000;
        public int atlasWidth = 4096;
        public int atlasHeight = 4096;
        public bool perChannelMask = false;
    }
}
