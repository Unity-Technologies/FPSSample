using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

using System.Linq;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class TerrainLitGUI : LitGUI, ITerrainLayerCustomUI
    {
        protected override uint defaultExpandedState { get { return 0u; } }

        private class StylesLayer
        {
            public readonly GUIContent enableHeightBlend = new GUIContent("Enable Height-based Blend", "Blend terrain layers based on height values.");
            public readonly GUIContent heightTransition = new GUIContent("Height Transition", "Size in world units of the smooth transition between layers.");
            public readonly GUIContent enableInstancedPerPixelNormal = new GUIContent("Enable Per-pixel Normal", "Enable per-pixel normal when the terrain uses instanced rendering.");

            public readonly GUIContent diffuseTexture = new GUIContent("Diffuse");
            public readonly GUIContent colorTint = new GUIContent("Color Tint");
            public readonly GUIContent opacityAsDensity = new GUIContent("Opacity as Density", "Enable Density Blend");
            public readonly GUIContent normalMapTexture = new GUIContent("Normal Map");
            public readonly GUIContent normalScale = new GUIContent("Normal Scale");
            public readonly GUIContent maskMapTexture = new GUIContent("Mask", "R: Metallic\nG: AO\nB: Height\nA: Smoothness");
            public readonly GUIContent maskMapTextureWithoutHeight = new GUIContent("Mask Map", "R: Metallic\nG: AO\nA: Smoothness");
            public readonly GUIContent channelRemapping = new GUIContent("Channel Remapping");
            public readonly GUIContent defaultValues = new GUIContent("Channel Default Values");
            public readonly GUIContent metallic = new GUIContent("R: Metallic");
            public readonly GUIContent ao = new GUIContent("G: AO");
            public readonly GUIContent height = new GUIContent("B: Height");
            public readonly GUIContent heightParametrization = new GUIContent("Parametrization");
            public readonly GUIContent heightAmplitude = new GUIContent("Amplitude (cm)");
            public readonly GUIContent heightBase = new GUIContent("Base");
            public readonly GUIContent heightMin = new GUIContent("Min (cm)");
            public readonly GUIContent heightMax = new GUIContent("Max (cm)");
            public readonly GUIContent heightCm = new GUIContent("B: Height (cm)");
            public readonly GUIContent smoothness = new GUIContent("A: Smoothness");
        }

        static StylesLayer s_Styles = null;
        private static StylesLayer styles { get { if (s_Styles == null) s_Styles = new StylesLayer(); return s_Styles; } }

        public TerrainLitGUI()
        {
        }

        MaterialProperty enableHeightBlend;
        const string kEnableHeightBlend = "_EnableHeightBlend";

        // Height blend
        MaterialProperty heightTransition = null;
        const string kHeightTransition = "_HeightTransition";

        MaterialProperty enableInstancedPerPixelNormal = null;
        const string kEnableInstancedPerPixelNormal = "_EnableInstancedPerPixelNormal";

        protected override void FindMaterialProperties(MaterialProperty[] props)
        {
            enableHeightBlend = FindProperty(kEnableHeightBlend, props, false);
            heightTransition = FindProperty(kHeightTransition, props, false);
            enableInstancedPerPixelNormal = FindProperty(kEnableInstancedPerPixelNormal, props, false);
        }

        protected override bool ShouldEmissionBeEnabled(Material mat)
        {
            return false;
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        static public void SetupLayersMappingKeywords(Material material)
        {
            const string kLayerMappingPlanar = "_LAYER_MAPPING_PLANAR";
            const string kLayerMappingTriplanar = "_LAYER_MAPPING_TRIPLANAR";

            for (int i = 0; i < kMaxLayerCount; ++i)
            {
                string layerUVBaseParam = string.Format("{0}{1}", kUVBase, i);
                UVBaseMapping layerUVBaseMapping = (UVBaseMapping)material.GetFloat(layerUVBaseParam);
                string currentLayerMappingPlanar = string.Format("{0}{1}", kLayerMappingPlanar, i);
                CoreUtils.SetKeyword(material, currentLayerMappingPlanar, layerUVBaseMapping == UVBaseMapping.Planar);
                string currentLayerMappingTriplanar = string.Format("{0}{1}", kLayerMappingTriplanar, i);
                CoreUtils.SetKeyword(material, currentLayerMappingTriplanar, layerUVBaseMapping == UVBaseMapping.Triplanar);
            }
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static new public void SetupMaterialKeywordsAndPass(Material material)
        {
            SetupBaseLitKeywords(material);
            SetupBaseLitMaterialPass(material);

            // TODO: planar/triplannar supprt
            //SetupLayersMappingKeywords(material);

            bool enableHeightBlend = material.HasProperty(kEnableHeightBlend) && material.GetFloat(kEnableHeightBlend) > 0;
            CoreUtils.SetKeyword(material, "_TERRAIN_BLEND_HEIGHT", enableHeightBlend);

            bool enableInstancedPerPixelNormal = material.GetFloat(kEnableInstancedPerPixelNormal) > 0.0f;
            CoreUtils.SetKeyword(material, "_TERRAIN_INSTANCED_PERPIXEL_NORMAL", enableInstancedPerPixelNormal);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            FindBaseMaterialProperties(props);
            FindMaterialProperties(props);

            m_MaterialEditor = materialEditor;

            // We should always register the key used to keep collapsable state
            InitExpandableState(materialEditor);

            // We should always do this call at the beginning
            m_MaterialEditor.serializedObject.Update();

            //Material material = m_MaterialEditor.target as Material;
            //AssetImporter materialImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetInstanceID()));

            bool optionsChanged = false;
            EditorGUI.BeginChangeCheck();
            {
                BaseMaterialPropertiesGUI();
                if (enableHeightBlend != null)
                {
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(enableHeightBlend, styles.enableHeightBlend);
                    if (enableHeightBlend.floatValue > 0)
                    {
                        EditorGUI.indentLevel++;
                        m_MaterialEditor.ShaderProperty(heightTransition, styles.heightTransition);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space();
            }
            if (EditorGUI.EndChangeCheck())
            {
                optionsChanged = true;
            }

            bool enablePerPixelNormalChanged = false;


            using (var header = new HeaderScope(StylesBaseUnlit.advancedText, (uint)Expandable.Advance, this))
            {
                if (header.expanded)
                {
                    // NB RenderQueue editor is not shown on purpose: we want to override it based on blend mode
                    m_MaterialEditor.EnableInstancingField();
                    if (m_MaterialEditor.IsInstancingEnabled())
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginChangeCheck();
                        m_MaterialEditor.ShaderProperty(enableInstancedPerPixelNormal, styles.enableInstancedPerPixelNormal);
                        enablePerPixelNormalChanged = EditorGUI.EndChangeCheck();
                        EditorGUI.indentLevel--;
                    }
                }
            }

            if (optionsChanged || enablePerPixelNormalChanged)
            {
                foreach (var obj in m_MaterialEditor.targets)
                {
                    SetupMaterialKeywordsAndPassInternal((Material)obj);
                }
            }

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }

        private bool m_ShowChannelRemapping = false;

        enum HeightParametrization
        {
            Amplitude,
            MinMax
        };
        private HeightParametrization m_HeightParametrization = HeightParametrization.Amplitude;

        private static bool DoesTerrainUseMaskMaps(TerrainLayer[] terrainLayers)
        {
            for (int i = 0; i < terrainLayers.Length; ++i)
            {
                if (terrainLayers[i].maskMapTexture != null)
                    return true;
            }
            return false;
        }

        bool ITerrainLayerCustomUI.OnTerrainLayerGUI(TerrainLayer terrainLayer, Terrain terrain)
        {
            var terrainLayers = terrain.terrainData.terrainLayers;
            if (!DoesTerrainUseMaskMaps(terrainLayers))
                return false;

            // Don't use the member field enableHeightBlend as ShaderGUI.OnGUI might not be called if the material UI is folded.
            bool heightBlend = terrain.materialTemplate.HasProperty(kEnableHeightBlend) && terrain.materialTemplate.GetFloat(kEnableHeightBlend) > 0;

            terrainLayer.diffuseTexture = EditorGUILayout.ObjectField(styles.diffuseTexture, terrainLayer.diffuseTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateDiffuseTextureUI(terrainLayer.diffuseTexture);

            var diffuseRemapMin = terrainLayer.diffuseRemapMin;
            var diffuseRemapMax = terrainLayer.diffuseRemapMax;
            EditorGUI.BeginChangeCheck();

            bool enableDensity = false;
            if (terrainLayer.diffuseTexture != null)
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect.y += 16 + 4;
                rect.width = EditorGUIUtility.labelWidth + 64;
                rect.height = 16;

                ++EditorGUI.indentLevel;

                var diffuseTint = new Color(diffuseRemapMax.x, diffuseRemapMax.y, diffuseRemapMax.z);
                diffuseTint = EditorGUI.ColorField(rect, styles.colorTint, diffuseTint, true, false, false);
                diffuseRemapMax.x = diffuseTint.r;
                diffuseRemapMax.y = diffuseTint.g;
                diffuseRemapMax.z = diffuseTint.b;
                diffuseRemapMin.x = diffuseRemapMin.y = diffuseRemapMin.z = 0;

                if (!heightBlend)
                {
                    rect.y = rect.yMax + 2;
                    enableDensity = EditorGUI.Toggle(rect, styles.opacityAsDensity, diffuseRemapMin.w > 0);
                }

                --EditorGUI.indentLevel;
            }
            diffuseRemapMax.w = 1;
            diffuseRemapMin.w = enableDensity ? 1 : 0;

            if (EditorGUI.EndChangeCheck())
            {
                terrainLayer.diffuseRemapMin = diffuseRemapMin;
                terrainLayer.diffuseRemapMax = diffuseRemapMax;
            }

            terrainLayer.normalMapTexture = EditorGUILayout.ObjectField(styles.normalMapTexture, terrainLayer.normalMapTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateNormalMapTextureUI(terrainLayer.normalMapTexture, TerrainLayerUtility.CheckNormalMapTextureType(terrainLayer.normalMapTexture));

            if (terrainLayer.normalMapTexture != null)
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect.y += 16 + 4;
                rect.width = EditorGUIUtility.labelWidth + 64;
                rect.height = 16;

                ++EditorGUI.indentLevel;
                terrainLayer.normalScale = EditorGUI.FloatField(rect, styles.normalScale, terrainLayer.normalScale);
                --EditorGUI.indentLevel;
            }

            terrainLayer.maskMapTexture = EditorGUILayout.ObjectField(heightBlend ? styles.maskMapTexture : styles.maskMapTextureWithoutHeight, terrainLayer.maskMapTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateMaskMapTextureUI(terrainLayer.maskMapTexture);

            var maskMapRemapMin = terrainLayer.maskMapRemapMin;
            var maskMapRemapMax = terrainLayer.maskMapRemapMax;

            ++EditorGUI.indentLevel;
            EditorGUI.BeginChangeCheck();

            m_ShowChannelRemapping = EditorGUILayout.Foldout(m_ShowChannelRemapping, terrainLayer.maskMapTexture != null ? s_Styles.channelRemapping : s_Styles.defaultValues);
            if (m_ShowChannelRemapping)
            {
                if (terrainLayer.maskMapTexture != null)
                {
                    float min, max;
                    min = maskMapRemapMin.x; max = maskMapRemapMax.x;
                    EditorGUILayout.MinMaxSlider(s_Styles.metallic, ref min, ref max, 0, 1);
                    maskMapRemapMin.x = min; maskMapRemapMax.x = max;

                    min = maskMapRemapMin.y; max = maskMapRemapMax.y;
                    EditorGUILayout.MinMaxSlider(s_Styles.ao, ref min, ref max, 0, 1);
                    maskMapRemapMin.y = min; maskMapRemapMax.y = max;

                    if (heightBlend)
                    {
                        EditorGUILayout.LabelField(styles.height);
                        ++EditorGUI.indentLevel;
                        m_HeightParametrization = (HeightParametrization)EditorGUILayout.EnumPopup(styles.heightParametrization, m_HeightParametrization);
                        if (m_HeightParametrization == HeightParametrization.Amplitude)
                        {
                            // (height - heightBase) * amplitude
                            float amplitude = Mathf.Max(maskMapRemapMax.z - maskMapRemapMin.z, Mathf.Epsilon); // to avoid divide by zero
                            float heightBase = -maskMapRemapMin.z / amplitude;
                            amplitude = EditorGUILayout.FloatField(styles.heightAmplitude, amplitude * 100) / 100;
                            heightBase = EditorGUILayout.FloatField(styles.heightBase, heightBase);
                            maskMapRemapMin.z = -heightBase * amplitude;
                            maskMapRemapMax.z = (1 - heightBase) * amplitude;
                        }
                        else
                        {
                            maskMapRemapMin.z = EditorGUILayout.FloatField(styles.heightMin, maskMapRemapMin.z * 100) / 100;
                            maskMapRemapMax.z = EditorGUILayout.FloatField(styles.heightMax, maskMapRemapMax.z * 100) / 100;
                        }
                        --EditorGUI.indentLevel;
                    }

                    min = maskMapRemapMin.w; max = maskMapRemapMax.w;
                    EditorGUILayout.MinMaxSlider(s_Styles.smoothness, ref min, ref max, 0, 1);
                    maskMapRemapMin.w = min; maskMapRemapMax.w = max;
                }
                else
                {
                    maskMapRemapMin.x = maskMapRemapMax.x = EditorGUILayout.Slider(s_Styles.metallic, maskMapRemapMin.x, 0, 1);
                    maskMapRemapMin.y = maskMapRemapMax.y = EditorGUILayout.Slider(s_Styles.ao, maskMapRemapMin.y, 0, 1);
                    if (heightBlend)
                        maskMapRemapMin.z = maskMapRemapMax.z = EditorGUILayout.FloatField(s_Styles.heightCm, maskMapRemapMin.z * 100) / 100;
                    maskMapRemapMin.w = maskMapRemapMax.w = EditorGUILayout.Slider(s_Styles.smoothness, maskMapRemapMin.w, 0, 1);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                terrainLayer.maskMapRemapMin = maskMapRemapMin;
                terrainLayer.maskMapRemapMax = maskMapRemapMax;
            }
            --EditorGUI.indentLevel;

            EditorGUILayout.Space();
            TerrainLayerUtility.TilingSettingsUI(terrainLayer);

            return true;
        }
    }
} // namespace UnityEditor
