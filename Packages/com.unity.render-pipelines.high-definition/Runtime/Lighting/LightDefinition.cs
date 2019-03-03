using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    //-----------------------------------------------------------------------------
    // structure definition
    //-----------------------------------------------------------------------------

    // Caution: Order is important and is use for optimization in light loop
    [GenerateHLSL]
    public enum GPULightType
    {
        Directional,
        Point,
        Spot,
        ProjectorPyramid,
        ProjectorBox,

        // AreaLight
        Tube, // Keep Line lights before Rectangle. This is needed because of a compiler bug (see LightLoop.hlsl)
        Rectangle,
        // Currently not supported in real time (just use for reference)
        // Sphere,
        // Disk,
    };

    public static class GPULightTypeExtension
    {
        public static bool IsAreaLight(this GPULightType lightType)
        {
            return lightType == GPULightType.Rectangle || lightType == GPULightType.Tube;
        }

        public static bool IsSpot(this GPULightType lightType)
        {
            return lightType == GPULightType.Spot || lightType == GPULightType.ProjectorBox || lightType == GPULightType.ProjectorPyramid;
        }
    }

    // This is use to distinguish between reflection and refraction probe in LightLoop
    [GenerateHLSL]
    public enum GPUImageBasedLightingType
    {
        Reflection,
        Refraction
    };

    // These structures share between C# and hlsl need to be align on float4, so we pad them.
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct DirectionalLightData
    {
        // Packing order depends on chronological access to avoid cache misses
        // Make sure to respect the 16-byte alignment
        public Vector3 positionRWS;
        public uint    lightLayers;

        public float   lightDimmer;
        public float   volumetricLightDimmer;   // Replaces 'lightDimer'
        public float   angleScale;              // Sun disk highlight
        public float   angleOffset;             // Sun disk highlight

        public Vector3 forward;
        public int     cookieIndex;             // -1 if unused (TODO: 16 bit)

        public Vector3 right;                   // Rescaled by (2 / shapeWidth)
        public int     tileCookie;              // (TODO: use a bitfield)

        public Vector3 up;                      // Rescaled by (2 / shapeHeight)
        public int     shadowIndex;             // -1 if unused (TODO: 16 bit)

        public Vector3 color;
        public int     contactShadowIndex;      // -1 if unused (TODO: 16 bit)

        public float   shadowDimmer;
        public float   volumetricShadowDimmer;  // Replaces 'shadowDimmer'
        public int     nonLightMappedOnly;      // Used with ShadowMask (TODO: use a bitfield)
        public float   minRoughness;            // Hack

        public Vector4 shadowMaskSelector;      // Used with ShadowMask feature

        public float   diffuseDimmer;
        public float   specularDimmer;
    };

    [GenerateHLSL(PackingRules.Exact, false)]
    public struct LightData
    {
        // Packing order depends on chronological access to avoid cache misses
        // Make sure to respect the 16-byte alignment
        public Vector3 positionRWS;
        public uint    lightLayers;

        public float   lightDimmer;
        public float   volumetricLightDimmer;   // Replaces 'lightDimer'
        public float   angleScale;              // Spot light
        public float   angleOffset;             // Spot light

        public Vector3 forward;
        public GPULightType lightType;          // TODO: move this up?

        public Vector3 right;                   // If spot: rescaled by cot(outerHalfAngle); if projector: rescaled by (2 / shapeWidth)
        public float   range;

        public Vector3 up;                      // If spot: rescaled by cot(outerHalfAngle); if projector: rescaled by (2 / shapeHeight)
        public float   rangeAttenuationScale;

        public Vector3 color;
        public float   rangeAttenuationBias;

        public int     cookieIndex;             // -1 if unused
        public int     tileCookie;              // (TODO: use a bitfield)
        public int     shadowIndex;             // -1 if unused (TODO: 16 bit)
        public int     contactShadowIndex;      // -1 if unused (TODO: 16 bit)

        public float   shadowDimmer;
        public float   volumetricShadowDimmer;  // Replaces 'shadowDimmer'
        public int     nonLightMappedOnly;      // Used with ShadowMask feature (TODO: use a bitfield)
        public float   minRoughness;            // This is use to give a small "area" to punctual light, as if we have a light with a radius.

        public Vector4 shadowMaskSelector;      // Used with ShadowMask feature

        public Vector2 size;                    // Used by area (X = length or width, Y = height) and punctual lights (X = radius)
        public float   diffuseDimmer;
        public float   specularDimmer;
    };


    [GenerateHLSL]
    public enum EnvShapeType
    {
        None,
        Box,
        Sphere,
        Sky
    };

    [GenerateHLSL]
    public enum EnvConstants
    {
        SpecCubeLodStep = 6
    }


    // Guideline for reflection volume: In HDRenderPipeline we separate the projection volume (the proxy of the scene) from the influence volume (what pixel on the screen is affected)
    // However we add the constrain that the shape of the projection and influence volume is the same (i.e if we have a sphere shape projection volume, we have a shape influence).
    // It allow to have more coherence for the dynamic if in shader code.
    // Users can also chose to not have any projection, in this case we use the property minProjectionDistance to minimize code change. minProjectionDistance is set to huge number
    // that simulate effect of no shape projection
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct EnvLightData
    {
        // Packing order depends on chronological access to avoid cache misses
        public uint lightLayers;

        // Proxy properties
        public Vector3 capturePositionRWS;
        public EnvShapeType influenceShapeType;

        // Box: extents = box extents
        // Sphere: extents.x = sphere radius
        public Vector3 proxyExtents;
        // User can chose if they use This is use in case we want to force infinite projection distance (i.e no projection);
        public float minProjectionDistance;

        public Vector3 proxyPositionRWS;
        public Vector3 proxyForward;
        public Vector3 proxyUp;
        public Vector3 proxyRight;

        // Influence properties
        public Vector3 influencePositionRWS;
        public Vector3 influenceForward;
        public Vector3 influenceUp;
        public Vector3 influenceRight;

        public Vector3 influenceExtents;
        public float unused00;

        public Vector3 blendDistancePositive;
        public Vector3 blendDistanceNegative;
        public Vector3 blendNormalDistancePositive;
        public Vector3 blendNormalDistanceNegative;

        public Vector3 boxSideFadePositive;
        public Vector3 boxSideFadeNegative;
        public float weight;
        public float multiplier;

        // Sampling properties
        public int envIndex;
    };

    [GenerateHLSL]
    public enum EnvCacheType
    {
        Texture2D,
        Cubemap
    }

    // Usage of StencilBits.Lighting on 2 bits.
    // We support both deferred and forward renderer.  Here is the current usage of this 2 bits:
    // 0. Everything except case below. This include any forward opaque object. No lighting in deferred lighting path.
    // 1. All deferred opaque object that require split lighting (i.e output both specular and diffuse in two different render target). Typically Subsurface scattering material.
    // 2. All deferred opaque object.
    // 3. unused
    [GenerateHLSL]
    // Caution: Value below are hardcoded in some shader (because properties doesn't support include). If order or value is change, please update corresponding ".shader"
    public enum StencilLightingUsage
    {
        NoLighting,
        SplitLighting,
        RegularLighting
    }
}
