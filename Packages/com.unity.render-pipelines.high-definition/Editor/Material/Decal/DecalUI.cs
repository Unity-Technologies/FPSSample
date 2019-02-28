using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class DecalUI : ExpandableAreaMaterial
    {
        [Flags]
        enum Expandable : uint
        {
            Input = 1 << 0
        }
        protected override uint defaultExpandedState { get { return (uint)Expandable.Input; } }

        protected static class Styles
        {
            public static string InputsText = "Inputs";

            public static GUIContent baseColorText = new GUIContent("BaseColor (RGB) and Opacity (A)", "BaseColor (RGB) and Opacity (A)");
            public static GUIContent baseColorText2 = new GUIContent("Opacity(A)", "Opacity (A)");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (BC7/BC5/DXT5(nm))");
            public static GUIContent decalBlendText = new GUIContent("Global Opacity", "Whole decal Opacity");
            public static GUIContent AlbedoModeText = new GUIContent("Affect BaseColor", "Base color + Opacity, Opacity only");
 			public static GUIContent MeshDecalDepthBiasText = new GUIContent("Mesh decal depth bias", "prevents z-fighting");
	 		public static GUIContent DrawOrderText = new GUIContent("Draw order", "Controls draw order of decal projectors");

            public static GUIContent[] maskMapText =
            {
                new GUIContent("Error", "Mask map"), // Not possible
                new GUIContent("Mask Map - M(R), Opacity(B)", "Mask map - Metal(R), Opacity(B)"), // Decal.MaskBlendFlags.Metal:
                new GUIContent("Mask Map - AO(G), Opacity(B)", "Mask map - Ambient Occlusion(G), Opacity(B)"), // Decal.MaskBlendFlags.AO:
                new GUIContent("Mask Map - M(R), AO(G), Opacity(B)", "Mask map - Metal(R), Ambient Occlusion(G), Opacity(B)"), // Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO:
                new GUIContent("Mask Map - Opacity(B), S(A)", "Mask map - Opacity(B), Smoothness(A)"), // Decal.MaskBlendFlags.Smoothness:
                new GUIContent("Mask Map - M(R), Opacity(B), S(A)", "Mask map - Metal(R), Opacity(B), Smoothness(A)"), // Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.Smoothness:
                new GUIContent("Mask Map - AO(G), Opacity(B), S(A)", "Mask map - Ambient Occlusion(G), Opacity(B), Smoothness(A)"), // Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
                new GUIContent("Mask Map - M(R), AO(G), Opacity(B), S(A)", "Mask map - Metal(R), Ambient Occlusion(G), Opacity(B), Smoothness(A)") // Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
            };
        }

        enum BlendSource
        {
            BaseColorMapAlpha,
            MaskMapBlue
        }
        protected string[] blendSourceNames = Enum.GetNames(typeof(BlendSource));

        protected string[] blendModeNames = Enum.GetNames(typeof(BlendMode));

        protected MaterialProperty baseColorMap = new MaterialProperty();
        protected const string kBaseColorMap = "_BaseColorMap";

        protected MaterialProperty baseColor = new MaterialProperty();
        protected const string kBaseColor = "_BaseColor";

        protected MaterialProperty normalMap = new MaterialProperty();
        protected const string kNormalMap = "_NormalMap";

        protected MaterialProperty maskMap = new MaterialProperty();
        protected const string kMaskMap = "_MaskMap";

        protected MaterialProperty decalBlend = new MaterialProperty();
        protected const string kDecalBlend = "_DecalBlend";

        protected MaterialProperty albedoMode = new MaterialProperty();
        protected const string kAlbedoMode = "_AlbedoMode";

        protected MaterialProperty normalBlendSrc = new MaterialProperty();
        protected const string kNormalBlendSrc = "_NormalBlendSrc";

        protected MaterialProperty maskBlendSrc = new MaterialProperty();
        protected const string kMaskBlendSrc = "_MaskBlendSrc";

        protected MaterialProperty maskBlendMode = new MaterialProperty();
        protected const string kMaskBlendMode = "_MaskBlendMode";

        protected MaterialProperty maskmapMetal = new MaterialProperty();
        protected const string kMaskmapMetal = "_MaskmapMetal";

        protected MaterialProperty maskmapAO = new MaterialProperty();
        protected const string kMaskmapAO = "_MaskmapAO";

        protected MaterialProperty maskmapSmoothness = new MaterialProperty();
        protected const string kMaskmapSmoothness = "_MaskmapSmoothness";

        protected MaterialProperty decalMeshDepthBias = new MaterialProperty();
        protected const string kDecalMeshDepthBias = "_DecalMeshDepthBias";

        protected MaterialProperty drawOrder = new MaterialProperty();
        protected const string kDrawOrder = "_DrawOrder";

        protected const string kDecalStencilWriteMask = "_DecalStencilWriteMask";
        protected const string kDecalStencilRef = "_DecalStencilRef";

        protected MaterialEditor m_MaterialEditor;

        void FindMaterialProperties(MaterialProperty[] props)
        {
            baseColor = FindProperty(kBaseColor, props);
            baseColorMap = FindProperty(kBaseColorMap, props);
            normalMap = FindProperty(kNormalMap, props);
            maskMap = FindProperty(kMaskMap, props);
            decalBlend = FindProperty(kDecalBlend, props);
            albedoMode = FindProperty(kAlbedoMode, props);
            normalBlendSrc = FindProperty(kNormalBlendSrc, props);
            maskBlendSrc = FindProperty(kMaskBlendSrc, props);
            maskBlendMode = FindProperty(kMaskBlendMode, props);
            maskmapMetal = FindProperty(kMaskmapMetal, props);
            maskmapAO = FindProperty(kMaskmapAO, props);
            maskmapSmoothness = FindProperty(kMaskmapSmoothness, props);            
            decalMeshDepthBias = FindProperty(kDecalMeshDepthBias, props);            
            drawOrder = FindProperty(kDrawOrder, props);

            // always instanced
            SerializedProperty instancing = m_MaterialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            Decal.MaskBlendFlags blendMode = (Decal.MaskBlendFlags)material.GetFloat(kMaskBlendMode);

            CoreUtils.SetKeyword(material, "_ALBEDOCONTRIBUTION", material.GetFloat(kAlbedoMode) == 1.0f);
            CoreUtils.SetKeyword(material, "_COLORMAP", material.GetTexture(kBaseColorMap));
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));
            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));

            material.SetInt(kDecalStencilWriteMask, (int)HDRenderPipeline.StencilBitMask.Decals);
            material.SetInt(kDecalStencilRef, (int)HDRenderPipeline.StencilBitMask.Decals);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOSStr, false);
            material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecals3RTStr, true);
            switch (blendMode)
            {
                case Decal.MaskBlendFlags.Metal:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMStr, true);
                    break;

                case Decal.MaskBlendFlags.AO:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOStr, true);
                    break;

                case Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsSStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMSStr, true);
                    break;

                case Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsAOSStr, true);
                    break;

                case Decal.MaskBlendFlags.Metal | Decal.MaskBlendFlags.AO | Decal.MaskBlendFlags.Smoothness:
                    material.SetShaderPassEnabled(HDShaderPassNames.s_MeshDecalsMAOSStr, true);
                    break;
            }
        }

        protected void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;
            float normalBlendSrcValue = normalBlendSrc.floatValue;
            float maskBlendSrcValue =  maskBlendSrc.floatValue;

            Decal.MaskBlendFlags maskBlendFlags = (Decal.MaskBlendFlags)maskBlendMode.floatValue;              

            HDRenderPipelineAsset hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            bool perChannelMask = hdrp.renderPipelineSettings.decalSettings.perChannelMask;

            using (var header = new HeaderScope(Styles.InputsText, (uint)Expandable.Input, this))
            {
                if (header.expanded)
                {
                    // Detect any changes to the material
                    EditorGUI.BeginChangeCheck();
                    {
                        m_MaterialEditor.TexturePropertySingleLine((material.GetFloat(kAlbedoMode) == 1.0f) ? Styles.baseColorText : Styles.baseColorText2, baseColorMap, baseColor);
                        // Currently always display Albedo contribution as we have an albedo tint that apply
                        EditorGUI.indentLevel++;
                        m_MaterialEditor.ShaderProperty(albedoMode, Styles.AlbedoModeText);
                        EditorGUI.indentLevel--;

                        m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap);
                        if (material.GetTexture(kNormalMap))
                        {
                            EditorGUI.indentLevel++;
                            normalBlendSrcValue = EditorGUILayout.Popup("Normal Opacity channel", (int)normalBlendSrcValue, blendSourceNames);
                            EditorGUI.indentLevel--;
                        }

                        m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapText[(int)maskBlendFlags], maskMap);
                        if (material.GetTexture(kMaskMap))
                        {
                            EditorGUI.indentLevel++;
                            maskBlendSrcValue = EditorGUILayout.Popup("Mask Opacity channel", (int)maskBlendSrcValue, blendSourceNames);
                            if (perChannelMask)
                            {
                                // Following condition force users to always have at least one attribute enabled
                                m_MaterialEditor.ShaderProperty(maskmapMetal, "Affect Metal");
                                if ((maskmapMetal.floatValue == 0.0f) && (maskmapAO.floatValue == 0.0f) && (maskmapSmoothness.floatValue == 0.0f))
                                    maskmapMetal.floatValue = 1.0f;
                                m_MaterialEditor.ShaderProperty(maskmapAO, "Affect AO");
                                if ((maskmapMetal.floatValue == 0.0f) && (maskmapAO.floatValue == 0.0f) && (maskmapSmoothness.floatValue == 0.0f))
                                    maskmapAO.floatValue = 1.0f;
                                m_MaterialEditor.ShaderProperty(maskmapSmoothness, "Affect Smoothness");
                                if ((maskmapMetal.floatValue == 0.0f) && (maskmapAO.floatValue == 0.0f) && (maskmapSmoothness.floatValue == 0.0f))
                                    maskmapSmoothness.floatValue = 1.0f;

                                maskBlendFlags = 0; // Re-init the mask

                                if (maskmapMetal.floatValue == 1.0f)
                                    maskBlendFlags |= Decal.MaskBlendFlags.Metal;
                                if (maskmapAO.floatValue == 1.0f)
                                    maskBlendFlags |= Decal.MaskBlendFlags.AO;
                                if (maskmapSmoothness.floatValue == 1.0f)
                                    maskBlendFlags |= Decal.MaskBlendFlags.Smoothness;
                            }
                            else // if perChannelMask is not enabled, force to have smoothness
                            {
                                maskBlendFlags = Decal.MaskBlendFlags.Smoothness;
                            }
                            EditorGUI.indentLevel--;
                        }

                        m_MaterialEditor.ShaderProperty(drawOrder, Styles.DrawOrderText);
                        m_MaterialEditor.ShaderProperty(decalMeshDepthBias, Styles.MeshDecalDepthBiasText);
                        m_MaterialEditor.ShaderProperty(decalBlend, Styles.decalBlendText);

                        EditorGUI.indentLevel--;

                        EditorGUILayout.HelpBox(
                            "Control of AO and Metal is based on option 'Enable Metal and AO properties' in HDRP Asset.\nThere is a performance cost of enabling this option.",
                            MessageType.Info);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        normalBlendSrc.floatValue = normalBlendSrcValue;
                        maskBlendSrc.floatValue = maskBlendSrcValue;
                        maskBlendMode.floatValue = (float)maskBlendFlags;
                        foreach (var obj in m_MaterialEditor.targets)
                            SetupMaterialKeywordsAndPassInternal((Material)obj);
                    }
                }
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            m_MaterialEditor = materialEditor;

            // We should always register the key used to keep collapsable state
            InitExpandableState(materialEditor);

            // We should always do this call at the beginning
            m_MaterialEditor.serializedObject.Update();
            
            FindMaterialProperties(props);

            Material material = materialEditor.target as Material;
            ShaderPropertiesGUI(material);

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }
    }
}
