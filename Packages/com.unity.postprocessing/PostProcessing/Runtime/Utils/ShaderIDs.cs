namespace UnityEngine.Rendering.PostProcessing
{
    // Pre-hashed shader ids - naming conventions are a bit off in this file as we use the same
    // fields names as in the shaders for ease of use... Would be nice to clean this up at some
    // point.
    static class ShaderIDs
    {
        internal static readonly int MainTex                         = Shader.PropertyToID("_MainTex");

        internal static readonly int Jitter                          = Shader.PropertyToID("_Jitter");
        internal static readonly int Sharpness                       = Shader.PropertyToID("_Sharpness");
        internal static readonly int FinalBlendParameters            = Shader.PropertyToID("_FinalBlendParameters");
        internal static readonly int HistoryTex                      = Shader.PropertyToID("_HistoryTex");

        internal static readonly int SMAA_Flip                       = Shader.PropertyToID("_SMAA_Flip");
        internal static readonly int SMAA_Flop                       = Shader.PropertyToID("_SMAA_Flop");

        internal static readonly int AOParams                        = Shader.PropertyToID("_AOParams");
        internal static readonly int AOColor                         = Shader.PropertyToID("_AOColor");
        internal static readonly int OcclusionTexture1               = Shader.PropertyToID("_OcclusionTexture1");
        internal static readonly int OcclusionTexture2               = Shader.PropertyToID("_OcclusionTexture2");
        internal static readonly int SAOcclusionTexture              = Shader.PropertyToID("_SAOcclusionTexture");
        internal static readonly int MSVOcclusionTexture             = Shader.PropertyToID("_MSVOcclusionTexture");
        internal static readonly int DepthCopy                       = Shader.PropertyToID("DepthCopy");
        internal static readonly int LinearDepth                     = Shader.PropertyToID("LinearDepth");
        internal static readonly int LowDepth1                       = Shader.PropertyToID("LowDepth1");
        internal static readonly int LowDepth2                       = Shader.PropertyToID("LowDepth2");
        internal static readonly int LowDepth3                       = Shader.PropertyToID("LowDepth3");
        internal static readonly int LowDepth4                       = Shader.PropertyToID("LowDepth4");
        internal static readonly int TiledDepth1                     = Shader.PropertyToID("TiledDepth1");
        internal static readonly int TiledDepth2                     = Shader.PropertyToID("TiledDepth2");
        internal static readonly int TiledDepth3                     = Shader.PropertyToID("TiledDepth3");
        internal static readonly int TiledDepth4                     = Shader.PropertyToID("TiledDepth4");
        internal static readonly int Occlusion1                      = Shader.PropertyToID("Occlusion1");
        internal static readonly int Occlusion2                      = Shader.PropertyToID("Occlusion2");
        internal static readonly int Occlusion3                      = Shader.PropertyToID("Occlusion3");
        internal static readonly int Occlusion4                      = Shader.PropertyToID("Occlusion4");
        internal static readonly int Combined1                       = Shader.PropertyToID("Combined1");
        internal static readonly int Combined2                       = Shader.PropertyToID("Combined2");
        internal static readonly int Combined3                       = Shader.PropertyToID("Combined3");

        internal static readonly int SSRResolveTemp                  = Shader.PropertyToID("_SSRResolveTemp");
        internal static readonly int Noise                           = Shader.PropertyToID("_Noise");
        internal static readonly int Test                            = Shader.PropertyToID("_Test");
        internal static readonly int Resolve                         = Shader.PropertyToID("_Resolve");
        internal static readonly int History                         = Shader.PropertyToID("_History");
        internal static readonly int ViewMatrix                      = Shader.PropertyToID("_ViewMatrix");
        internal static readonly int InverseViewMatrix               = Shader.PropertyToID("_InverseViewMatrix");
        internal static readonly int InverseProjectionMatrix         = Shader.PropertyToID("_InverseProjectionMatrix");
        internal static readonly int ScreenSpaceProjectionMatrix     = Shader.PropertyToID("_ScreenSpaceProjectionMatrix");
        internal static readonly int Params2                         = Shader.PropertyToID("_Params2");

        internal static readonly int FogColor                        = Shader.PropertyToID("_FogColor");
        internal static readonly int FogParams                       = Shader.PropertyToID("_FogParams");

        internal static readonly int VelocityScale                   = Shader.PropertyToID("_VelocityScale");
        internal static readonly int MaxBlurRadius                   = Shader.PropertyToID("_MaxBlurRadius");
        internal static readonly int RcpMaxBlurRadius                = Shader.PropertyToID("_RcpMaxBlurRadius");
        internal static readonly int VelocityTex                     = Shader.PropertyToID("_VelocityTex");
        internal static readonly int Tile2RT                         = Shader.PropertyToID("_Tile2RT");
        internal static readonly int Tile4RT                         = Shader.PropertyToID("_Tile4RT");
        internal static readonly int Tile8RT                         = Shader.PropertyToID("_Tile8RT");
        internal static readonly int TileMaxOffs                     = Shader.PropertyToID("_TileMaxOffs");
        internal static readonly int TileMaxLoop                     = Shader.PropertyToID("_TileMaxLoop");
        internal static readonly int TileVRT                         = Shader.PropertyToID("_TileVRT");
        internal static readonly int NeighborMaxTex                  = Shader.PropertyToID("_NeighborMaxTex");
        internal static readonly int LoopCount                       = Shader.PropertyToID("_LoopCount");

        internal static readonly int DepthOfFieldTemp                = Shader.PropertyToID("_DepthOfFieldTemp");
        internal static readonly int DepthOfFieldTex                 = Shader.PropertyToID("_DepthOfFieldTex");
        internal static readonly int Distance                        = Shader.PropertyToID("_Distance");
        internal static readonly int LensCoeff                       = Shader.PropertyToID("_LensCoeff");
        internal static readonly int MaxCoC                          = Shader.PropertyToID("_MaxCoC");
        internal static readonly int RcpMaxCoC                       = Shader.PropertyToID("_RcpMaxCoC");
        internal static readonly int RcpAspect                       = Shader.PropertyToID("_RcpAspect");
        internal static readonly int CoCTex                          = Shader.PropertyToID("_CoCTex");
        internal static readonly int TaaParams                       = Shader.PropertyToID("_TaaParams");

        internal static readonly int AutoExposureTex                 = Shader.PropertyToID("_AutoExposureTex");
        internal static readonly int HistogramBuffer                 = Shader.PropertyToID("_HistogramBuffer");
        internal static readonly int Params                          = Shader.PropertyToID("_Params");
        internal static readonly int ScaleOffsetRes                  = Shader.PropertyToID("_ScaleOffsetRes");

        internal static readonly int BloomTex                        = Shader.PropertyToID("_BloomTex");
        internal static readonly int SampleScale                     = Shader.PropertyToID("_SampleScale");
        internal static readonly int Threshold                       = Shader.PropertyToID("_Threshold");
        internal static readonly int ColorIntensity                  = Shader.PropertyToID("_ColorIntensity");
        internal static readonly int Bloom_DirtTex                   = Shader.PropertyToID("_Bloom_DirtTex");
        internal static readonly int Bloom_Settings                  = Shader.PropertyToID("_Bloom_Settings");
        internal static readonly int Bloom_Color                     = Shader.PropertyToID("_Bloom_Color");
        internal static readonly int Bloom_DirtTileOffset            = Shader.PropertyToID("_Bloom_DirtTileOffset");

        internal static readonly int ChromaticAberration_Amount      = Shader.PropertyToID("_ChromaticAberration_Amount");
        internal static readonly int ChromaticAberration_SpectralLut = Shader.PropertyToID("_ChromaticAberration_SpectralLut");

        internal static readonly int Distortion_CenterScale          = Shader.PropertyToID("_Distortion_CenterScale");
        internal static readonly int Distortion_Amount               = Shader.PropertyToID("_Distortion_Amount");

        internal static readonly int Lut2D                           = Shader.PropertyToID("_Lut2D");
        internal static readonly int Lut3D                           = Shader.PropertyToID("_Lut3D");
        internal static readonly int Lut3D_Params                    = Shader.PropertyToID("_Lut3D_Params");
        internal static readonly int Lut2D_Params                    = Shader.PropertyToID("_Lut2D_Params");
        internal static readonly int UserLut2D_Params                = Shader.PropertyToID("_UserLut2D_Params");
        internal static readonly int PostExposure                    = Shader.PropertyToID("_PostExposure");
        internal static readonly int ColorBalance                    = Shader.PropertyToID("_ColorBalance");
        internal static readonly int ColorFilter                     = Shader.PropertyToID("_ColorFilter");
        internal static readonly int HueSatCon                       = Shader.PropertyToID("_HueSatCon");
        internal static readonly int Brightness                      = Shader.PropertyToID("_Brightness");
        internal static readonly int ChannelMixerRed                 = Shader.PropertyToID("_ChannelMixerRed");
        internal static readonly int ChannelMixerGreen               = Shader.PropertyToID("_ChannelMixerGreen");
        internal static readonly int ChannelMixerBlue                = Shader.PropertyToID("_ChannelMixerBlue");
        internal static readonly int Lift                            = Shader.PropertyToID("_Lift");
        internal static readonly int InvGamma                        = Shader.PropertyToID("_InvGamma");
        internal static readonly int Gain                            = Shader.PropertyToID("_Gain");
        internal static readonly int Curves                          = Shader.PropertyToID("_Curves");
        internal static readonly int CustomToneCurve                 = Shader.PropertyToID("_CustomToneCurve");
        internal static readonly int ToeSegmentA                     = Shader.PropertyToID("_ToeSegmentA");
        internal static readonly int ToeSegmentB                     = Shader.PropertyToID("_ToeSegmentB");
        internal static readonly int MidSegmentA                     = Shader.PropertyToID("_MidSegmentA");
        internal static readonly int MidSegmentB                     = Shader.PropertyToID("_MidSegmentB");
        internal static readonly int ShoSegmentA                     = Shader.PropertyToID("_ShoSegmentA");
        internal static readonly int ShoSegmentB                     = Shader.PropertyToID("_ShoSegmentB");

        internal static readonly int Vignette_Color                  = Shader.PropertyToID("_Vignette_Color");
        internal static readonly int Vignette_Center                 = Shader.PropertyToID("_Vignette_Center");
        internal static readonly int Vignette_Settings               = Shader.PropertyToID("_Vignette_Settings");
        internal static readonly int Vignette_Mask                   = Shader.PropertyToID("_Vignette_Mask");
        internal static readonly int Vignette_Opacity                = Shader.PropertyToID("_Vignette_Opacity");
        internal static readonly int Vignette_Mode                   = Shader.PropertyToID("_Vignette_Mode");

        internal static readonly int Grain_Params1                   = Shader.PropertyToID("_Grain_Params1");
        internal static readonly int Grain_Params2                   = Shader.PropertyToID("_Grain_Params2");
        internal static readonly int GrainTex                        = Shader.PropertyToID("_GrainTex");
        internal static readonly int Phase                           = Shader.PropertyToID("_Phase");
        internal static readonly int GrainNoiseParameters            = Shader.PropertyToID("_NoiseParameters");

        internal static readonly int LumaInAlpha                     = Shader.PropertyToID("_LumaInAlpha");

        internal static readonly int DitheringTex                    = Shader.PropertyToID("_DitheringTex");
        internal static readonly int Dithering_Coords                = Shader.PropertyToID("_Dithering_Coords");

        internal static readonly int From                            = Shader.PropertyToID("_From");
        internal static readonly int To                              = Shader.PropertyToID("_To");
        internal static readonly int Interp                          = Shader.PropertyToID("_Interp");
        internal static readonly int TargetColor                     = Shader.PropertyToID("_TargetColor");

        internal static readonly int HalfResFinalCopy                = Shader.PropertyToID("_HalfResFinalCopy");
        internal static readonly int WaveformSource                  = Shader.PropertyToID("_WaveformSource");
        internal static readonly int WaveformBuffer                  = Shader.PropertyToID("_WaveformBuffer");
        internal static readonly int VectorscopeBuffer               = Shader.PropertyToID("_VectorscopeBuffer");

        internal static readonly int RenderViewportScaleFactor       = Shader.PropertyToID("_RenderViewportScaleFactor");

        internal static readonly int UVTransform                     = Shader.PropertyToID("_UVTransform");
        internal static readonly int DepthSlice                      = Shader.PropertyToID("_DepthSlice");
        internal static readonly int UVScaleOffset                   = Shader.PropertyToID("_UVScaleOffset");
        internal static readonly int PosScaleOffset                  = Shader.PropertyToID("_PosScaleOffset");
    }
}
