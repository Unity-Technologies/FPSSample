// ===========================================================================
//                              WARNING:
// On PS4, texture/sampler declarations need to be outside of CBuffers
// Otherwise those parameters are not bound correctly at runtime.
// ===========================================================================
//


//////////////////////////////////////////////////////////////////////////////
// SVBRDF
TEXTURE2D(_SVBRDF_DiffuseColorMap);          // RGB Diffuse color (2.2 gamma must be applied)
TEXTURE2D(_SVBRDF_SpecularColorMap);         // RGB Specular color (2.2 gamma must be applied)
TEXTURE2D(_SVBRDF_NormalMap);                     // Tangent-Space Normal vector with offset (i.e. in [0,1], need to 2*normal-1 to get actual vector)
TEXTURE2D(_SVBRDF_SpecularLobeMap);               // Specular lobe in [0,1]. Either a scalar if isotropic, or a float2 if anisotropic.
TEXTURE2D(_SVBRDF_AlphaMap);                    // Alpha (scalar in [0,1])
TEXTURE2D(_SVBRDF_FresnelMap);               // RGB F0 (2.2 gamma must be applied)
TEXTURE2D(_SVBRDF_AnisoRotationMap);   // Rotation angle (scalar in [0,1], needs to be remapped in [0,2PI])
TEXTURE2D(_SVBRDF_HeightMap);                     // Height map (scalar in [0,1], need to be remapped with heightmap

SAMPLER(sampler_SVBRDF_DiffuseColorMap);
SAMPLER(sampler_SVBRDF_SpecularColorMap);
SAMPLER(sampler_SVBRDF_NormalMap);
SAMPLER(sampler_SVBRDF_SpecularLobeMap);
SAMPLER(sampler_SVBRDF_AlphaMap);
SAMPLER(sampler_SVBRDF_FresnelMap);
SAMPLER(sampler_SVBRDF_AnisoRotationMap);
SAMPLER(sampler_SVBRDF_HeightMap);


//////////////////////////////////////////////////////////////////////////////
// Car Paint
TEXTURE2D(_CarPaint2_BRDFColorMap);       // RGB BRDF color (2.2 gamma must be applied + scale)
TEXTURE2D_ARRAY(_CarPaint2_BTFFlakeMap); // RGB Flakes color (2.2 gamma must be applied + scale)
TEXTURE2D(_CarPaint2_FlakeThetaFISliceLUTMap);     // UINT indirection values (must be scaled by 255 and cast as UINTs)

SAMPLER(sampler_CarPaint2_BRDFColorMap);
SAMPLER(sampler_CarPaint2_BTFFlakeMap);
SAMPLER(sampler_CarPaint2_FlakeThetaFISliceLUTMap);


//////////////////////////////////////////////////////////////////////////////
// Other
TEXTURE2D(_SVBRDF_ClearcoatColorMap);        // RGB Clearcoat color (2.2 gamma must be applied)
TEXTURE2D(_ClearcoatNormalMap);            // Tangent-Space clearcoat Normal vector with offset (i.e. in [0,1], need to 2*normal-1 to get actual vector)
TEXTURE2D(_SVBRDF_ClearcoatIORMap);          // Clearcoat F0 (2.2 gamma must be applied)

SAMPLER(sampler_SVBRDF_ClearcoatColorMap);
SAMPLER(sampler_ClearcoatNormalMap);
SAMPLER(sampler_SVBRDF_ClearcoatIORMap);


CBUFFER_START(UnityPerMaterial)

    float   _MaterialTilingU;              // Size of the U range, in millimeters (currently used as UV scale factor)
    float   _MaterialTilingV;              // Size of the V range, in millimeters (currently used as UV scale factor)

    uint    _Flags;                         // Bit 0 = Anisotropic. If true, specular lobe map contains 2 channels and the _AnisotropicRotationAngleMap needs to be read
                                            // Bit 1 = HasClearcoat. If true, the clearcoat must be applied. The _ClearcoatNormalMap must be valid and contain clearcoat normal data.
                                            // Bit 2 = ClearcoatUseRefraction. If true, then _ClearcoatIORMap must be valid and contain clearcoat IOR data. If false then rays are not refracted by the clearcoat layer.
                                            // Bit 3 = useHeightMap. If true then displacement mapping is used and _HeightMap must contain valid data.
                                            //

    //////////////////////////////////////////////////////////////////////////////
    // SVBRDF
    uint    _SVBRDF_BRDFType;               // Bit 0 = Diffuse Type. 0 = Lambert, 1 = Oren-Nayar
                                            // Bit 1-3 = Specular Type. 0 = Ward, 1 = Blinn-Phong, 2 = Cook-Torrance, 3 = GGX, 4 = Phong
                                            //

    uint    _SVBRDF_BRDFVariants;           // Bit 0-1 = Fresnel Variant. 0 = No Fresnel, 1 = Dielectric (Cook-Torrance 1981), 2 = Schlick (1994)
                                            // Bit 2-3 = Ward NDF Variant. 0 = Moroder (2010), 1 = Dur (2006), 2 = Ward (1992)
                                            // Bit 4-5 = Blinn-Phong Variant. 0 = Ashikmin-Shirley (2000), 1 = Blinn (1977), 2 = V-Ray, 3 = Lewis (1993)
                                            //

    float   _SVBRDF_SpecularLobeMapScale;  // Optional scale factor to the specularLob map (useful when the map contains arbitrary Phong exponents)

    float   _SVBRDF_HeightMapMaxMM;        // Maximum height map displacement, in millimeters


    //////////////////////////////////////////////////////////////////////////////
    // Car Paint
    float   _CarPaint2_CTDiffuse;           // Diffuse factor
    float   _CarPaint2_ClearcoatIOR;                  // Clearcoat IOR

        // BRDF
    float   _CarPaint2_BRDFColorMapScale;   // Optional scale factor to the BRDFColor map
    float   _CarPaint2_BTFFlakeMapScale;   // Optional scale factor to the BTFFlakes map

        // Cook-Torrance Lobes Descriptors
    uint    _CarPaint2_LobeCount;           // Amount of valid components in the vectors below
    float4  _CarPaint2_CTF0s;               // Description of multi-lobes F0 values
    float4  _CarPaint2_CTCoeffs;            // Description of multi-lobes coefficients values
    float4  _CarPaint2_CTSpreads;           // Description of multi-lobes spread values

        // Flakes
    float   _CarPaint2_FlakeTiling;         // Tiling factor for flakes
    uint    _CarPaint2_FlakeMaxThetaI;            // Maximum thetaI index
    uint    _CarPaint2_FlakeNumThetaF;            // Amount of thetaF entries (in litterature, that's called thetaH, the angle between the normal and the half vector)
    uint    _CarPaint2_FlakeNumThetaI;            // Amount of thetaI entries (in litterature, that's called thetaD, the angle between the light/view and the half vector)


    //////////////////////////////////////////////////////////////////////////////

float _AlphaCutoff;
float4 _DoubleSidedConstants;

// Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
// value that exist to identify if the GI emission need to be enabled.
// In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
// TODO: Fix the code in legacy unity so we can customize the behavior for GI
float3 _EmissionColor;

// Following two variables are feeded by the C++ Editor for Scene selection
int _ObjectId;
int _PassValue;

CBUFFER_END
