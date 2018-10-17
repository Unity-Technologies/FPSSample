using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class StackLitGUI : BaseMaterialGUI
    {
        static Expendable state = Expendable.Base | Expendable.Input | Expendable.VertexAnimation | Expendable.Detail | Expendable.Emissive | Expendable.Transparency | Expendable.Other;
        protected override uint expendedState { get { return (uint)state; } set { state = (Expendable)value; } }

        protected static class StylesStackLit
        {
            public const string stackOptionText = "Stack Option";

            public static GUIContent useLocalPlanarMapping = new GUIContent("Use Local Planar Mapping", "Use local space for planar/triplanar mapping instead of world space");
        };

        #region Strings

        protected const string k_DoubleSidedNormalMode = "_DoubleSidedNormalMode";

        protected const string k_UVBase = "_UVBase";

        // Base
        protected const string k_BaseColor = "_BaseColor";
        protected const string k_BaseColorMap = "_BaseColorMap";
        protected const string k_BaseColorMapUV = "_BaseColorMapUV";

        protected const string k_Metallic = "_Metallic";
        protected const string k_MetallicMap = "_MetallicMap";
        protected const string k_MetallicMapUV = "_MetallicMapUV";

        protected const string k_DielectricIor = "_DielectricIor";

        protected const string k_SmoothnessA = "_SmoothnessA";
        protected const string k_SmoothnessAMap = "_SmoothnessAMap";
        protected const string k_SmoothnessAMapUV = "_SmoothnessAMapUV";

        protected const string k_NormalMap = "_NormalMap";
        protected const string k_NormalMapUV = "_NormalMapUV";
        protected const string k_NormalScale = "_NormalScale";

        protected const string k_AmbientOcclusion = "_AmbientOcclusion";
        protected const string k_AmbientOcclusionMap = "_AmbientOcclusionMap";
        protected const string k_AmbientOcclusionMapUV = "_AmbientOcclusionMapUV";

        // Emissive
        protected const string k_EmissiveColor = "_EmissiveColor";
        protected const string k_EmissiveColorMap = "_EmissiveColorMap";
        protected const string k_EmissiveColorMapUV = "_EmissiveColorMapUV";
        protected const string k_AlbedoAffectEmissive = "_AlbedoAffectEmissive";

        // Coat
        protected const string k_EnableCoat = "_EnableCoat";
        protected const string k_CoatSmoothness = "_CoatSmoothness";
        protected const string k_CoatSmoothnessMap = "_CoatSmoothnessMap";
        protected const string k_CoatSmoothnessMapUV = "_CoatSmoothnessMapUV";
        protected const string k_CoatIor = "_CoatIor";
        protected const string k_CoatThickness = "_CoatThickness";
        protected const string k_CoatExtinction = "_CoatExtinction";
        protected const string k_EnableCoatNormalMap = "_EnableCoatNormalMap";
        protected const string k_CoatNormalMap = "_CoatNormalMap";
        protected const string k_CoatNormalMapUV = "_CoatNormalMapUV";
        protected const string k_CoatNormalScale = "_CoatNormalScale";

        // SSS
        protected const string k_EnableSubsurfaceScattering = "_EnableSubsurfaceScattering";
        protected const string k_DiffusionProfile = "_DiffusionProfile";
        protected const string k_SubsurfaceMask = "_SubsurfaceMask";
        protected const string k_SubsurfaceMaskMap = "_SubsurfaceMaskMap";
        protected const string k_SubsurfaceMaskMapUV = "_SubsurfaceMaskMapUV";

        // Translucency
        protected const string k_EnableTransmission = "_EnableTransmission";
        protected const string k_Thickness = "_Thickness";
        protected const string k_ThicknessMap = "_ThicknessMap";
        protected const string k_ThicknessMapUV = "_ThicknessMapUV";

        // Second Lobe.
        protected const string k_EnableDualSpecularLobe = "_EnableDualSpecularLobe";
        protected const string k_SmoothnessB = "_SmoothnessB";
        protected const string k_SmoothnessBMap = "_SmoothnessBMap";
        protected const string k_SmoothnessBMapUV = "_SmoothnessBMapUV";

        protected const string k_LobeMix = "_LobeMix";

        // Anisotropy
        protected const string k_EnableAnisotropy = "_EnableAnisotropy";
        protected const string k_Anisotropy = "_Anisotropy";
        protected const string k_AnisotropyMap = "_AnisotropyMap";
        protected const string k_AnisotropyMapUV = "_AnisotropyMapUV";

        // Iridescence
        protected const string k_EnableIridescence = "_EnableIridescence";
        protected const string k_IridescenceIor = "_IridescenceIor";
        protected const string k_IridescenceThickness = "_IridescenceThickness";
        protected const string k_IridescenceThicknessMap = "_IridescenceThicknessMap";
        protected const string k_IridescenceThicknessMapUV = "_IridescenceThicknessMapUV";
        protected const string k_IridescenceMask = "_IridescenceMask";
        protected const string k_IridescenceMaskMap = "_IridescenceMaskMap";
        protected const string k_IridescenceMaskMapUV = "_IridescenceMaskMapUV";

        // Details
        protected const string k_EnableDetails = "_EnableDetails";
        
        protected const string k_DetailMask = "_DetailMask";
        protected const string k_DetailMaskMap = "_DetailMaskMap";
        protected const string k_DetailMaskMapUV = "_DetailMaskMapUV";

        protected const string k_DetailSmoothness = "_DetailSmoothness";
        protected const string k_DetailSmoothnessScale = "_DetailSmoothnessScale";
        protected const string k_DetailSmoothnessMap = "_DetailSmoothnessMap";
        protected const string k_DetailSmoothnessMapUV = "_DetailSmoothnessMapUV";

        protected const string k_DetailNormalMap = "_DetailNormalMap";
        protected const string k_DetailNormalMapUV = "_DetailNormalMapUV";
        protected const string k_DetailNormalScale = "_DetailNormalScale";

        // Stencil is use to control lighting mode (regular, split lighting)
        protected const string kStencilRef = "_StencilRef";
        protected const string kStencilWriteMask = "_StencilWriteMask";
        protected const string kStencilRefMV = "_StencilRefMV";
        protected const string kStencilWriteMaskMV = "_StencilWriteMaskMV";
        protected const string kStencilDepthPrepassRef = "_StencilDepthPrepassRef";
        protected const string kStencilDepthPrepassWriteMask = "_StencilDepthPrepassWriteMask";

        protected const string k_GeometricNormalFilteringEnabled = "_GeometricNormalFilteringEnabled";
        protected const string k_TextureNormalFilteringEnabled = "_TextureNormalFilteringEnabled";

        #endregion

        // Add the properties into an array.
        private readonly GroupProperty _baseMaterialProperties = null;
        private readonly GroupProperty _materialProperties = null;

        private Property EnableDetails;
        private Property EnableSSS;
        private Property EnableTransmission;
        private Property EnableCoat;
        private Property EnableCoatNormalMap;
        private Property EnableAnisotropy;
        private Property EnableDualSpecularLobe;        
        private Property EnableIridescence;

        private Property EnableGeometricNormalFiltering;
        private Property EnableTextureNormalFiltering;

        protected bool stackOptionExpended = true;

        public StackLitGUI()
        {
            _baseMaterialProperties = new GroupProperty(this, "_BaseMaterial", new BaseProperty[]
            {
                // JFFTODO: Find the proper condition, and proper way to display this.
                new Property(this, k_DoubleSidedNormalMode, "Normal mode", "This will modify the normal base on the selected mode. Mirror: Mirror the normal with vertex normal plane, Flip: Flip the normal.", false),
            });

            //
            EnableDetails = new Property(this, k_EnableDetails, "Enable Details", "Enable Detail", true);
            EnableSSS = new Property(this, k_EnableSubsurfaceScattering, "Enable Subsurface Scattering", "Enable Subsurface Scattering", true);
            EnableTransmission = new Property(this, k_EnableTransmission, "Enable Transmission", "Enable Transmission", true);
            EnableCoat = new Property(this, k_EnableCoat, "Enable Coat", "Enable coat layer with true vertical physically based BSDF mixing", true);
            EnableCoatNormalMap = new Property(this, k_EnableCoatNormalMap, "Enable Coat Normal Map", "Enable separate top coat normal map", true);
            EnableAnisotropy = new Property(this, k_EnableAnisotropy, "Enable Anisotropy", "Enable anisotropy, correct anisotropy for punctual light but very coarse approximated for reflection", true);
            EnableDualSpecularLobe = new Property(this, k_EnableDualSpecularLobe, "Enable Dual Specular Lobe", "Enable a second specular lobe, aim to simulate a mix of a narrow and a haze lobe that better match measured material", true);
            EnableIridescence = new Property(this, k_EnableIridescence, "Enable Iridescence", "Enable physically based iridescence layer", true);

            EnableGeometricNormalFiltering = new Property(this, k_GeometricNormalFilteringEnabled, "Enable Geometric filtering", "Enable specular antialiasing", true);
            EnableTextureNormalFiltering = new Property(this, k_TextureNormalFilteringEnabled, "Enable Texture filtering", "Require normal map to use _NA or _OSNA suffix for normal map name", true);

            // All material properties
            // All GroupPropery below need to define a
            // [HideInInspector] _XXXShow("_XXXShow", Float) = 0.0 parameter in the StackLit.shader to work
            _materialProperties = new GroupProperty(this, "_Material", new BaseProperty[]
            {
                new GroupProperty(this, "_MaterialFeatures", "Material Features", new BaseProperty[]
                {
                    EnableDetails,
                    EnableDualSpecularLobe,
                    EnableAnisotropy,
                    EnableCoat,
                    EnableCoatNormalMap,
                    EnableIridescence,
                    EnableSSS,
                    EnableTransmission
                }),

                new GroupProperty(this, "_Standard", "Standard", new BaseProperty[]
                {
                    new TextureProperty(this, k_BaseColorMap, k_BaseColor, "Base Color + Opacity", "Albedo (RGB) and Opacity (A)", true, false),
                    new TextureProperty(this, k_MetallicMap, k_Metallic, "Metallic", "Metallic", false, false),
                    new Property(this, k_DielectricIor, "DieletricIor", "IOR use for dielectric material (i.e non metallic material)", false),
                    new TextureProperty(this, k_SmoothnessAMap, k_SmoothnessA, "Smoothness", "Smoothness", false, false),
                    new TextureProperty(this, k_NormalMap, k_NormalScale, "Normal", "Normal Map", true, false, true),
                    new TextureProperty(this, k_AmbientOcclusionMap, k_AmbientOcclusion, "AmbientOcclusion", "AmbientOcclusion Map", false, false),
                }),

                new GroupProperty(this, "_Details", "Details", new BaseProperty[]
                {
                    new TextureProperty(this, k_DetailMaskMap, "", "Detail Mask Map", "Detail Mask Map", false, false),
                    new TextureProperty(this, k_DetailNormalMap, k_DetailNormalScale, "Detail Normal Map", "Detail Normal Map Scale", true, false, true),
                    new TextureProperty(this, k_DetailSmoothnessMap, k_DetailSmoothnessScale, "Detail Smoothness", "Detail Smoothness", true, false),
                }, _ => EnableDetails.BoolValue == true),

                new GroupProperty(this, "_DualSpecularLobe", "Dual Specular Lobe", new BaseProperty[]
                {
                    new TextureProperty(this, k_SmoothnessBMap, k_SmoothnessB, "Smoothness B", "Smoothness B", false, false),
                    new Property(this, k_LobeMix, "Lobe Mix", "Lobe Mix", false),
                }, _ => EnableDualSpecularLobe.BoolValue == true),

                new GroupProperty(this, "_Anisotropy", "Anisotropy", new BaseProperty[]
                {
                    new Property(this, k_Anisotropy, "Anisotropy", "Anisotropy of base layer", false),
                    // TODO: Tangent map and rotation
                }, _ => EnableAnisotropy.BoolValue == true),

                new GroupProperty(this, "_Coat", "Coat", new BaseProperty[]
                {
                    new TextureProperty(this, k_CoatSmoothnessMap, k_CoatSmoothness, "Coat smoothness", "Coat smoothness", false),
                    new TextureProperty(this, k_CoatNormalMap, k_CoatNormalScale, "Coat Normal Map", "Coat Normal Map", true, false, true,  _ => EnableCoatNormalMap.BoolValue == true),
                    new Property(this, "_CoatIor", "Coat IOR", "Index of refraction", false),
                    new Property(this, "_CoatThickness", "Coat Thickness", "Coat thickness", false),
                    new Property(this, "_CoatExtinction", "Coat Absorption", "Coat absorption tint (the thicker the coat, the more that color is removed)", false),
                }, _ => EnableCoat.BoolValue == true),

                new GroupProperty(this, "_Iridescence", "Iridescence", new BaseProperty[]
                {
                    //just to test: to use the same EvalIridescence as lit, find a good mapping for the top IOR (over the iridescence dielectric film)
                    //when having iridescence:
                    //new Property(this, "_IridescenceIor", "TopIOR", "Index of refraction on top of iridescence layer", false),
                    new TextureProperty(this, k_IridescenceMaskMap, k_IridescenceMask, "Iridescence Mask", "Iridescence Mask", false),
                    new TextureProperty(this, k_IridescenceThicknessMap, k_IridescenceThickness, "Iridescence thickness (Remap to 0..3000nm)", "Iridescence thickness (Remap to 0..3000nm)", false),
                }, _ => EnableIridescence.BoolValue == true),

                new GroupProperty(this, "_SSS", "Sub-Surface Scattering", new BaseProperty[]
                {
                    new DiffusionProfileProperty(this, k_DiffusionProfile, "Diffusion Profile", "A profile determines the shape of the SSS/transmission filter.", false),
                    new TextureProperty(this, k_SubsurfaceMaskMap, k_SubsurfaceMask, "Subsurface mask map (R)", "Determines the strength of the subsurface scattering effect.", false, false),
                }, _ => EnableSSS.BoolValue == true),

                new GroupProperty(this, "_Transmission", "Transmission", new BaseProperty[]
                {
                    new DiffusionProfileProperty(this, k_DiffusionProfile, "Diffusion Profile", "A profile determines the shape of the SSS/transmission filter.", false, _ => EnableSSS.BoolValue == false),
                    new TextureProperty(this, k_ThicknessMap, k_Thickness, "Thickness", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.", false),
                }, _ => EnableTransmission.BoolValue == true),

                new GroupProperty(this, "_Emissive", "Emissive", new BaseProperty[]
                {
                    new TextureProperty(this, k_EmissiveColorMap, k_EmissiveColor, "Emissive Color", "Emissive", true, false),
                    new Property(this, k_AlbedoAffectEmissive, "Albedo Affect Emissive", "Specifies whether or not the emissive color is multiplied by the albedo.", false),
                }),

                new GroupProperty(this, "_SpecularAntiAliasing", "Specular Anti-Aliasing", new BaseProperty[]
                {
                    EnableTextureNormalFiltering,
                    EnableGeometricNormalFiltering,
                    new Property(this, "_SpecularAntiAliasingThreshold", "Threshold", "Threshold", false, _ => (EnableGeometricNormalFiltering.BoolValue || EnableTextureNormalFiltering.BoolValue) == true),
                    new Property(this, "_SpecularAntiAliasingScreenSpaceVariance", "Screen Space Variance", "Screen Space Variance (should be less than 0.25)", false, _ => EnableGeometricNormalFiltering.BoolValue == true),
                }),

                new GroupProperty(this, "_Debug", "Debug", new BaseProperty[]
                {
                    new Property(this, "_VlayerRecomputePerLight", "Vlayer Recompute Per Light", "", false),
                    new Property(this, "_VlayerUseRefractedAnglesForBase", "Vlayer Use Refracted Angles For Base", "", false),
                    new Property(this, "_DebugEnable", "Debug Enable", "Switch to a debug version of the shader", false),
                    new Property(this, "_DebugEnvLobeMask", "DebugEnvLobeMask", "xyz is Environments Lobe 0 1 2 Enable, w is Enable VLayering", false),
                    new Property(this, "_DebugLobeMask", "DebugLobeMask", "xyz is Analytical Lobe 0 1 2 Enable", false),
                    new Property(this, "_DebugAniso", "DebugAniso", "x is Hack Enable, y is factor", false),
               }),
            });
        }

        protected override void FpsModePropertiesGUI()
        {

        }

        protected override bool ShouldEmissionBeEnabled(Material material)
        {
            return (material.GetColor(k_EmissiveColor) != Color.black) || material.GetTexture(k_EmissiveColorMap);
        }

        protected override void FindBaseMaterialProperties(MaterialProperty[] props)
        {
            base.FindBaseMaterialProperties(props);
            _baseMaterialProperties.OnFindProperty(props);
        }

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            //base.FindMaterialProperties(props);
            _materialProperties.OnFindProperty(props);
        }

        protected override void BaseMaterialPropertiesGUI()
        {
            base.BaseMaterialPropertiesGUI();
            _baseMaterialProperties.OnGUI();
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            //if (GUILayout.Button("Generate All Properties"))
            //{
            //    Debug.Log(_materialProperties.ToShaderPropertiesStringInternal());
            //}
            
            using (var header = new HeaderScope(StylesStackLit.stackOptionText, (uint)Expendable.Input, this, spaceAtEnd: false))
            {
                if (header.expended)
                    _materialProperties.OnGUI();
            }
        }

        protected override void MaterialPropertiesAdvanceGUI(Material material)
        {
        }

        protected override void VertexAnimationPropertiesGUI()
        {
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        protected static void SetupTextureMaterialProperty(Material material, string basePropertyName)
        {
            // TODO: Caution this can generate a lot of garbage collection call ?
            string useMapPropertyName = basePropertyName + "UseMap";
            string mapPropertyName = basePropertyName + "Map";
            string remapPropertyName = basePropertyName + "MapRemap";
            string invertPropertyName = basePropertyName + "MapRemapInverted";
            string rangePropertyName = basePropertyName + "MapRange";
            string channelPropertyName = basePropertyName + "MapChannel";
            string channelMaskPropertyName = basePropertyName + "MapChannelMask";

            if (material.GetTexture(mapPropertyName))
            {
                if (material.HasProperty(remapPropertyName) && material.HasProperty(rangePropertyName))
                {
                    Vector4 rangeVector = material.GetVector(remapPropertyName);
                    if (material.HasProperty(invertPropertyName) && material.GetFloat(invertPropertyName) > 0.0f)
                    {
                        float s = rangeVector.x;
                        rangeVector.x = rangeVector.y;
                        rangeVector.y = s;
                    }

                    material.SetVector(rangePropertyName, rangeVector);
                }

                if (material.HasProperty(useMapPropertyName))
                {
                    material.SetFloat(useMapPropertyName, 1.0f);
                }

                if (material.HasProperty(channelPropertyName))
                {
                    int channel = (int)material.GetFloat(channelPropertyName);
                    switch (channel)
                    {
                        case 0:
                            material.SetVector(channelMaskPropertyName, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                            break;
                        case 1:
                            material.SetVector(channelMaskPropertyName, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                            break;
                        case 2:
                            material.SetVector(channelMaskPropertyName, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
                            break;
                        case 3:
                            material.SetVector(channelMaskPropertyName, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                            break;
                    }
                }
            }
            else
            {
                if (material.HasProperty(useMapPropertyName))
                {
                    material.SetFloat(useMapPropertyName, 0.0f);
                }
                if (material.HasProperty(rangePropertyName))
                {
                    material.SetVector(rangePropertyName, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                }
                if (material.HasProperty(channelPropertyName))
                {
                    material.SetVector(channelMaskPropertyName, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                }
            }
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        public static void SetupMaterialKeywordsAndPass(Material material)
        {
            SetupBaseUnlitKeywords(material);
            SetupBaseUnlitMaterialPass(material);

            bool doubleSidedEnable = material.GetFloat(kDoubleSidedEnable) > 0.0f;

            if (doubleSidedEnable)
            {
                BaseLitGUI.DoubleSidedNormalMode doubleSidedNormalMode =
                    (BaseLitGUI.DoubleSidedNormalMode)material.GetFloat(k_DoubleSidedNormalMode);
                switch (doubleSidedNormalMode)
                {
                    case BaseLitGUI.DoubleSidedNormalMode.Mirror: // Mirror mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, -1.0f, 0.0f));
                        break;

                    case BaseLitGUI.DoubleSidedNormalMode.Flip: // Flip mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(-1.0f, -1.0f, -1.0f, 0.0f));
                        break;

                    case BaseLitGUI.DoubleSidedNormalMode.None: // None mode (in tangent space)
                        material.SetVector("_DoubleSidedConstants", new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
                        break;
                }
            }

            SetupMainTexForAlphaTestGI("_BaseColorMap", "_BaseColor", material);

            //TODO: disable DBUFFER

            SetupTextureMaterialProperty(material, k_Metallic);
            SetupTextureMaterialProperty(material, k_SmoothnessA);
            SetupTextureMaterialProperty(material, k_SmoothnessB);
            SetupTextureMaterialProperty(material, k_AmbientOcclusion);
            SetupTextureMaterialProperty(material, k_SubsurfaceMask);
            SetupTextureMaterialProperty(material, k_Thickness);
            SetupTextureMaterialProperty(material, k_Anisotropy);
            SetupTextureMaterialProperty(material, k_IridescenceThickness);
            SetupTextureMaterialProperty(material, k_IridescenceMask);
            SetupTextureMaterialProperty(material, k_CoatSmoothness);

            // details
            SetupTextureMaterialProperty(material, k_DetailMask);
            SetupTextureMaterialProperty(material, k_DetailSmoothness);

            // Check if we are using specific UVs.
            TextureProperty.UVMapping[] uvIndices = new[]
            {
                (TextureProperty.UVMapping)material.GetFloat(k_BaseColorMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_MetallicMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_NormalMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_SmoothnessAMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_SmoothnessBMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_AmbientOcclusionMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_EmissiveColorMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_SubsurfaceMaskMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_ThicknessMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_AnisotropyMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_IridescenceThicknessMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_IridescenceMaskMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_CoatSmoothnessMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_CoatNormalMapUV),
                // Details
                (TextureProperty.UVMapping)material.GetFloat(k_DetailMaskMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_DetailSmoothnessMapUV),
                (TextureProperty.UVMapping)material.GetFloat(k_DetailNormalMapUV),
            };

            // Set keyword for mapping

            //bool requireUv2 = false;
            //bool requireUv3 = false;
            bool requireTriplanar = false;
            for (int i = 0; i < uvIndices.Length; ++i)
            {
                //requireUv2 = requireUv2 || uvIndices[i] == TextureProperty.UVMapping.UV2;
                //requireUv3 = requireUv3 || uvIndices[i] == TextureProperty.UVMapping.UV3;
                requireTriplanar = requireTriplanar || uvIndices[i] == TextureProperty.UVMapping.Triplanar;
            }
            CoreUtils.SetKeyword(material, "_USE_TRIPLANAR", requireTriplanar);

            bool detailsEnabled = material.HasProperty(k_EnableDetails) && material.GetFloat(k_EnableDetails) > 0.0f;
            CoreUtils.SetKeyword(material, "_USE_DETAILMAP", detailsEnabled);

            bool dualSpecularLobeEnabled = material.HasProperty(k_EnableDualSpecularLobe) && material.GetFloat(k_EnableDualSpecularLobe) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_DUAL_SPECULAR_LOBE", dualSpecularLobeEnabled);

            bool anisotropyEnabled = material.HasProperty(k_EnableAnisotropy) && material.GetFloat(k_EnableAnisotropy) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_ANISOTROPY", anisotropyEnabled);

            bool iridescenceEnabled = material.HasProperty(k_EnableIridescence) && material.GetFloat(k_EnableIridescence) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_IRIDESCENCE", iridescenceEnabled);

            bool transmissionEnabled = material.HasProperty(k_EnableTransmission) && material.GetFloat(k_EnableTransmission) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", transmissionEnabled);

            bool sssEnabled = material.HasProperty(k_EnableSubsurfaceScattering) && material.GetFloat(k_EnableSubsurfaceScattering) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", sssEnabled);

            bool coatEnabled = material.HasProperty(k_EnableCoat) && material.GetFloat(k_EnableCoat) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_COAT", coatEnabled);

            bool coatNormalMapEnabled = material.HasProperty(k_EnableCoatNormalMap) && material.GetFloat(k_EnableCoatNormalMap) > 0.0f;
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_COAT_NORMALMAP", coatNormalMapEnabled);

            // TEMP - Remove once dev is finish
            bool debugEnabled = material.HasProperty("_DebugEnable") && material.GetFloat("_DebugEnable") > 0.0f;
            CoreUtils.SetKeyword(material, "_STACKLIT_DEBUG", debugEnabled);

            bool vlayerRecomputePerLight = material.HasProperty("_VlayerRecomputePerLight") && material.GetFloat("_VlayerRecomputePerLight") > 0.0f;
            CoreUtils.SetKeyword(material, "_VLAYERED_RECOMPUTE_PERLIGHT", vlayerRecomputePerLight);

            bool vlayerUseRefractedAnglesForBase = material.HasProperty("_VlayerUseRefractedAnglesForBase") && material.GetFloat("_VlayerUseRefractedAnglesForBase") > 0.0f;
            CoreUtils.SetKeyword(material, "_VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE", vlayerUseRefractedAnglesForBase);


            // Set the reference value for the stencil test - required for SSS
            int stencilRef = (int)StencilLightingUsage.RegularLighting;
            if (sssEnabled)
            {
                stencilRef = (int)StencilLightingUsage.SplitLighting;
            }

            // As we tag both during velocity pass and Gbuffer pass we need a separate state and we need to use the write mask
            material.SetInt(kStencilRef, stencilRef);
            material.SetInt(kStencilWriteMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);
            material.SetInt(kStencilRefMV, (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);
            material.SetInt(kStencilWriteMaskMV, (int)HDRenderPipeline.StencilBitMask.ObjectVelocity);

            // for depth only pass to be used in decal to normal buffer compositing
            material.SetInt(kStencilDepthPrepassRef, (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer);
            material.SetInt(kStencilDepthPrepassWriteMask, (int)HDRenderPipeline.StencilBitMask.DecalsForwardOutputNormalBuffer);
        }
    }
} // namespace UnityEditor
