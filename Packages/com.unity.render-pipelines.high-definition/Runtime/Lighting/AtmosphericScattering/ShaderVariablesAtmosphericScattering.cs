
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL(needAccessors = false, omitStructDeclaration = true)]
    public struct ShaderVariablesAtmosphericScattering
    {
        // Common
        public int     _AtmosphericScatteringType;
        public float   _MaxFogDistance;
        public float   _FogColorMode;
        public float   _SkyTextureMipCount;
        public Vector4 _FogColorDensity; // color in rgb, density in alpha
        public Vector4 _MipFogParameters;

        // Linear fog
        public Vector4 _LinearFogParameters;

        // Exp fog
        public Vector4 _ExpFogParameters;

        // Volumetrics
        public float  _VBufferLastSliceDist;       // The distance to the middle of the last slice
        public int    _EnableDistantFog;           // bool...
    }
}

