using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class AxFGUI : BaseUnlitGUI
    {
        protected override uint defaultExpandedState { get { return (uint)(Expandable.Base | Expandable.Detail | Expandable.Emissive | Expandable.Input | Expandable.Other | Expandable.Tesselation | Expandable.Transparency | Expandable.VertexAnimation); } }

        protected static class Styles
        {
            public static string InputsText = "Inputs";

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // SVBRDF Parameters
            public static GUIContent    diffuseColorMapText = new GUIContent("Diffuse Color");
            public static GUIContent    specularColorMapText = new GUIContent("Specular Color");
            public static GUIContent    specularLobeMapText = new GUIContent("Specular Lobe");
            public static GUIContent    specularLobeMapScaleText = new GUIContent("Specular Lobe Scale");
            public static GUIContent    fresnelMapText = new GUIContent("Fresnel");
            public static GUIContent    normalMapText = new GUIContent("Normal");

            // Alpha
            public static GUIContent    alphaMapText = new GUIContent("Alpha");

            // Displacement
            public static GUIContent    heightMapText = new GUIContent("Height");

            // Anisotropy
            public static GUIContent    anisoRotationMapText = new GUIContent("Anisotropy Angle");

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Car Paint Parameters
            public static GUIContent    BRDFColorMapText = new GUIContent("BRDF Color");
            public static GUIContent    BRDFColorMapScaleText = new GUIContent("BRDF Color Scale");

            public static GUIContent    BTFFlakesMapText = new GUIContent("BTF Flake Color Texture2DArray");
            public static GUIContent    BTFFlakesMapScaleText = new GUIContent("BTF Flakes Scale");
            public static GUIContent    FlakesTilingText = new GUIContent("Flakes Tiling");

            public static GUIContent    thetaFI_sliceLUTMapText = new GUIContent("ThetaFI Slice LUT");

            public static GUIContent    CarPaintIORText = new GUIContent("IOR");

            /////////////////////////////////////////////////////////////////////////////////////////////////
            // Generic

            // Clearcoat
            public static GUIContent    clearcoatColorMapText = new GUIContent("Clearcoat Color");
            public static GUIContent    clearcoatNormalMapText = new GUIContent("Clearcoat Normal");
            public static GUIContent    clearcoatIORMapText = new GUIContent("Clearcoat IOR");
        }

        enum    AxfBrdfType
        {
            SVBRDF,
            CAR_PAINT,
            BTF,
        }
        static readonly string[]    AxfBrdfTypeNames = Enum.GetNames(typeof(AxfBrdfType));

        enum    SvbrdfDiffuseType
        {
            LAMBERT = 0,
            OREN_NAYAR = 1,
        }
        static readonly string[]    SvbrdfDiffuseTypeNames = Enum.GetNames(typeof(SvbrdfDiffuseType));

        enum    SvbrdfSpecularType
        {
            WARD = 0,
            BLINN_PHONG = 1,
            COOK_TORRANCE = 2,
            GGX = 3,
            PHONG = 4,
        }
        static readonly string[]    SvbrdfSpecularTypeNames = Enum.GetNames(typeof(SvbrdfSpecularType));

        enum    SvbrdfSpecularVariantWard   // Ward variants
        {
            GEISLERMORODER,     // 2010 (albedo-conservative, should always be preferred!)
            DUER,               // 2006
            WARD,               // 1992 (original paper)
        }
        static readonly string[]    SvbrdfSpecularVariantWardNames = Enum.GetNames(typeof(SvbrdfSpecularVariantWard));
        enum    SvbrdfSpecularVariantBlinn  // Blinn-Phong variants
        {
            ASHIKHMIN_SHIRLEY,  // 2000
            BLINN,              // 1977 (original paper)
            VRAY,
            LEWIS,              // 1993
        }
        static readonly string[]    SvbrdfSpecularVariantBlinnNames = Enum.GetNames(typeof(SvbrdfSpecularVariantBlinn));

        enum    SvbrdfFresnelVariant
        {
            NO_FRESNEL,         // No fresnel
            FRESNEL,            // Full fresnel (1818)
            SCHLICK,            // Schlick's Approximation (1994)
        }
        static readonly string[]    SvbrdfFresnelVariantNames = Enum.GetNames(typeof(SvbrdfFresnelVariant));

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Generic Parameters
        static string               m_MaterialTilingUText = "_MaterialTilingU";
        protected MaterialProperty  m_MaterialTilingU;
        static string               m_MaterialTilingVText = "_MaterialTilingV";
        protected MaterialProperty  m_MaterialTilingV;

        static string               m_AxF_BRDFTypeText = "_AxF_BRDFType";
        protected MaterialProperty  m_AxF_BRDFType = null;

        static string               m_FlagsText = "_Flags";
        protected MaterialProperty  m_Flags;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // SVBRDF Parameters
        static string               m_SVBRDF_BRDFTypeText = "_SVBRDF_BRDFType";
        protected MaterialProperty  m_SVBRDF_BRDFType;
        static string               m_SVBRDF_BRDFVariantsText = "_SVBRDF_BRDFVariants";
        protected MaterialProperty  m_SVBRDF_BRDFVariants;
        static string               m_SVBRDF_HeightMapMaxMMText = "_SVBRDF_HeightMapMaxMM";
        protected MaterialProperty  m_SVBRDF_HeightMapMaxMM;

        // Regular maps
        static string               m_DiffuseColorMapText = "_SVBRDF_DiffuseColorMap";
        protected MaterialProperty  m_DiffuseColorMap = null;
        static string               m_SpecularColorMapText = "_SVBRDF_SpecularColorMap";
        protected MaterialProperty  m_SpecularColorMap = null;

        static string               m_SpecularLobeMapText = "_SVBRDF_SpecularLobeMap";
        protected MaterialProperty  m_SpecularLobeMap = null;
        static string               m_SpecularLobeMapScaleText = "_SVBRDF_SpecularLobeMapScale";
        protected MaterialProperty  m_SpecularLobeMapScale;

        static string               m_FresnelMapText = "_SVBRDF_FresnelMap";
        protected MaterialProperty  m_FresnelMap = null;
        static string               m_NormalMapText = "_SVBRDF_NormalMap";
        protected MaterialProperty  m_NormalMap = null;

        // Alpha
        static string               m_AlphaMapText = "_SVBRDF_AlphaMap";
        protected MaterialProperty  m_AlphaMap = null;

        // Displacement
        static string               m_HeightMapText = "_SVBRDF_HeightMap";
        protected MaterialProperty  m_HeightMap = null;

        // Anisotropy
        static string               m_AnisoRotationMapText = "_SVBRDF_AnisoRotationMap";
        protected MaterialProperty  m_AnisoRotationMap = null;


        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Car Paint Parameters
        static string               m_CarPaint2_BRDFColorMapText = "_CarPaint2_BRDFColorMap";
        protected MaterialProperty  m_CarPaint2_BRDFColorMap = null;

        static string               m_CarPaint2_BRDFColorMapScaleText = "_CarPaint2_BRDFColorMapScale";
        protected MaterialProperty  m_CarPaint2_BRDFColorMapScale;

        static string               m_CarPaint2_BTFFlakeMapText = "_CarPaint2_BTFFlakeMap";
        protected MaterialProperty  m_CarPaint2_BTFFlakeMap = null;

        static string               m_CarPaint2_BTFFlakeMapScaleText = "_CarPaint2_BTFFlakeMapScale";
        protected MaterialProperty  m_CarPaint2_BTFFlakeMapScale;

        static string               m_CarPaint2_FlakeTilingText = "_CarPaint2_FlakeTiling";
        protected MaterialProperty  m_CarPaint2_FlakeTiling;

        static string               m_CarPaint2_FlakeThetaFISliceLUTMapText = "_CarPaint2_FlakeThetaFISliceLUTMap";
        protected MaterialProperty  m_CarPaint2_FlakeThetaFISliceLUTMap;

        static string               m_CarPaint2_FlakeMaxThetaIText = "_CarPaint2_FlakeMaxThetaI";
        protected MaterialProperty  m_CarPaint2_FlakeMaxThetaI;
        static string               m_CarPaint2_FlakeNumThetaFText = "_CarPaint2_FlakeNumThetaF";
        protected MaterialProperty  m_CarPaint2_FlakeNumThetaF;
        static string               m_CarPaint2_FlakeNumThetaIText = "_CarPaint2_FlakeNumThetaI";
        protected MaterialProperty  m_CarPaint2_FlakeNumThetaI;

        static string               m_CarPaint2_ClearcoatIORText = "_CarPaint2_ClearcoatIOR";
        protected MaterialProperty  m_CarPaint2_ClearcoatIOR;

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Clearcoat
        static string               m_ClearcoatColorMapText = "_SVBRDF_ClearcoatColorMap";
        protected MaterialProperty  m_ClearcoatColorMap = null;
        static string               m_ClearcoatNormalMapText = "_ClearcoatNormalMap";
        protected MaterialProperty  m_ClearcoatNormalMap = null;
        static string               m_ClearcoatIORMapText = "_SVBRDF_ClearcoatIORMap";
        protected MaterialProperty  m_ClearcoatIORMap = null;

        override protected void FindMaterialProperties(MaterialProperty[] props)
        {
            m_MaterialTilingU = FindProperty(m_MaterialTilingUText, props);
            m_MaterialTilingV = FindProperty(m_MaterialTilingVText, props);

            m_AxF_BRDFType = FindProperty(m_AxF_BRDFTypeText, props);

            m_Flags = FindProperty(m_FlagsText, props);

            //////////////////////////////////////////////////////////////////////////
            // SVBRDF
            m_SVBRDF_BRDFType = FindProperty(m_SVBRDF_BRDFTypeText, props);
            m_SVBRDF_BRDFVariants = FindProperty(m_SVBRDF_BRDFVariantsText, props);
            m_SVBRDF_HeightMapMaxMM = FindProperty(m_SVBRDF_HeightMapMaxMMText, props);

            // Regular maps
            m_DiffuseColorMap = FindProperty(m_DiffuseColorMapText, props);
            m_SpecularColorMap = FindProperty(m_SpecularColorMapText, props);
            m_SpecularLobeMap = FindProperty(m_SpecularLobeMapText, props);
            m_SpecularLobeMapScale = FindProperty(m_SpecularLobeMapScaleText, props);
            m_FresnelMap = FindProperty(m_FresnelMapText, props);
            m_NormalMap = FindProperty(m_NormalMapText, props);

            // Alpha
            m_AlphaMap = FindProperty(m_AlphaMapText, props);

            // Displacement
            m_HeightMap = FindProperty(m_HeightMapText, props);

            // Anisotropy
            m_AnisoRotationMap = FindProperty(m_AnisoRotationMapText, props);


            //////////////////////////////////////////////////////////////////////////
            // Car Paint
            m_CarPaint2_BRDFColorMap = FindProperty(m_CarPaint2_BRDFColorMapText, props);
            m_CarPaint2_BTFFlakeMap = FindProperty(m_CarPaint2_BTFFlakeMapText, props);
            m_CarPaint2_FlakeThetaFISliceLUTMap = FindProperty(m_CarPaint2_FlakeThetaFISliceLUTMapText, props);

            m_CarPaint2_BRDFColorMapScale = FindProperty(m_CarPaint2_BRDFColorMapScaleText, props);
            m_CarPaint2_BTFFlakeMapScale = FindProperty(m_CarPaint2_BTFFlakeMapScaleText, props);
            m_CarPaint2_FlakeTiling = FindProperty(m_CarPaint2_FlakeTilingText, props);

            m_CarPaint2_FlakeMaxThetaI = FindProperty(m_CarPaint2_FlakeMaxThetaIText, props);
            m_CarPaint2_FlakeNumThetaF = FindProperty(m_CarPaint2_FlakeNumThetaFText, props);
            m_CarPaint2_FlakeNumThetaI = FindProperty(m_CarPaint2_FlakeNumThetaIText, props);

            m_CarPaint2_ClearcoatIOR = FindProperty(m_CarPaint2_ClearcoatIORText, props);

            //////////////////////////////////////////////////////////////////////////
            // Clearcoat
            m_ClearcoatColorMap = FindProperty(m_ClearcoatColorMapText, props);
            m_ClearcoatNormalMap = FindProperty(m_ClearcoatNormalMapText, props);
            m_ClearcoatIORMap = FindProperty(m_ClearcoatIORMapText, props);
        }

        protected unsafe override void MaterialPropertiesGUI(Material _material)
        {
            EditorGUILayout.LabelField(Styles.InputsText, EditorStyles.boldLabel);

            m_MaterialEditor.ShaderProperty(m_MaterialTilingU, "Material Tiling U");
            m_MaterialEditor.ShaderProperty(m_MaterialTilingV, "Material Tiling V");

            AxfBrdfType AxF_BRDFType = (AxfBrdfType)m_AxF_BRDFType.floatValue;
            AxF_BRDFType = (AxfBrdfType)EditorGUILayout.Popup("BRDF Type", (int)AxF_BRDFType, AxfBrdfTypeNames);
            m_AxF_BRDFType.floatValue = (float)AxF_BRDFType;

            switch (AxF_BRDFType)
            {
                case AxfBrdfType.SVBRDF:
                {
                    EditorGUILayout.Space();
                    ++EditorGUI.indentLevel;

                    // Read as compact flags
                    uint    flags = (uint)m_Flags.floatValue;
                    uint    BRDFType = (uint)m_SVBRDF_BRDFType.floatValue;
                    uint    BRDFVariants = (uint)m_SVBRDF_BRDFVariants.floatValue;

                    SvbrdfDiffuseType diffuseType = (SvbrdfDiffuseType)(BRDFType & 0x1);
                    SvbrdfSpecularType specularType = (SvbrdfSpecularType)((BRDFType >> 1) & 0x7);
                    SvbrdfFresnelVariant fresnelVariant = (SvbrdfFresnelVariant)(BRDFVariants & 0x3);
                    SvbrdfSpecularVariantWard wardVariant = (SvbrdfSpecularVariantWard)((BRDFVariants >> 2) & 0x3);
                    SvbrdfSpecularVariantBlinn blinnVariant = (SvbrdfSpecularVariantBlinn)((BRDFVariants >> 4) & 0x3);

                    // Expand as user-friendly UI
//                     EditorGUILayout.LabelField( "Flags", EditorStyles.boldLabel );
                    EditorGUILayout.LabelField("BRDF Variants", EditorStyles.boldLabel);

                    diffuseType = (SvbrdfDiffuseType)EditorGUILayout.Popup("Diffuse Type", (int)diffuseType, SvbrdfDiffuseTypeNames);
                    specularType = (SvbrdfSpecularType)EditorGUILayout.Popup("Specular Type", (int)specularType, SvbrdfSpecularTypeNames);

                    if (specularType == SvbrdfSpecularType.WARD)
                    {
                        fresnelVariant = (SvbrdfFresnelVariant)EditorGUILayout.Popup("Fresnel Variant", (int)fresnelVariant, SvbrdfFresnelVariantNames);
                        wardVariant = (SvbrdfSpecularVariantWard)EditorGUILayout.Popup("Ward Variant", (int)wardVariant, SvbrdfSpecularVariantWardNames);
                    }
                    else if (specularType == SvbrdfSpecularType.BLINN_PHONG)
                    {
                        blinnVariant = (SvbrdfSpecularVariantBlinn)EditorGUILayout.Popup("Blinn Variant", (int)blinnVariant, SvbrdfSpecularVariantBlinnNames);
                    }

                    // Regular maps
                    m_MaterialEditor.TexturePropertySingleLine(Styles.diffuseColorMapText, m_DiffuseColorMap);
                    m_MaterialEditor.TexturePropertySingleLine(Styles.specularColorMapText, m_SpecularColorMap);
                    m_MaterialEditor.TexturePropertySingleLine(Styles.specularLobeMapText, m_SpecularLobeMap);
                    m_SpecularLobeMapScale.floatValue = EditorGUILayout.FloatField(Styles.specularLobeMapScaleText, m_SpecularLobeMapScale.floatValue);
                    m_MaterialEditor.TexturePropertySingleLine(Styles.fresnelMapText, m_FresnelMap);
                    m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, m_NormalMap);

                    // Alpha
                    m_MaterialEditor.TexturePropertySingleLine(Styles.alphaMapText, m_AlphaMap);

                    // Displacement
                    bool    useDisplacementMap = EditorGUILayout.Toggle("Enable Displacement Map", (flags & 8) != 0);
                    if (useDisplacementMap)
                    {
                        ++EditorGUI.indentLevel;
                        m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, m_HeightMap);
                        m_MaterialEditor.ShaderProperty(m_SVBRDF_HeightMapMaxMM, "Max Displacement (mm)");
                        --EditorGUI.indentLevel;
                    }

                    // Anisotropy
                    bool    isAnisotropic = EditorGUILayout.Toggle("Is Anisotropic", (flags & 1) != 0);
                    if (isAnisotropic)
                    {
                        ++EditorGUI.indentLevel;
                        m_MaterialEditor.TexturePropertySingleLine(Styles.anisoRotationMapText, m_AnisoRotationMap);
                        --EditorGUI.indentLevel;
                    }

                    // Clearcoat
                    bool    hasClearcoat = EditorGUILayout.Toggle("Enable Clearcoat", (flags & 2) != 0);
                    bool    clearcoatUsesRefraction = (flags & 4) != 0;
                    if (hasClearcoat)
                    {
                        ++EditorGUI.indentLevel;
                        m_MaterialEditor.TexturePropertySingleLine(Styles.clearcoatColorMapText, m_ClearcoatColorMap);
                        m_MaterialEditor.TexturePropertySingleLine(Styles.clearcoatNormalMapText, m_ClearcoatNormalMap);
                        clearcoatUsesRefraction = EditorGUILayout.Toggle("Enable Refraction", clearcoatUsesRefraction);
                        if (clearcoatUsesRefraction)
                        {
                            ++EditorGUI.indentLevel;
                            m_MaterialEditor.TexturePropertySingleLine(Styles.clearcoatIORMapText, m_ClearcoatIORMap);
                            --EditorGUI.indentLevel;
                        }
                        --EditorGUI.indentLevel;
                    }

                    // Write back as compact flags
                    flags = 0;
                    flags |= isAnisotropic ? 1U : 0U;
                    flags |= hasClearcoat ? 2U : 0U;
                    flags |= clearcoatUsesRefraction ? 4U : 0U;
                    flags |= useDisplacementMap ? 8U : 0U;

                    BRDFType = 0;
                    BRDFType |= (uint)diffuseType;
                    BRDFType |= ((uint)specularType) << 1;

                    BRDFVariants = 0;
                    BRDFVariants |= (uint)fresnelVariant;
                    BRDFVariants |= ((uint)wardVariant) << 2;
                    BRDFVariants |= ((uint)blinnVariant) << 4;

//                    cmd.SetGlobalFloat( HDShaderIDs._TexturingModeFlags, *(float*) &texturingModeFlags );
                    m_Flags.floatValue = (float)flags;
                    m_SVBRDF_BRDFType.floatValue = (float)BRDFType;
                    m_SVBRDF_BRDFVariants.floatValue = (float)BRDFVariants;

                    --EditorGUI.indentLevel;
                    break;
                }

                case AxfBrdfType.CAR_PAINT:
                {
                    EditorGUILayout.Space();
                    ++EditorGUI.indentLevel;

                    // Read as compact flags
                    uint    flags = (uint)m_Flags.floatValue;

                    bool    isAnisotropic = false;
                    bool    useDisplacementMap = false;

                    // Expand as user-friendly UI

                    // Regular maps
                    m_MaterialEditor.TexturePropertySingleLine(Styles.BRDFColorMapText, m_CarPaint2_BRDFColorMap);
                    m_CarPaint2_BRDFColorMapScale.floatValue = EditorGUILayout.FloatField(Styles.BRDFColorMapScaleText, m_CarPaint2_BRDFColorMapScale.floatValue);

                    m_MaterialEditor.TexturePropertySingleLine(Styles.BTFFlakesMapText, m_CarPaint2_BTFFlakeMap);
                    //EditorGUILayout.LabelField( "Texture Dimension = " + m_CarPaint_BTFFlakesMap_sRGB.textureDimension );
                    //EditorGUILayout.LabelField( "Texture Format = " + m_CarPaint_BTFFlakesMap_sRGB.textureValue. );
                    m_CarPaint2_BTFFlakeMapScale.floatValue = EditorGUILayout.FloatField(Styles.BTFFlakesMapScaleText, m_CarPaint2_BTFFlakeMapScale.floatValue);
                    m_CarPaint2_FlakeTiling.floatValue = EditorGUILayout.FloatField(Styles.FlakesTilingText, m_CarPaint2_FlakeTiling.floatValue);

                    m_MaterialEditor.TexturePropertySingleLine(Styles.thetaFI_sliceLUTMapText, m_CarPaint2_FlakeThetaFISliceLUTMap);

                    // m_CarPaint_maxThetaI = FindProperty( m_CarPaint_maxThetaIText, props );
                    // m_CarPaint_numThetaF = FindProperty( m_CarPaint_numThetaFText, props );
                    // m_CarPaint_numThetaI = FindProperty( m_CarPaint_numThetaIText, props );


                    // Clearcoat
                    bool    hasClearcoat = EditorGUILayout.Toggle("Enable Clearcoat", (flags & 2) != 0);
                    bool    clearcoatUsesRefraction = (flags & 4) != 0;
                    if (hasClearcoat)
                    {
                        ++EditorGUI.indentLevel;
//                        m_MaterialEditor.TexturePropertySingleLine( Styles.clearcoatColorMapText, m_ClearcoatColorMap );
                        m_MaterialEditor.TexturePropertySingleLine(Styles.clearcoatNormalMapText, m_ClearcoatNormalMap);
//                        if ( clearcoatUsesRefraction ) {
                        {
                            ++EditorGUI.indentLevel;
//                            m_MaterialEditor.TexturePropertySingleLine( Styles.clearcoatIORMapText, m_ClearcoatIORMap );
                            m_CarPaint2_ClearcoatIOR.floatValue = EditorGUILayout.FloatField(Styles.CarPaintIORText, m_CarPaint2_ClearcoatIOR.floatValue);
                            --EditorGUI.indentLevel;
                        }
                        --EditorGUI.indentLevel;
                        clearcoatUsesRefraction = EditorGUILayout.Toggle("Enable Refraction", clearcoatUsesRefraction);
                    }

                    // Write back as compact flags
                    flags = 0;
                    flags |= isAnisotropic ? 1U : 0U;
                    flags |= hasClearcoat ? 2U : 0U;
                    flags |= clearcoatUsesRefraction ? 4U : 0U;
                    flags |= useDisplacementMap ? 8U : 0U;

//                    cmd.SetGlobalFloat( HDShaderIDs._TexturingModeFlags, *(float*) &texturingModeFlags );
                    m_Flags.floatValue = (float)flags;

                    --EditorGUI.indentLevel;
                    break;
                }
            }
        }

        protected override void MaterialPropertiesAdvanceGUI(Material _material)
        {
        }

        protected override void VertexAnimationPropertiesGUI()
        {
        }

        protected override bool ShouldEmissionBeEnabled(Material _material)
        {
            return false;//_material.GetFloat(kEmissiveIntensity) > 0.0f;
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material _material)
        {
            SetupMaterialKeywordsAndPass(_material);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material _material)
        {
            SetupBaseUnlitKeywords(_material);
            SetupBaseUnlitMaterialPass(_material);

            AxfBrdfType   BRDFType = (AxfBrdfType)_material.GetFloat(m_AxF_BRDFTypeText);

            CoreUtils.SetKeyword(_material, "_AXF_BRDF_TYPE_SVBRDF", BRDFType == AxfBrdfType.SVBRDF);
            CoreUtils.SetKeyword(_material, "_AXF_BRDF_TYPE_CAR_PAINT", BRDFType == AxfBrdfType.CAR_PAINT);
            CoreUtils.SetKeyword(_material, "_AXF_BRDF_TYPE_BTF", BRDFType == AxfBrdfType.BTF);
        }
    }
} // namespace UnityEditor
