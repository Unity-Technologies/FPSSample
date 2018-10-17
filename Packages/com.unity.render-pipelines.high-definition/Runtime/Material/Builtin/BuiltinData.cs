//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class Builtin // Note: This particular class doesn't derive from RenderPipelineMaterial
    {
        //-----------------------------------------------------------------------------
        // BuiltinData
        // This structure include common data that should be present in all material
        // and are independent from the BSDF parametrization.
        // Note: These parameters can be store in GBuffer or not depends on storage available
        //-----------------------------------------------------------------------------
        [GenerateHLSL(PackingRules.Exact, false, true, 100)]
        public struct BuiltinData
        {
            [SurfaceDataAttributes("Opacity")]
            public float opacity;

            // These are lighting data.
            // We would prefer to split lighting and material information but for performance reasons,
            // those lighting information are fill
            // at the same time than material information.
            [SurfaceDataAttributes("Bake Diffuse Lighting", false, true)]
            public Vector3 bakeDiffuseLighting; // This is the result of sampling lightmap/lightprobe/proxyvolume
            [SurfaceDataAttributes("Back Bake Diffuse Lighting", false, true)]
            public Vector3 backBakeDiffuseLighting; // This is the result of sampling lightmap/lightprobe/proxyvolume from the back for transmission

            // Use for float instead of vector4 to ease the debug (no performance impact)
            // Note: We have no way to remove these value automatically based on either SHADEROPTIONS_BAKED_SHADOW_MASK_ENABLE or s_BakedShadowMaskEnable here. Unless we make two structure... For now always keep this value
            [SurfaceDataAttributes("Shadow Mask 0")]
            public float shadowMask0;
            [SurfaceDataAttributes("Shadow Mask 1")]
            public float shadowMask1;
            [SurfaceDataAttributes("Shadow Mask 2")]
            public float shadowMask2;
            [SurfaceDataAttributes("Shadow Mask 3")]
            public float shadowMask3;

            [SurfaceDataAttributes("Emissive Color", false, false)]
            public Vector3 emissiveColor;

            // These is required for motion blur and temporalAA
            [SurfaceDataAttributes("Velocity")]
            public Vector2 velocity;

            // Distortion
            [SurfaceDataAttributes("Distortion")]
            public Vector2 distortion;
            [SurfaceDataAttributes("Distortion Blur")]
            public float distortionBlur;           // Define the color buffer mipmap level to use

            // Misc
            [SurfaceDataAttributes("RenderingLayers")]
            public uint renderingLayers;

            [SurfaceDataAttributes("Depth Offset")]
            public float depthOffset; // define the depth in unity unit to add in Z forward direction
        };

        //-----------------------------------------------------------------------------
        // LightTransportData
        // This struct is use to store information for Enlighten/Progressive light mapper. both at runtime or off line.
        //-----------------------------------------------------------------------------
        [GenerateHLSL(PackingRules.Exact, false)]
        public struct LightTransportData
        {
            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 emissiveColor; // HDR value
        };

        public static RenderTextureFormat GetLightingBufferFormat()
        {
            return RenderTextureFormat.RGB111110Float;
        }

        public static bool GetLightingBufferSRGBFlag()
        {
            return false;
        }

        public static RenderTextureFormat GetShadowMaskBufferFormat()
        {
            return RenderTextureFormat.ARGB32;
        }

        public static bool GetShadowMaskBufferSRGBFlag()
        {
            return false;
        }

        public static RenderTextureFormat GetVelocityBufferFormat()
        {
            return RenderTextureFormat.RGHalf; // TODO: We should use 16bit normalized instead, better precision // RGInt
        }

        public static bool GetVelocityBufferSRGBFlag()
        {
            return false;
        }

        public static RenderTextureFormat GetDistortionBufferFormat()
        {
            // TODO: // This format need to be additive blendable and include distortionBlur, blend mode different for alpha value
            return RenderTextureFormat.ARGBHalf;
        }

        public static bool GetDistortionBufferSRGBFlag()
        {
            return false;
        }
    }
}
