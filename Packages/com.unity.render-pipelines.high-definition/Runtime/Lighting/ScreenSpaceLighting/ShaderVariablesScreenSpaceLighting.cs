
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL(needAccessors = false, omitStructDeclaration = true)]
    public struct ShaderVariablesScreenSpaceLighting
    {
        // Buffer pyramid
        public Vector4  _ColorPyramidSize;              // (x,y) = Actual Pixel Size, (z,w) = 1 / Actual Pixel Size
        public Vector4  _DepthPyramidSize;              // (x,y) = Actual Pixel Size, (z,w) = 1 / Actual Pixel Size
        public Vector4  _CameraMotionVectorsSize;       // (x,y) = Actual Pixel Size, (z,w) = 1 / Actual Pixel Size
        public Vector4  _ColorPyramidScale;             // (x,y) = Screen Scale, z = lod count, w = unused
        public Vector4  _DepthPyramidScale;             // (x,y) = Screen Scale, z = lod count, w = unused
        public Vector4  _CameraMotionVectorsScale;      // (x,y) = Screen Scale, z = lod count, w = unused

        // Ambient occlusion
        public Vector4 _AmbientOcclusionParam; // xyz occlusion color, w directLightStrenght

        public Vector4 _IndirectLightingMultiplier; // .x indirect diffuse multiplier (use with indirect lighting volume controler)

        // Screen space refraction
        public float   _SSRefractionInvScreenWeightDistance; // Distance for screen space smoothstep with fallback
    }
}

