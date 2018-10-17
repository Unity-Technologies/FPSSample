using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum DebugMipMapMode
    {
        None,
        MipRatio,
        MipCount,
        MipCountReduction,
        StreamingMipBudget,
        StreamingMip
    }

    [GenerateHLSL]
    public enum DebugMipMapModeTerrainTexture
    {
        Control,
        Layer0,
        Layer1,
        Layer2,
        Layer3,
        Layer4,
        Layer5,
        Layer6,
        Layer7
    }

    [Serializable]
    public class MipMapDebugSettings
    {
        public DebugMipMapMode debugMipMapMode = DebugMipMapMode.None;
        public DebugMipMapModeTerrainTexture terrainTexture = DebugMipMapModeTerrainTexture.Control;

        public bool IsDebugDisplayEnabled()
        {
            return debugMipMapMode != DebugMipMapMode.None;
        }

        public void OnValidate()
        {
        }
    }
}
