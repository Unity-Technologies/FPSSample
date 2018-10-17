using UnityEngine;
using System;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL(PackingRules.Exact)]
    public enum ShaderPass
    {
        GBuffer,
        Forward,
        ForwardUnlit,
        DeferredLighting,
        DepthOnly,
        Velocity,
        Distortion,
        LightTransport,
        Shadows,
        SubsurfaceScattering,
        VolumeVoxelization,
        VolumetricLighting,
        DbufferProjector,
        DbufferMesh
    }
}
