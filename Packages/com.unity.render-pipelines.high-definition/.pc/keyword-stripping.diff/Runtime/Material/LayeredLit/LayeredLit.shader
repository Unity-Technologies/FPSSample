Shader "HDRenderPipeline/LayeredLit"
{
    Properties
    {
        // Versioning of material to help for upgrading
        [HideInInspector] _HdrpVersion("_HdrpVersion", Float) = 2

        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

        // All the following properties are filled by the referenced lit shader.

        // Reminder. Color here are in linear but the UI (color picker) do the conversion sRGB to linear
        _BaseColor0("BaseColor0", Color) = (1, 1, 1, 1)
        _BaseColor1("BaseColor1", Color) = (1, 1, 1, 1)
        _BaseColor2("BaseColor2", Color) = (1, 1, 1, 1)
        _BaseColor3("BaseColor3", Color) = (1, 1, 1, 1)

        _BaseColorMap0("BaseColorMap0", 2D) = "white" {}
        _BaseColorMap1("BaseColorMap1", 2D) = "white" {}
        _BaseColorMap2("BaseColorMap2", 2D) = "white" {}
        _BaseColorMap3("BaseColorMap3", 2D) = "white" {}

        _Metallic0("Metallic0", Range(0.0, 1.0)) = 0
        _Metallic1("Metallic1", Range(0.0, 1.0)) = 0
        _Metallic2("Metallic2", Range(0.0, 1.0)) = 0
        _Metallic3("Metallic3", Range(0.0, 1.0)) = 0

        _Smoothness0("Smoothness0", Range(0.0, 1.0)) = 0.5
        _Smoothness1("Smoothness1", Range(0.0, 1.0)) = 0.5
        _Smoothness2("Smoothness2", Range(0.0, 1.0)) = 0.5
        _Smoothness3("Smoothness3", Range(0.0, 1.0)) = 0.5

        _SmoothnessRemapMin0("SmoothnessRemapMin0", Range(0.0, 1.0)) = 0.0
        _SmoothnessRemapMin1("SmoothnessRemapMin1", Range(0.0, 1.0)) = 0.0
        _SmoothnessRemapMin2("SmoothnessRemapMin2", Range(0.0, 1.0)) = 0.0
        _SmoothnessRemapMin3("SmoothnessRemapMin3", Range(0.0, 1.0)) = 0.0

        _SmoothnessRemapMax0("SmoothnessRemapMax0", Range(0.0, 1.0)) = 1.0
        _SmoothnessRemapMax1("SmoothnessRemapMax1", Range(0.0, 1.0)) = 1.0
        _SmoothnessRemapMax2("SmoothnessRemapMax2", Range(0.0, 1.0)) = 1.0
        _SmoothnessRemapMax3("SmoothnessRemapMax3", Range(0.0, 1.0)) = 1.0

        _AORemapMin0("AORemapMin0", Range(0.0, 1.0)) = 0.0
        _AORemapMin1("AORemapMin1", Range(0.0, 1.0)) = 0.0
        _AORemapMin2("AORemapMin2", Range(0.0, 1.0)) = 0.0
        _AORemapMin3("AORemapMin3", Range(0.0, 1.0)) = 0.0

        _AORemapMax0("AORemapMax0", Range(0.0, 1.0)) = 1.0
        _AORemapMax1("AORemapMax1", Range(0.0, 1.0)) = 1.0
        _AORemapMax2("AORemapMax2", Range(0.0, 1.0)) = 1.0
        _AORemapMax3("AORemapMax3", Range(0.0, 1.0)) = 1.0

        _MaskMap0("MaskMap0", 2D) = "white" {}
        _MaskMap1("MaskMap1", 2D) = "white" {}
        _MaskMap2("MaskMap2", 2D) = "white" {}
        _MaskMap3("MaskMap3", 2D) = "white" {}

        _NormalMap0("NormalMap0", 2D) = "bump" {}
        _NormalMap1("NormalMap1", 2D) = "bump" {}
        _NormalMap2("NormalMap2", 2D) = "bump" {}
        _NormalMap3("NormalMap3", 2D) = "bump" {}

        _NormalMapOS0("NormalMapOS0", 2D) = "white" {}
        _NormalMapOS1("NormalMapOS1", 2D) = "white" {}
        _NormalMapOS2("NormalMapOS2", 2D) = "white" {}
        _NormalMapOS3("NormalMapOS3", 2D) = "white" {}

        _NormalScale0("_NormalScale0", Range(0.0, 2.0)) = 1
        _NormalScale1("_NormalScale1", Range(0.0, 2.0)) = 1
        _NormalScale2("_NormalScale2", Range(0.0, 2.0)) = 1
        _NormalScale3("_NormalScale3", Range(0.0, 2.0)) = 1

        _BentNormalMap0("BentNormalMap0", 2D) = "bump" {}
        _BentNormalMap1("BentNormalMap1", 2D) = "bump" {}
        _BentNormalMap2("BentNormalMap2", 2D) = "bump" {}
        _BentNormalMap3("BentNormalMap3", 2D) = "bump" {}

        _BentNormalMapOS0("BentNormalMapOS0", 2D) = "white" {}
        _BentNormalMapOS1("BentNormalMapOS1", 2D) = "white" {}
        _BentNormalMapOS2("BentNormalMapOS2", 2D) = "white" {}
        _BentNormalMapOS3("BentNormalMapOS3", 2D) = "white" {}

        _HeightMap0("HeightMap0", 2D) = "black" {}
        _HeightMap1("HeightMap1", 2D) = "black" {}
        _HeightMap2("HeightMap2", 2D) = "black" {}
        _HeightMap3("HeightMap3", 2D) = "black" {}

        // Caution: Default value of _HeightAmplitude must be (_HeightMax - _HeightMin) * 0.01
        // Those two properties are computed from the ones exposed in the UI and depends on the displaement mode so they are separate because we don't want to lose information upon displacement mode change.
        [HideInInspector] _HeightAmplitude0("Height Scale0", Float) = 0.02
        [HideInInspector] _HeightAmplitude1("Height Scale1", Float) = 0.02
        [HideInInspector] _HeightAmplitude2("Height Scale2", Float) = 0.02
        [HideInInspector] _HeightAmplitude3("Height Scale3", Float) = 0.02
        [HideInInspector] _HeightCenter0("Height Bias0", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter1("Height Bias1", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter2("Height Bias2", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _HeightCenter3("Height Bias3", Range(0.0, 1.0)) = 0.5

        [Enum(MinMax, 0, Amplitude, 1)] _HeightMapParametrization0("Heightmap Parametrization0", Int) = 0
        [Enum(MinMax, 0, Amplitude, 1)] _HeightMapParametrization1("Heightmap Parametrization1", Int) = 0
        [Enum(MinMax, 0, Amplitude, 1)] _HeightMapParametrization2("Heightmap Parametrization2", Int) = 0
        [Enum(MinMax, 0, Amplitude, 1)] _HeightMapParametrization3("Heightmap Parametrization3", Int) = 0
        // These parameters are for vertex displacement/Tessellation
        _HeightOffset0("Height Offset0", Float) = 0
        _HeightOffset1("Height Offset1", Float) = 0
        _HeightOffset2("Height Offset2", Float) = 0
        _HeightOffset3("Height Offset3", Float) = 0
        // MinMax mode
        _HeightMin0("Height Min0", Float) = -1
        _HeightMin1("Height Min1", Float) = -1
        _HeightMin2("Height Min2", Float) = -1
        _HeightMin3("Height Min3", Float) = -1
        _HeightMax0("Height Max0", Float) = 1
        _HeightMax1("Height Max1", Float) = 1
        _HeightMax2("Height Max2", Float) = 1
        _HeightMax3("Height Max3", Float) = 1

        // Amplitude mode
        _HeightTessAmplitude0("Amplitude0", Float) = 2.0 // in Centimeters
        _HeightTessAmplitude1("Amplitude1", Float) = 2.0 // in Centimeters
        _HeightTessAmplitude2("Amplitude2", Float) = 2.0 // in Centimeters
        _HeightTessAmplitude3("Amplitude3", Float) = 2.0 // in Centimeters
        _HeightTessCenter0("Height Bias0", Range(0.0, 1.0)) = 0.5
        _HeightTessCenter1("Height Bias1", Range(0.0, 1.0)) = 0.5
        _HeightTessCenter2("Height Bias2", Range(0.0, 1.0)) = 0.5
        _HeightTessCenter3("Height Bias3", Range(0.0, 1.0)) = 0.5

        // These parameters are for pixel displacement
        _HeightPoMAmplitude0("Height Amplitude0", Float) = 2.0 // In centimeters
        _HeightPoMAmplitude1("Height Amplitude1", Float) = 2.0 // In centimeters
        _HeightPoMAmplitude2("Height Amplitude2", Float) = 2.0 // In centimeters
        _HeightPoMAmplitude3("Height Amplitude3", Float) = 2.0 // In centimeters

        _DetailMap0("DetailMap0", 2D) = "black" {}
        _DetailMap1("DetailMap1", 2D) = "black" {}
        _DetailMap2("DetailMap2", 2D) = "black" {}
        _DetailMap3("DetailMap3", 2D) = "black" {}

        _DetailAlbedoScale0("_DetailAlbedoScale0", Range(0.0, 2.0)) = 1
        _DetailAlbedoScale1("_DetailAlbedoScale1", Range(0.0, 2.0)) = 1
        _DetailAlbedoScale2("_DetailAlbedoScale2", Range(0.0, 2.0)) = 1
        _DetailAlbedoScale3("_DetailAlbedoScale3", Range(0.0, 2.0)) = 1

        _DetailNormalScale0("_DetailNormalScale0", Range(0.0, 2.0)) = 1
        _DetailNormalScale1("_DetailNormalScale1", Range(0.0, 2.0)) = 1
        _DetailNormalScale2("_DetailNormalScale2", Range(0.0, 2.0)) = 1
        _DetailNormalScale3("_DetailNormalScale3", Range(0.0, 2.0)) = 1

        _DetailSmoothnessScale0("_DetailSmoothnessScale0", Range(0.0, 2.0)) = 1
        _DetailSmoothnessScale1("_DetailSmoothnessScale1", Range(0.0, 2.0)) = 1
        _DetailSmoothnessScale2("_DetailSmoothnessScale2", Range(0.0, 2.0)) = 1
        _DetailSmoothnessScale3("_DetailSmoothnessScale3", Range(0.0, 2.0)) = 1

        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace0("NormalMap space", Float) = 0
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace1("NormalMap space", Float) = 0
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace2("NormalMap space", Float) = 0
        [Enum(TangentSpace, 0, ObjectSpace, 1)] _NormalMapSpace3("NormalMap space", Float) = 0

        _DiffusionProfile0("Diffusion Profile0", Int) = 0
        _DiffusionProfile1("Diffusion Profile1", Int) = 0
        _DiffusionProfile2("Diffusion Profile2", Int) = 0
        _DiffusionProfile3("Diffusion Profile3", Int) = 0

        _SubsurfaceMask0("Subsurface Mask0", Range(0.0, 1.0)) = 1.0
        _SubsurfaceMask1("Subsurface Mask1", Range(0.0, 1.0)) = 1.0
        _SubsurfaceMask2("Subsurface Mask2", Range(0.0, 1.0)) = 1.0
        _SubsurfaceMask3("Subsurface Mask3", Range(0.0, 1.0)) = 1.0

        _SubsurfaceMaskMap0("Subsurface Mask Map0", 2D) = "white" {}
        _SubsurfaceMaskMap1("Subsurface Mask Map1", 2D) = "white" {}
        _SubsurfaceMaskMap2("Subsurface Mask Map2", 2D) = "white" {}
        _SubsurfaceMaskMap3("Subsurface Mask Map3", 2D) = "white" {}

        _Thickness0("Thickness", Range(0.0, 1.0)) = 1.0
        _Thickness1("Thickness", Range(0.0, 1.0)) = 1.0
        _Thickness2("Thickness", Range(0.0, 1.0)) = 1.0
        _Thickness3("Thickness", Range(0.0, 1.0)) = 1.0

        _ThicknessMap0("Thickness Map", 2D) = "white" {}
        _ThicknessMap1("Thickness Map", 2D) = "white" {}
        _ThicknessMap2("Thickness Map", 2D) = "white" {}
        _ThicknessMap3("Thickness Map", 2D) = "white" {}

        _ThicknessRemap0("Thickness Remap", Vector) = (0, 1, 0, 0)
        _ThicknessRemap1("Thickness Remap", Vector) = (0, 1, 0, 0)
        _ThicknessRemap2("Thickness Remap", Vector) = (0, 1, 0, 0)
        _ThicknessRemap3("Thickness Remap", Vector) = (0, 1, 0, 0)

        // All the following properties exist only in layered lit material

        // Layer blending options
        _LayerMaskMap("LayerMaskMap", 2D) = "white" {}
        _LayerInfluenceMaskMap("LayerInfluenceMaskMap", 2D) = "white" {}
        [ToggleUI] _UseHeightBasedBlend("UseHeightBasedBlend", Float) = 0.0

        _HeightTransition("Height Transition", Range(0, 1.0)) = 0.0

        [ToggleUI] _UseDensityMode("Use Density mode", Float) = 0.0
        [ToggleUI] _UseMainLayerInfluence("UseMainLayerInfluence", Float) = 0.0

        _InheritBaseNormal1("_InheritBaseNormal1", Range(0, 1.0)) = 0.0
        _InheritBaseNormal2("_InheritBaseNormal2", Range(0, 1.0)) = 0.0
        _InheritBaseNormal3("_InheritBaseNormal3", Range(0, 1.0)) = 0.0

        _InheritBaseHeight1("_InheritBaseHeight1", Range(0, 1.0)) = 0.0
        _InheritBaseHeight2("_InheritBaseHeight2", Range(0, 1.0)) = 0.0
        _InheritBaseHeight3("_InheritBaseHeight3", Range(0, 1.0)) = 0.0

        _InheritBaseColor1("_InheritBaseColor1", Range(0, 1.0)) = 0.0
        _InheritBaseColor2("_InheritBaseColor2", Range(0, 1.0)) = 0.0
        _InheritBaseColor3("_InheritBaseColor3", Range(0, 1.0)) = 0.0

        [ToggleUI] _OpacityAsDensity0("_OpacityAsDensity0", Float) = 0.0
        [ToggleUI] _OpacityAsDensity1("_OpacityAsDensity1", Float) = 0.0
        [ToggleUI] _OpacityAsDensity2("_OpacityAsDensity2", Float) = 0.0
        [ToggleUI] _OpacityAsDensity3("_OpacityAsDensity3", Float) = 0.0

        [HideInInspector] _LayerCount("_LayerCount", Float) = 2.0

        [Enum(None, 0, Multiply, 1, Add, 2)] _VertexColorMode("Vertex color mode", Float) = 0

        [ToggleUI]  _ObjectScaleAffectTile("_ObjectScaleAffectTile", Float) = 0.0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBlendMask("UV Set for blendMask", Float) = 0
        [HideInInspector] _UVMappingMaskBlendMask("_UVMappingMaskBlendMask", Color) = (1, 0, 0, 0)
        _TexWorldScaleBlendMask("Tiling", Float) = 1.0

        // Following are builtin properties

        [ToggleUI]  _EnableSpecularOcclusion("Enable specular occlusion", Float) = 0.0

        [HDR] _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        [ToggleUI] _AlbedoAffectEmissive("Albedo Affect Emissive", Float) = 0.0

        [ToggleUI] _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0

        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _TransparentSortPriority("_TransparentSortPriority", Float) = 0

        // Stencil state
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2 // StencilLightingUsage.RegularLighting
        [HideInInspector] _StencilWriteMask("_StencilWriteMask", Int) = 7 // StencilMask.Lighting  (fixed at compile time)
        [HideInInspector] _StencilRefMV("_StencilRefMV", Int) = 128 // StencilLightingUsage.RegularLighting  (fixed at compile time)
        [HideInInspector] _StencilWriteMaskMV("_StencilWriteMaskMV", Int) = 128 // StencilMask.ObjectsVelocity  (fixed at compile time)

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode ("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _ZTestDepthEqualForOpaque("_ZTestDepthEqualForOpaque", Int) = 4 // Less equal
        [HideInInspector] _ZTestGBuffer("_ZTestGBuffer", Int) = 4

        [ToggleUI] _EnableFogOnTransparent("Enable Fog", Float) = 1.0
        [ToggleUI] _EnableBlendModePreserveSpecularLighting("Enable Blend Mode Preserve Specular Lighting", Float) = 1.0

        [ToggleUI] _DoubleSidedEnable("Double sided enable", Float) = 0.0
        [Enum(Flip, 0, Mirror, 1, None, 2)] _DoubleSidedNormalMode("Double sided normal mode", Float) = 1
        [HideInInspector] _DoubleSidedConstants("_DoubleSidedConstants", Vector) = (1, 1, -1, 0)

        // For layering, due to combinatorial explosion, we only support SSS/Transmission and Standard. We let other case for the shader graph
        [Enum(Subsurface Scattering, 0, Standard, 1, Translucent, 5)] _MaterialID("MaterialId", Int) = 1 // MaterialId.Standard
        [ToggleUI] _TransmissionEnable("_TransmissionEnable", Float) = 1.0

        [Enum(None, 0, Vertex displacement, 1, Pixel displacement, 2)] _DisplacementMode("DisplacementMode", Int) = 0
        [ToggleUI] _DisplacementLockObjectScale("displacement lock object scale", Float) = 1.0
        [ToggleUI] _DisplacementLockTilingScale("displacement lock tiling scale", Float) = 1.0
        [ToggleUI] _DepthOffsetEnable("Depth Offset View space", Float) = 0.0

        [ToggleUI] _EnableGeometricSpecularAA("EnableGeometricSpecularAA", Float) = 0.0
        _SpecularAAScreenSpaceVariance("SpecularAAScreenSpaceVariance", Range(0.0, 1.0)) = 0.1
        _SpecularAAThreshold("SpecularAAThreshold", Range(0.0, 1.0)) = 0.2

        [ToggleUI] _EnableMotionVectorForVertexAnimation("EnableMotionVectorForVertexAnimation", Float) = 0.0

        _PPDMinSamples("Min sample for POM", Range(1.0, 64.0)) = 5
        _PPDMaxSamples("Max sample for POM", Range(1.0, 64.0)) = 15
        _PPDLodThreshold("Start lod to fade out the POM effect", Range(0.0, 16.0)) = 5
        _PPDPrimitiveLength("Primitive length for POM", Float) = 1
        _PPDPrimitiveWidth("Primitive width for POM", Float) = 1
        [HideInInspector] _InvPrimScale("Inverse primitive scale for non-planar POM", Vector) = (1, 1, 0, 0)

        [Enum(Use Emissive Color, 0, Use Emissive Mask, 1)] _EmissiveColorMode("Emissive color mode", Float) = 1
        [Enum(UV0, 0, Planar, 4, TriPlanar, 5)] _UVEmissive("UV Set for emissive", Float) = 0
        _TexWorldScaleEmissive("Scale to apply on world coordinate", Float) = 1.0
        [HideInInspector] _UVMappingMaskEmissive("_UVMappingMaskEmissive", Color) = (1, 0, 0, 0)

        // Wind
        [ToggleUI]  _EnableWind("Enable Wind", Float) = 0.0
        _InitialBend("Initial Bend", float) = 1.0
        _Stiffness("Stiffness", float) = 1.0
        _Drag("Drag", float) = 1.0
        _ShiverDrag("Shiver Drag", float) = 0.2
        _ShiverDirectionality("Shiver Directionality", Range(0.0, 1.0)) = 0.5

        // Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
        // value that exist to identify if the GI emission need to be enabled.
        // In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
        // TODO: Fix the code in legacy unity so we can customize the beahvior for GI
        _EmissionColor("Color", Color) = (1, 1, 1)

        _TexWorldScale0("Tiling", Float) = 1.0
        _TexWorldScale1("Tiling", Float) = 1.0
        _TexWorldScale2("Tiling", Float) = 1.0
        _TexWorldScale3("Tiling", Float) = 1.0

        [HideInInspector] _InvTilingScale0("Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y))", Float) = 1
        [HideInInspector] _InvTilingScale1("Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y))", Float) = 1
        [HideInInspector] _InvTilingScale2("Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y))", Float) = 1
        [HideInInspector] _InvTilingScale3("Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y))", Float) = 1

        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase0("UV Set for base0", Float) = 0 // no UV1/2/3 for main layer (matching Lit.shader and for PPDisplacement restriction)
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase1("UV Set for base1", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase2("UV Set for base2", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3, Planar, 4, Triplanar, 5)] _UVBase3("UV Set for base3", Float) = 0

        [HideInInspector] _UVMappingMask0("_UVMappingMask0", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVMappingMask1("_UVMappingMask1", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVMappingMask2("_UVMappingMask2", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVMappingMask3("_UVMappingMask3", Color) = (1, 0, 0, 0)

        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail0("UV Set for detail0", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail1("UV Set for detail1", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail2("UV Set for detail2", Float) = 0
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVDetail3("UV Set for detail3", Float) = 0

        [HideInInspector] _UVDetailsMappingMask0("_UVDetailsMappingMask0", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVDetailsMappingMask1("_UVDetailsMappingMask1", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVDetailsMappingMask2("_UVDetailsMappingMask2", Color) = (1, 0, 0, 0)
        [HideInInspector] _UVDetailsMappingMask3("_UVDetailsMappingMask3", Color) = (1, 0, 0, 0)

        [ToggleUI] _LinkDetailsWithBase0("LinkDetailsWithBase0", Float) = 1.0
        [ToggleUI] _LinkDetailsWithBase1("LinkDetailsWithBase1", Float) = 1.0
        [ToggleUI] _LinkDetailsWithBase2("LinkDetailsWithBase2", Float) = 1.0
        [ToggleUI] _LinkDetailsWithBase3("LinkDetailsWithBase3", Float) = 1.0

        // Transparency
        [ToggleUI] _PreRefractionPass("PreRefractionPass", Float) = 0.0

        // HACK: GI Baking system relies on some properties existing in the shader ("_MainTex", "_Cutoff" and "_Color") for opacity handling, so we need to store our version of those parameters in the hard-coded name the GI baking system recognizes.
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [ToggleUI] _SupportDecals("Support Decals", Float) = 1.0
        [ToggleUI] _ReceivesSSR("Receives SSR", Float) = 1.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _DEPTHOFFSET_ON
    #pragma shader_feature _DOUBLESIDED_ON
    #pragma shader_feature _ _VERTEX_DISPLACEMENT _PIXEL_DISPLACEMENT
    #pragma shader_feature _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
    #pragma shader_feature _DISPLACEMENT_LOCK_TILING_SCALE
    #pragma shader_feature _PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE
    #pragma shader_feature _VERTEX_WIND

    #pragma shader_feature _ _EMISSIVE_MAPPING_PLANAR _EMISSIVE_MAPPING_TRIPLANAR
    #pragma shader_feature _LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR_BLENDMASK _LAYER_MAPPING_TRIPLANAR_BLENDMASK
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR0 _LAYER_MAPPING_TRIPLANAR0
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR1 _LAYER_MAPPING_TRIPLANAR1
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR2 _LAYER_MAPPING_TRIPLANAR2
    #pragma shader_feature _ _LAYER_MAPPING_PLANAR3 _LAYER_MAPPING_TRIPLANAR3
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE0
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE1
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE2
    #pragma shader_feature _NORMALMAP_TANGENT_SPACE3
    #pragma shader_feature _ _REQUIRE_UV2 _REQUIRE_UV3

    #pragma shader_feature _NORMALMAP0
    #pragma shader_feature _NORMALMAP1
    #pragma shader_feature _NORMALMAP2
    #pragma shader_feature _NORMALMAP3
    #pragma shader_feature _MASKMAP0
    #pragma shader_feature _MASKMAP1
    #pragma shader_feature _MASKMAP2
    #pragma shader_feature _MASKMAP3
    #pragma shader_feature _BENTNORMALMAP0
    #pragma shader_feature _BENTNORMALMAP1
    #pragma shader_feature _BENTNORMALMAP2
    #pragma shader_feature _BENTNORMALMAP3
    #pragma shader_feature _EMISSIVE_COLOR_MAP
    #pragma shader_feature _ENABLESPECULAROCCLUSION
    #pragma shader_feature _DETAIL_MAP0
    #pragma shader_feature _DETAIL_MAP1
    #pragma shader_feature _DETAIL_MAP2
    #pragma shader_feature _DETAIL_MAP3
    #pragma shader_feature _HEIGHTMAP0
    #pragma shader_feature _HEIGHTMAP1
    #pragma shader_feature _HEIGHTMAP2
    #pragma shader_feature _HEIGHTMAP3
    #pragma shader_feature _SUBSURFACE_MASK_MAP0
    #pragma shader_feature _SUBSURFACE_MASK_MAP1
    #pragma shader_feature _SUBSURFACE_MASK_MAP2
    #pragma shader_feature _SUBSURFACE_MASK_MAP3
    #pragma shader_feature _THICKNESSMAP0
    #pragma shader_feature _THICKNESSMAP1
    #pragma shader_feature _THICKNESSMAP2
    #pragma shader_feature _THICKNESSMAP3

    #pragma shader_feature _ _LAYER_MASK_VERTEX_COLOR_MUL _LAYER_MASK_VERTEX_COLOR_ADD
    #pragma shader_feature _MAIN_LAYER_INFLUENCE_MODE
    #pragma shader_feature _INFLUENCEMASK_MAP
    #pragma shader_feature _DENSITY_MODE
    #pragma shader_feature _HEIGHT_BASED_BLEND
    #pragma shader_feature _ _LAYEREDLIT_3_LAYERS _LAYEREDLIT_4_LAYERS

    #pragma shader_feature _DISABLE_DECALS
    #pragma shader_feature _DISABLE_SSR
    #pragma shader_feature _ENABLE_GEOMETRIC_SPECULAR_AA

    // Keyword for transparent
    #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
    #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
    #pragma shader_feature _BLENDMODE_PRESERVE_SPECULAR_LIGHTING
    #pragma shader_feature _ENABLE_FOG_ON_TRANSPARENT

    // MaterialFeature are used as shader feature to allow compiler to optimize properly
    #pragma shader_feature _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    #pragma shader_feature _MATERIAL_FEATURE_TRANSMISSION

    // enable dithering LOD crossfade
    #pragma multi_compile _ LOD_FADE_CROSSFADE

    //enable GPU instancing support
    #pragma multi_compile_instancing
    #pragma instancing_options renderinglayer

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    // Use surface gradient normal mapping as it handle correctly triplanar normal mapping and multiple UVSet
    #define SURFACE_GRADIENT
    // This shader support vertex modification
    #define HAVE_VERTEX_MODIFICATION

    // If we use subsurface scattering, enable output split lighting (for forward pass)
    #if defined(_MATERIAL_FEATURE_SUBSURFACE_SCATTERING) && !defined(_SURFACE_TYPE_TRANSPARENT)
    #define OUTPUT_SPLIT_LIGHTING
    #endif

    #define _MAX_LAYER 4

    #if defined(_LAYEREDLIT_4_LAYERS)
    #   define _LAYER_COUNT 4
    #elif defined(_LAYEREDLIT_3_LAYERS)
    #   define _LAYER_COUNT 3
    #else
    #   define _LAYER_COUNT 2
    #endif

    // Explicitely said that we are a layered shader as we share code between lit and layered lit
    #define LAYERED_LIT_SHADER

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Wind.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
    // #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    // Can't include 'ShaderVariables.hlsl' here because of USE_LEGACY_UNITY_MATRIX_VARIABLES. :-(
    // #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    // Same story for 'Material.hlsl' (above) which includes 'AtmosphericScattering.hlsl' which includes 'ShaderVariables.hlsl'.
    // #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitProperties.hlsl"

    // TODO:
    // Currently, Lit.hlsl and LitData.hlsl are included for every pass. Split Lit.hlsl in two:
    // LitData.hlsl and LitShading.hlsl (merge into the existing LitData.hlsl).
    // LitData.hlsl should be responsible for preparing shading parameters.
    // LitShading.hlsl implements the light loop API.
    // LitData.hlsl is included here, LitShading.hlsl is included below for shading passes only.

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDLitShader" }

        Pass
        {
            Name "SceneSelectionPass"
            Tags{ "LightMode" = "SceneSelectionPass" }

            Cull[_CullMode]

            ZWrite On

            ColorMask 0

            HLSLPROGRAM

            // Note: Require _ObjectId and _PassValue variables

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENESELECTIONPASS // This will drive the output of the scene selection shader
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LayeredLit/LayeredLitData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        // Caution: The outline selection in the editor use the vertex shader/hull/domain shader of the first pass declare. So it should not bethe  meta pass.
        Pass
        {
            Name "GBuffer"  // Name is not used
            Tags { "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

            Cull [_CullMode]
            ZTest[_ZTestGBuffer]

            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // Setup DECALS_OFF so the shader stripper can remove variants
            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile _ LIGHT_LAYERS

        #ifdef _ALPHATEST_ON
            // When we have alpha test, we will force a depth prepass so we always bypass the clip instruction in the GBuffer
            #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
        #endif

            #define SHADERPASS SHADERPASS_GBUFFER
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #ifdef DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
            #endif
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LayeredLit/LayeredLitData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassGBuffer.hlsl"

            ENDHLSL
        }

        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags{ "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM

            // Lightmap memo
            // DYNAMICLIGHTMAP_ON is used when we have an "enlighten lightmap" ie a lightmap updated at runtime by enlighten.This lightmap contain indirect lighting from realtime lights and realtime emissive material.Offline baked lighting(from baked material / light,
            // both direct and indirect lighting) will hand up in the "regular" lightmap->LIGHTMAP_ON.

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LayeredLit/LayeredLitData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Motion Vectors"
            Tags{ "LightMode" = "MotionVectors" } // Caution, this need to be call like this to setup the correct parameters by C++ (legacy Unity)

            // If velocity pass (motion vectors) is enabled we tag the stencil so it don't perform CameraMotionVelocity
            Stencil
            {
                WriteMask [_StencilWriteMaskMV]
                Ref [_StencilRefMV]
                Comp Always
                Pass Replace
            }

            Cull[_CullMode]

            ZWrite On

            HLSLPROGRAM
            #pragma multi_compile _ WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_MSAA_DEPTH
            #define SHADERPASS SHADERPASS_VELOCITY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #ifdef WRITE_NORMAL_BUFFER // If enabled we need all regular interpolator
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
            #else
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitVelocityPass.hlsl"
            #endif
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LayeredLit/LayeredLitData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassVelocity.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }

            Cull[_CullMode]

            ZClip [_ZClip]
            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_SHADOWS
            #define USE_LEGACY_UNITY_MATRIX_VARIABLES
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LayeredLit/LayeredLitData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{ "LightMode" = "DepthOnly" }

            Cull[_CullMode]

            // To be able to tag stencil with disableSSR information for forward
            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            ZWrite On

            HLSLPROGRAM

            // In deferred, depth only pass don't output anything.
            // In forward it output the normal buffer
            #pragma multi_compile _ WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_MSAA_DEPTH

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"

            #ifdef WRITE_NORMAL_BUFFER // If enabled we need all regular interpolator
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
            #else
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitDepthPass.hlsl"
            #endif

			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LayeredLit/LayeredLitData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Forward" // Name is not used
            Tags{ "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            Blend [_SrcBlend][_DstBlend]
            // In case of forward we want to have depth equal for opaque mesh
            ZTest [_ZTestDepthEqualForOpaque]
            ZWrite [_ZWrite]
            Cull [_CullMode]

            HLSLPROGRAM

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // Setup DECALS_OFF so the shader stripper can remove variants
            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
            
            // Supported shadow modes per light type
            #pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH    

            // #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Lighting/Forward.hlsl"
            //#pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS
            #define LIGHTLOOP_TILE_PASS
            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #define SHADERPASS SHADERPASS_FORWARD
            // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
            #ifndef _SURFACE_TYPE_TRANSPARENT
                #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
            #endif
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"

        #ifdef DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #endif

            // The light loop (or lighting architecture) is in charge to:
            // - Define light list
            // - Define the light loop
            // - Setup the constant/data
            // - Do the reflection hierarchy
            // - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

            #define HAS_LIGHTLOOP

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/ShaderPass/LitSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LayeredLit/LayeredLitData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "UnityEditor.Experimental.Rendering.HDPipeline.LayeredLitGUI"
}
