using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class FabricGUI : BaseLitGUI
    {
        static Expendable state = Expendable.Base | Expendable.Input | Expendable.VertexAnimation | Expendable.Detail | Expendable.Emissive | Expendable.Transparency | Expendable.Other;
        protected override uint expendedState { get { return (uint)state; } set { state = (Expendable)value; } }

        protected static class Styles
        {
            // Fields
            public static GUIContent fabricTypeText = new GUIContent("Fabric Type", "");
            public static string InputsText = "Inputs";
            public static string emissiveLabelText = "Emissive Inputs";
            public static string fabricLabelText = "Fabric Options";

            // Primary UV mapping
            public static GUIContent UVBaseMappingText = new GUIContent("Base UV mapping", "");

            // Base Color
            public static GUIContent baseColorText = new GUIContent("Base Color + Opacity", "Albedo (RGB) and Opacity (A)");

            // Specular Color
            public static GUIContent specularColorText = new GUIContent("Specular Color", "");

            // Smoothness
            public static GUIContent smoothnessMapChannelText = new GUIContent("Smoothness Source", "Smoothness texture and channel");
            public static GUIContent smoothnessText = new GUIContent("Smoothness", "Smoothness scale factor");
            public static GUIContent smoothnessRemappingText = new GUIContent("Smoothness Remapping", "Smoothness remapping");
            
            // AO
            public static GUIContent aoRemappingText = new GUIContent("AmbientOcclusion Remapping", "AmbientOcclusion remapping");
            
            // Mask
            public static GUIContent maskMapSText = new GUIContent("Mask Map - X, AO(G), DM(B), S(A)", "Mask map");
            public static GUIContent maskMapSpecularText = new GUIContent("Mask Map - AO(G), DM(B), S(A)", "Mask map");

            // Normal map
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (BC7/BC5/DXT5(nm))");
            // public static GUIContent bentNormalMapText = new GUIContent("Bent normal map", "Use only with indirect diffuse lighting (Lightmap/light-probe) - Cosine weighted Bent Normal Map (average un-occluded direction) (BC7/BC5/DXT5(nm))");

            // Tangent map
            public static GUIContent tangentMapText = new GUIContent("Tangent Map", "Tangent Map (BC7/BC5/DXT5(nm))");
           
            // Anisotropy
            public static GUIContent anisotropyText = new GUIContent("Anisotropy", "Anisotropy scale factor");
            public static GUIContent anisotropyMapText = new GUIContent("Anisotropy Map (R)", "Anisotropy");

            // Thread map
            public static string threadText = "Thread Inputs";
            public static GUIContent UVThreadMappingText = new GUIContent("Thread UV mapping", "");
            public static GUIContent threadMapText = new GUIContent("Thread Map AO(R) Ny(G) S(B) Nx(A)", "Thread Map");
            public static GUIContent threadAOScaleText = new GUIContent("Thread AO", "Thread AO Scale factor");
            public static GUIContent threadNormalScaleText = new GUIContent("Thread NormalScale", "Normal Scale factor");
            public static GUIContent threadSmoothnessScaleText = new GUIContent("Thread SmoothnessScale", "Smoothness Scale factor");
            public static GUIContent linkDetailsWithBaseText = new GUIContent("Lock to Base Tiling/Offset", "Lock details Tiling/Offset to Base Tiling/Offset");

            // Fuzz detail
            public static GUIContent FuzzDetailText = new GUIContent("Fuzz Detail", "Fuzz Detail factor, it affects the base color of the fabric.");
            public static GUIContent FuzzDetailScale = new GUIContent("Fuzz Detail Scale", "Fuzz Detail scale");
            public static GUIContent FuzzDetailUVScale = new GUIContent("Fuzz Detail UV Scale", "Fuzz Detail uv scale");

            // Diffusion
            public static GUIContent diffusionProfileText = new GUIContent("Diffusion profile", "A profile determines the shape of the SSS/transmission filter.");

            // Transmission
            public static GUIContent transmissionToggleText = new GUIContent("Transmission Enabled", "Enable/Disable the transmission");

            // Subsurface scattering
            public static GUIContent subsurfaceToggleText = new GUIContent("Subsurface Enabled", "Enable/Disable the subsurface");
            public static GUIContent subsurfaceMaskText = new GUIContent("Subsurface mask", "Determines the strength of the subsurface scattering effect.");
            public static GUIContent subsurfaceMaskMapText = new GUIContent("Subsurface mask map (R)", "Determines the strength of the subsurface scattering effect.");

            // Thickness
            public static GUIContent thicknessText = new GUIContent("Thickness", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.");
            public static GUIContent thicknessMapText = new GUIContent("Thickness map (R)", "If subsurface scattering is enabled, low values allow some light to be transmitted through the object.");
            public static GUIContent thicknessRemapText = new GUIContent("Thickness Remap", "Remaps values of the thickness map from [0, 1] to the specified range.");

            // Emissive
            public static GUIContent UVMappingEmissiveText = new GUIContent("Emissive UV mapping", "");
            public static GUIContent emissiveText = new GUIContent("Emissive Map + Color", "Emissive Map + Color (linear RGB) in nits unit");
            public static GUIContent albedoAffectEmissiveText = new GUIContent("Albedo Affect Emissive", "Specifies whether or not the emissive color is multiplied by the albedo.");

            // Specular occlusion
            // public static GUIContent enableSpecularOcclusionText = new GUIContent("Enable Specular Occlusion from Bent normal", "Require cosine weighted bent normal and cosine weighted ambient occlusion. Specular occlusion for reflection probe");
            // public static GUIContent specularOcclusionWarning = new GUIContent("Require a cosine weighted bent normal and ambient occlusion maps");
        }

        // Fabric Type
        protected MaterialProperty fabricType = null;
        protected const string kFabricType = "_FabricType";

        // Base UV set & mask
        protected MaterialProperty UVBase = null;
        protected const string kUVBase = "_UVBase";
        protected MaterialProperty UVMappingMask = null;
        protected const string kUVMappingMask = "_UVMappingMask";

        // Base color
        protected MaterialProperty baseColor = null;
        protected const string kBaseColor = "_BaseColor";

        // Base color map
        protected MaterialProperty baseColorMap = null;
        protected const string kBaseColorMap = "_BaseColorMap";
        protected MaterialProperty smoothness = null;

        // Smoothness
        protected const string kSmoothness = "_Smoothness";

        // Mask map
        protected MaterialProperty maskMap = null;
        protected const string kMaskMap = "_MaskMap";

        // Smoothness remapping
        protected MaterialProperty smoothnessRemapMin = null;
        protected const string kSmoothnessRemapMin = "_SmoothnessRemapMin";
        protected MaterialProperty smoothnessRemapMax = null;
        protected const string kSmoothnessRemapMax = "_SmoothnessRemapMax";

        // AO remapping
        protected MaterialProperty aoRemapMin = null;
        protected const string kAORemapMin = "_AORemapMin";
        protected MaterialProperty aoRemapMax = null;
        protected const string kAORemapMax = "_AORemapMax";

        // Normal Scale & Map
        protected MaterialProperty normalScale = null;
        protected const string kNormalScale = "_NormalScale";
        protected MaterialProperty normalMap = null;
        protected const string kNormalMap = "_NormalMap";
        // protected MaterialProperty bentNormalMap = null;
        // protected const string kBentNormalMap = "_BentNormalMap";

        // Tangent Map
        protected MaterialProperty tangentMap = null;
        protected const string kTangentMap = "_TangentMap";

        // Specular Color
        protected MaterialProperty specularColor = null;
        protected const string kSpecularColor = "_SpecularColor";

        // Diffusion profile
        protected MaterialProperty diffusionProfileID = null;
        protected const string kDiffusionProfileID = "_DiffusionProfile";

        // Transmission
        protected MaterialProperty enableTransmission = null;
        protected const string kEnableTransmission = "_EnableTransmission";

        // Subsurface scattering
        protected MaterialProperty enableSubsurfaceScattering = null;
        protected const string kEnableSubsurfaceScattering = "_EnableSubsurfaceScattering";
        protected MaterialProperty subsurfaceMask = null;
        protected const string kSubsurfaceMask = "_SubsurfaceMask";
        protected MaterialProperty subsurfaceMaskMap = null;
        protected const string kSubsurfaceMaskMap = "_SubsurfaceMaskMap";

        // Thickness
        protected MaterialProperty thickness = null;
        protected const string kThickness = "_Thickness";
        protected MaterialProperty thicknessMap = null;
        protected const string kThicknessMap = "_ThicknessMap";
        protected MaterialProperty thicknessRemap = null;
        protected const string kThicknessRemap = "_ThicknessRemap";

        // UV Thread Set & Mask
        protected MaterialProperty UVThread = null;
        protected const string kUVThread = "_UVThread";
        protected MaterialProperty UVMappingMaskThread = null;
        protected const string kUVMappingMaskThread = "_UVMappingMaskThread";
        
        // Thread Map
        protected MaterialProperty threadMap = null;
        protected const string kThreadMap = "_ThreadMap";

        // Thread adjusting
        protected MaterialProperty threadAOScale = null;
        protected const string kThreadAOScale = "_ThreadAOScale";
        protected MaterialProperty threadNormalScale = null;
        protected const string kThreadNormalScale = "_ThreadNormalScale";
        protected MaterialProperty threadSmoothnessScale = null;
        protected const string kThreadSmoothnessScale = "_ThreadSmoothnessScale";

        // Fuzz Detail
        protected MaterialProperty fuzzDetailMap = null;
        protected const string kFuzzDetailMap = "_FuzzDetailMap";
        protected MaterialProperty fuzzDetailScale = null;
        protected const string kFuzzDetailScale = "_FuzzDetailScale";
        protected MaterialProperty fuzzDetailUVScale = null;
        protected const string kFuzzDetailUVScale = "_FuzzDetailUVScale";

        // Link detail with base
        protected MaterialProperty linkDetailsWithBase = null;
        protected const string kLinkDetailsWithBase = "_LinkDetailsWithBase";     

        // protected MaterialProperty tangentMap = null;
        // protected const string kTangentMap = "_TangentMap";
        protected MaterialProperty anisotropy = null;
        protected const string kAnisotropy = "_Anisotropy";
        protected MaterialProperty anisotropyMap = null;
        protected const string kAnisotropyMap = "_AnisotropyMap";

        // UV Emissive Set & Mask
        protected MaterialProperty UVEmissive = null;
        protected const string kUVEmissive = "_UVEmissive";
        protected MaterialProperty UVMappingMaskEmissive = null;
        protected const string kUVMappingMaskEmissive = "_UVMappingMaskEmissive";

        // Emissive
        protected MaterialProperty emissiveColor = null;
        protected const string kEmissiveColor = "_EmissiveColor";
        protected MaterialProperty emissiveColorMap = null;
        protected const string kEmissiveColorMap = "_EmissiveColorMap";

        // protected MaterialProperty enableSpecularOcclusion = null;
        // protected const string kEnableSpecularOcclusion = "_EnableSpecularOcclusion";

        protected bool fabricOptionExpended = true;

        override protected void FindMaterialProperties(MaterialProperty[] props)
        {
            // Fabric Type
            fabricType = FindProperty(kFabricType, props);           

            // Base UV set & mask
            UVBase = FindProperty(kUVBase, props);
            UVMappingMask = FindProperty(kUVMappingMask, props);

            // Base Color & Map
            baseColor = FindProperty(kBaseColor, props);
            baseColorMap = FindProperty(kBaseColorMap, props);

            // Smoothness
            smoothness = FindProperty(kSmoothness, props);

            // Mask and remapping values
            maskMap = FindProperty(kMaskMap, props);
            smoothnessRemapMin = FindProperty(kSmoothnessRemapMin, props);
            smoothnessRemapMax = FindProperty(kSmoothnessRemapMax, props);
            aoRemapMin = FindProperty(kAORemapMin, props);
            aoRemapMax = FindProperty(kAORemapMax, props);

            // Normal map and scale
            normalMap = FindProperty(kNormalMap, props);
            normalScale = FindProperty(kNormalScale, props);
            // bentNormalMap = FindProperty(kBentNormalMap, props);

            // Tangent map
            tangentMap = FindProperty(kTangentMap, props);

            // Specular Color
            specularColor = FindProperty(kSpecularColor, props);

            // Diffusion profile
            diffusionProfileID = FindProperty(kDiffusionProfileID, props);

            // Transmission
            enableTransmission = FindProperty(kEnableTransmission, props);

            // Sub surface
            enableSubsurfaceScattering = FindProperty(kEnableSubsurfaceScattering, props);
            subsurfaceMask = FindProperty(kSubsurfaceMask, props);
            subsurfaceMaskMap = FindProperty(kSubsurfaceMaskMap, props);

            // Thickness
            thickness = FindProperty(kThickness, props);
            thicknessMap = FindProperty(kThicknessMap, props);
            thicknessRemap = FindProperty(kThicknessRemap, props);

            // Details Set and Mask
            UVThread = FindProperty(kUVThread, props);
            UVMappingMaskThread = FindProperty(kUVMappingMaskThread, props);
            
            // Thread map and remapping
            threadMap = FindProperty(kThreadMap, props);
            threadAOScale = FindProperty(kThreadAOScale, props);
            threadNormalScale = FindProperty(kThreadNormalScale, props);
            threadSmoothnessScale = FindProperty(kThreadSmoothnessScale, props);
            linkDetailsWithBase = FindProperty(kLinkDetailsWithBase, props);

            // Fuzz Detail
            fuzzDetailMap = FindProperty(kFuzzDetailMap, props);
            fuzzDetailScale = FindProperty(kFuzzDetailScale, props);
            fuzzDetailUVScale = FindProperty(kFuzzDetailUVScale, props);

            // Anisotropy
            // tangentMap = FindProperty(kTangentMap, props);
            anisotropy = FindProperty(kAnisotropy, props);
            anisotropyMap = FindProperty(kAnisotropyMap, props);

            // UV Emissive set & Mask
            UVEmissive = FindProperty(kUVEmissive, props);
            UVMappingMaskEmissive = FindProperty(kUVMappingMaskEmissive, props);

            // Emissive Data
            emissiveColor = FindProperty(kEmissiveColor, props);
            emissiveColorMap = FindProperty(kEmissiveColorMap, props);

            // Specular occlusion
            // enableSpecularOcclusion = FindProperty(kEnableSpecularOcclusion, props);
        }

        public enum FabricType
        {
            Silk,
            CottonWool,
        }

        public enum UVBaseMapping
        {
            UV0,
            UV1,
            UV2,
            UV3
        }

        public enum UVThreadMapping
        {
            UV0,
            UV1,
            UV2,
            UV3
        }

        public enum UVEmissiveMapping
        {
            UV0,
            UV1,
            UV2,
            UV3
        }

        protected void BaseUVMappingInputGUI()
        {
            m_MaterialEditor.ShaderProperty(UVBase, Styles.UVBaseMappingText);

            UVBaseMapping uvBaseMapping = (UVBaseMapping)UVBase.floatValue;

            float X, Y, Z, W;
            X = (uvBaseMapping == UVBaseMapping.UV0) ? 1.0f : 0.0f;
            Y = (uvBaseMapping == UVBaseMapping.UV1) ? 1.0f : 0.0f;
            Z = (uvBaseMapping == UVBaseMapping.UV2) ? 1.0f : 0.0f;
            W = (uvBaseMapping == UVBaseMapping.UV3) ? 1.0f : 0.0f;

            UVMappingMask.colorValue = new Color(X, Y, Z, W);

            m_MaterialEditor.TextureScaleOffsetProperty(baseColorMap);
        }

        protected void BaseInputGUI(Material material)
        {
            using (var header = new HeaderScope(Styles.InputsText, (uint)Expendable.Input, this))
            {
                if (header.expended)
                {
                    // The base color map and matching base color value
                    m_MaterialEditor.TexturePropertySingleLine(Styles.baseColorText, baseColorMap, baseColor);

                    // If no mask texture was provided, we display the smoothness value
                    if (maskMap.textureValue == null)
                    {
                        m_MaterialEditor.ShaderProperty(smoothness, Styles.smoothnessText);
                    }

                    // If we have a mask map, we do not use values but remapping fields instead
                    m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapSpecularText, maskMap);
                    if (maskMap.textureValue != null)
                    {
                        float remapMin = smoothnessRemapMin.floatValue;
                        float remapMax = smoothnessRemapMax.floatValue;
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.MinMaxSlider(Styles.smoothnessRemappingText, ref remapMin, ref remapMax, 0.0f, 1.0f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            smoothnessRemapMin.floatValue = remapMin;
                            smoothnessRemapMax.floatValue = remapMax;
                        }

                        float aoMin = aoRemapMin.floatValue;
                        float aoMax = aoRemapMax.floatValue;
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.MinMaxSlider(Styles.aoRemappingText, ref aoMin, ref aoMax, 0.0f, 1.0f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            aoRemapMin.floatValue = aoMin;
                            aoRemapMax.floatValue = aoMax;
                        }
                    }

                    // The specular color value (that affects the color of the specular lighting term)
                    m_MaterialEditor.ShaderProperty(specularColor, Styles.specularColorText);
                    // The primal normal map field
                    m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap, normalScale);

                    // m_MaterialEditor.TexturePropertySingleLine(Styles.bentNormalMapText, bentNormalMap);

                    // The diffusion/transmission/subsurface gui
                    ShaderSSSAndTransmissionInputGUI(material);

                    // Anisotropy GUI
                    ShaderAnisoInputGUI(material);

                    // Define the UV mapping for the base textures
                    BaseUVMappingInputGUI();

                }
            }
        }

        protected void DetailsInput(Material material)
        {
            using (var header = new HeaderScope(Styles.threadText, (uint)Expendable.Detail, this))
            {
                if (header.expended)
                {
                    m_MaterialEditor.TexturePropertySingleLine(Styles.threadMapText, threadMap);
                    if (material.GetTexture(kThreadMap))
                    {
                        EditorGUI.indentLevel++;
                        m_MaterialEditor.ShaderProperty(threadAOScale, Styles.threadAOScaleText);
                        m_MaterialEditor.ShaderProperty(threadNormalScale, Styles.threadNormalScaleText);
                        m_MaterialEditor.ShaderProperty(threadSmoothnessScale, Styles.threadSmoothnessScaleText);
                        EditorGUI.indentLevel--;
                    }

                    m_MaterialEditor.TexturePropertySingleLine(Styles.FuzzDetailText, fuzzDetailMap);
                    if (material.GetTexture(kFuzzDetailMap))
                    {
                        m_MaterialEditor.ShaderProperty(fuzzDetailScale, Styles.FuzzDetailScale);
                        m_MaterialEditor.ShaderProperty(fuzzDetailUVScale, Styles.FuzzDetailUVScale);
                    }

                    if (material.GetTexture(kThreadMap) || material.GetTexture(kFuzzDetailMap))
                    {
                        EditorGUI.indentLevel++;

                        m_MaterialEditor.ShaderProperty(UVThread, Styles.UVThreadMappingText);

                        // Setup the UVSet for detail, if planar/triplanar is use for base, it will override the mapping of detail (See shader code)
                        float X, Y, Z, W;
                        X = ((UVThreadMapping)UVThread.floatValue == UVThreadMapping.UV0) ? 1.0f : 0.0f;
                        Y = ((UVThreadMapping)UVThread.floatValue == UVThreadMapping.UV1) ? 1.0f : 0.0f;
                        Z = ((UVThreadMapping)UVThread.floatValue == UVThreadMapping.UV2) ? 1.0f : 0.0f;
                        W = ((UVThreadMapping)UVThread.floatValue == UVThreadMapping.UV3) ? 1.0f : 0.0f;
                        UVMappingMaskThread.colorValue = new Color(X, Y, Z, W);

                        EditorGUI.indentLevel++;
                        m_MaterialEditor.ShaderProperty(linkDetailsWithBase, Styles.linkDetailsWithBaseText);
                        EditorGUI.indentLevel--;
                        m_MaterialEditor.TextureScaleOffsetProperty(threadMap);
                    }
                }
            }
        }

        protected void ShaderSSSAndTransmissionInputGUI(Material material)
        {
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

            if (hdPipeline == null)
                return;

            var diffusionProfileSettings = hdPipeline.diffusionProfileSettings;

            if (hdPipeline.IsInternalDiffusionProfile(diffusionProfileSettings))
            {
                EditorGUILayout.HelpBox("No diffusion profile Settings have been assigned to the render pipeline asset.", MessageType.Warning);
                return;
            }

         
            // Enable transmission toggle
            m_MaterialEditor.ShaderProperty(enableTransmission, Styles.transmissionToggleText);

            // Subsurface toggle and options
            m_MaterialEditor.ShaderProperty(enableSubsurfaceScattering, Styles.subsurfaceToggleText);
            if (enableSubsurfaceScattering.floatValue == 1.0f)
            {
                m_MaterialEditor.ShaderProperty(subsurfaceMask, Styles.subsurfaceMaskText);
                m_MaterialEditor.TexturePropertySingleLine(Styles.subsurfaceMaskMapText, subsurfaceMaskMap);
            }

            // The thickness sub-menu is toggled if either the transmission or subsurface are requested
            if (enableSubsurfaceScattering.floatValue == 1.0f || enableTransmission.floatValue == 1.0f)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.thicknessMapText, thicknessMap);
                if (thicknessMap.textureValue != null)
                {
                    // Display the remap of texture values.
                    Vector2 remap = thicknessRemap.vectorValue;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.MinMaxSlider(Styles.thicknessRemapText, ref remap.x, ref remap.y, 0.0f, 1.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        thicknessRemap.vectorValue = remap;
                    }
                }
                else
                {
                    // Allow the user to set the constant value of thickness if no thickness map is provided.
                    m_MaterialEditor.ShaderProperty(thickness, Styles.thicknessText);
                }
            }

            // We only need to display the diffusion profile if we have either transmission or diffusion
            // TODO: Optimize me
            if (enableSubsurfaceScattering.floatValue == 1.0f || enableTransmission.floatValue == 1.0f)
            {
                var profiles = diffusionProfileSettings.profiles;
                var names = new GUIContent[profiles.Length + 1];
                names[0] = new GUIContent("None");

                var values = new int[names.Length];
                values[0] = DiffusionProfileConstants.DIFFUSION_PROFILE_NEUTRAL_ID;

                for (int i = 0; i < profiles.Length; i++)
                {
                    names[i + 1] = new GUIContent(profiles[i].name);
                    values[i + 1] = i + 1;
                }

                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    int profileID = (int)diffusionProfileID.floatValue;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel(Styles.diffusionProfileText);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            profileID = EditorGUILayout.IntPopup(profileID, names, values);

                            if (GUILayout.Button("Goto", EditorStyles.miniButton, GUILayout.Width(50f)))
                                Selection.activeObject = diffusionProfileSettings;
                        }
                    }

                    if (scope.changed)
                        diffusionProfileID.floatValue = profileID;
                }
            }
        }

        protected void EmissiveInputGUI(Material material)
        {
            using (var header = new HeaderScope(Styles.emissiveLabelText, (uint)Expendable.Emissive, this))
            {
                if (header.expended)
                {
                    m_MaterialEditor.TexturePropertySingleLine(Styles.emissiveText, emissiveColorMap, emissiveColor);

                    if (material.GetTexture(kEmissiveColorMap))
                    {
                        EditorGUI.indentLevel++;
                        m_MaterialEditor.ShaderProperty(UVEmissive, Styles.UVMappingEmissiveText);
                        UVBaseMapping uvEmissiveMapping = (UVBaseMapping)UVEmissive.floatValue;

                        float X, Y, Z, W;
                        X = (uvEmissiveMapping == UVBaseMapping.UV0) ? 1.0f : 0.0f;
                        Y = (uvEmissiveMapping == UVBaseMapping.UV1) ? 1.0f : 0.0f;
                        Z = (uvEmissiveMapping == UVBaseMapping.UV2) ? 1.0f : 0.0f;
                        W = (uvEmissiveMapping == UVBaseMapping.UV3) ? 1.0f : 0.0f;

                        UVMappingMaskEmissive.colorValue = new Color(X, Y, Z, W);


                        m_MaterialEditor.TextureScaleOffsetProperty(emissiveColorMap);
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        protected void ShaderAnisoInputGUI(Material material)
        {
            // We only have anisotropy for the silk fabric
            FabricType fabricType = (FabricType)material.GetFloat(kFabricType);
            if(fabricType == FabricType.Silk)
            {
                m_MaterialEditor.TexturePropertySingleLine(Styles.tangentMapText, tangentMap);
                m_MaterialEditor.ShaderProperty(anisotropy, Styles.anisotropyText);
                m_MaterialEditor.TexturePropertySingleLine(Styles.anisotropyMapText, anisotropyMap);
            }
        }

        protected override void MaterialPropertiesGUI(Material material)
        {
            using (var header = new HeaderScope(Styles.fabricLabelText, (uint)Expendable.Other, this))
            {
                if (header.expended)
                {
                    // The generic type of the fabric (either cotton/wool or silk)
                    m_MaterialEditor.ShaderProperty(fabricType, Styles.fabricTypeText);
                }
            }

            // Base GUI
            BaseInputGUI(material);

            // Emissive GUI
            EmissiveInputGUI(material);

            // Details Input
            DetailsInput(material);
        }

        protected override void VertexAnimationPropertiesGUI()
        {

        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            SetupBaseLitKeywords(material);
            SetupBaseLitMaterialPass(material);

            // We need to override the behavior of the baselitui given that we do not use the LitSSS material ID to trigger the stencil
            int stencilRef = (int)StencilLightingUsage.RegularLighting;
            if (material.GetFloat(kEnableSubsurfaceScattering) > 0.0f)
            {
                stencilRef = (int)StencilLightingUsage.SplitLighting;
            }
            material.SetInt(kStencilRef, stencilRef);
            
            // With thread map, we always use a normal map and Unity provide a default (0, 0, 1) normal map for it
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));

            // However, the tangent map flag is only bound to the presence of a tangent map
            // CoreUtils.SetKeyword(material, "_TANGENTMAP", material.GetTexture(kTangentMap));

            // For the moment, we do not support the bent normal map
            // CoreUtils.SetKeyword(material, "_BENTNORMALMAP", material.GetTexture(kBentNormalMap));

            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));

            // We do not support specular occlusion for the moment
            // CoreUtils.SetKeyword(material, "_ENABLESPECULAROCCLUSION", material.GetFloat(kEnableSpecularOcclusion) > 0.0f);

            CoreUtils.SetKeyword(material, "_ANISOTROPYMAP", material.GetTexture(kAnisotropyMap));
            CoreUtils.SetKeyword(material, "_THREAD_MAP", material.GetTexture(kThreadMap));
            CoreUtils.SetKeyword(material, "_FUZZDETAIL_MAP", material.GetTexture(kFuzzDetailMap));
            CoreUtils.SetKeyword(material, "_SUBSURFACE_MASK_MAP", material.GetTexture(kSubsurfaceMaskMap));
            CoreUtils.SetKeyword(material, "_THICKNESSMAP", material.GetTexture(kThicknessMap));
            CoreUtils.SetKeyword(material, "_EMISSIVE_COLOR_MAP", material.GetTexture(kEmissiveColorMap)); 

            // Require and set 
            bool needUV2 = (UVThreadMapping)material.GetFloat(kUVThread) == UVThreadMapping.UV2 || (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV2 || (UVBaseMapping)material.GetFloat(kUVEmissive) == UVBaseMapping.UV2;
            bool needUV3 = (UVThreadMapping)material.GetFloat(kUVThread) == UVThreadMapping.UV3 || (UVBaseMapping)material.GetFloat(kUVBase) == UVBaseMapping.UV3 || (UVBaseMapping)material.GetFloat(kUVEmissive) == UVBaseMapping.UV2;

            if (needUV3)
            {
                material.DisableKeyword("_REQUIRE_UV2");
                material.EnableKeyword("_REQUIRE_UV3");
            }
            else if (needUV2)
            {
                material.EnableKeyword("_REQUIRE_UV2");
                material.DisableKeyword("_REQUIRE_UV3");
            }
            else
            {
                material.DisableKeyword("_REQUIRE_UV2");
                material.DisableKeyword("_REQUIRE_UV3");
            }

            // Fetch the fabric type
            FabricType fabricType = (FabricType)material.GetFloat(kFabricType);

            // If the material is of type cotton/wool we inject it! Otherwise it is necessarily of silk/anisotropy type (we don't inject it to save keywords)
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_COTTON_WOOL", fabricType == FabricType.CottonWool);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", material.GetFloat(kEnableSubsurfaceScattering) > 0.0f);
            CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", material.GetFloat(kEnableTransmission) > 0.0f);

            
        }
    }
} // namespace UnityEditor
