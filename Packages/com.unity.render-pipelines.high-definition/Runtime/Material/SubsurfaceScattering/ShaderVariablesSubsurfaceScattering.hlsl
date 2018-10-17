#ifdef SHADER_VARIABLES_INCLUDE_CB
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/DiffusionProfile/DiffusionProfileSettings.cs.hlsl"

    // Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
    // Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
    uint   _EnableSubsurfaceScattering; // Globally toggles subsurface and transmission scattering on/off
    float  _TexturingModeFlags;         // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
    float  _TransmissionFlags;          // 1 bit/profile; 0 = regular, 1 = thin
    // Use float4 to avoid any packing issue between compute and pixel shaders
    float4  _ThicknessRemaps[DIFFUSION_PROFILE_COUNT];   // R: start, G = end - start, BA unused
    float4 _ShapeParams[DIFFUSION_PROFILE_COUNT];        // RGB = S = 1 / D, A = filter radius
    float4 _TransmissionTintsAndFresnel0[DIFFUSION_PROFILE_COUNT];  // RGB = 1/4 * color, A = fresnel0
    float4 _WorldScales[DIFFUSION_PROFILE_COUNT];        // X = meters per world unit; Y = world units per meter
#else
#endif
