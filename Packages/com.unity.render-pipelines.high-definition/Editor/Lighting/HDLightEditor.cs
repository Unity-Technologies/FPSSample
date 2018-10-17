using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(HDRenderPipelineAsset))]
    sealed partial class HDLightEditor : LightEditor
    {
        [MenuItem("CONTEXT/Light/Remove Component", false, 0)]
        static void RemoveLight(MenuCommand menuCommand)
        {
            GameObject go = ((Light)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            Undo.IncrementCurrentGroup();
            Undo.DestroyObjectImmediate(go.GetComponent<Light>());
            Undo.DestroyObjectImmediate(go.GetComponent<HDAdditionalLightData>());
            Undo.DestroyObjectImmediate(go.GetComponent<AdditionalShadowData>());
        }

        [MenuItem("CONTEXT/Light/Reset", false, 0)]
        static void ResetLight(MenuCommand menuCommand)
        {
            GameObject go = ((Light)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            Light light = go.GetComponent<Light>();
            HDAdditionalLightData lightAdditionalData = go.GetComponent<HDAdditionalLightData>();
            AdditionalShadowData shadowAdditionalData = go.GetComponent<AdditionalShadowData>();

            Assert.IsNotNull(light);
            Assert.IsNotNull(lightAdditionalData);
            Assert.IsNotNull(shadowAdditionalData);

            Undo.RecordObjects(new UnityEngine.Object[] { light, lightAdditionalData, shadowAdditionalData }, "Reset HD Light");
            light.Reset();
            // To avoid duplicating init code we copy default settings to Reset additional data
            // Note: we can't call this code inside the HDAdditionalLightData, thus why we don't wrap it in a Reset() function
            HDUtils.s_DefaultHDAdditionalLightData.CopyTo(lightAdditionalData);
            HDUtils.s_DefaultAdditionalShadowData.CopyTo(shadowAdditionalData);
        }

        sealed class SerializedLightData
        {
            public SerializedProperty intensity;
            public SerializedProperty enableSpotReflector;
            public SerializedProperty spotInnerPercent;
            public SerializedProperty lightDimmer;
            public SerializedProperty fadeDistance;
            public SerializedProperty affectDiffuse;
            public SerializedProperty affectSpecular;
            public SerializedProperty nonLightmappedOnly;
            public SerializedProperty lightTypeExtent;
            public SerializedProperty spotLightShape;
            public SerializedProperty shapeWidth;
            public SerializedProperty shapeHeight;
            public SerializedProperty aspectRatio;
            public SerializedProperty shapeRadius;
            public SerializedProperty maxSmoothness;
            public SerializedProperty applyRangeAttenuation;
            public SerializedProperty volumetricDimmer;
            public SerializedProperty lightUnit;
            public SerializedProperty displayAreaLightEmissiveMesh;
            public SerializedProperty lightLayers;
            public SerializedProperty shadowNearPlane;
            public SerializedProperty shadowSoftness;
            public SerializedProperty blockerSampleCount;
            public SerializedProperty filterSampleCount;
            public SerializedProperty sunDiskSize;
            public SerializedProperty sunHaloSize;

            // Editor stuff
            public SerializedProperty useOldInspector;
            public SerializedProperty showFeatures;
            public SerializedProperty showAdditionalSettings;
        }

        sealed class SerializedShadowData
        {
            public SerializedProperty shadowDimmer;
            public SerializedProperty volumetricShadowDimmer;
            public SerializedProperty fadeDistance;
            public SerializedProperty resolution;
            public SerializedProperty contactShadows;

            // Bias control
            public SerializedProperty viewBiasMin;
            public SerializedProperty viewBiasMax;
            public SerializedProperty viewBiasScale;
            public SerializedProperty normalBiasMin;
            public SerializedProperty normalBiasMax;
            public SerializedProperty normalBiasScale;
            public SerializedProperty sampleBiasScale;
            public SerializedProperty edgeLeakFixup;
            public SerializedProperty edgeToleranceNormal;
            public SerializedProperty edgeTolerance;
        }

        SerializedObject m_SerializedAdditionalLightData;
        SerializedObject m_SerializedAdditionalShadowData;

        SerializedLightData m_AdditionalLightData;
        SerializedShadowData m_AdditionalShadowData;

        // LightType + LightTypeExtent combined
        enum LightShape
        {
            Spot,
            Directional,
            Point,
            //Area, <= offline base type not displayed in our case but used for GI of our area light
            Rectangle,
            Line,
            //Sphere,
            //Disc,
        }

        enum DirectionalLightUnit
        {
            Lux = LightUnit.Lux,
        }

        enum AreaLightUnit
        {
            Lumen = LightUnit.Lumen,
            Luminance = LightUnit.Luminance,
            Ev100 = LightUnit.Ev100,
        }

        enum PunctualLightUnit
        {
            Lumen = LightUnit.Lumen,
            Candela = LightUnit.Candela,
        }

        const float k_MinAreaWidth = 0.01f; // Provide a small size of 1cm for line light

        // Used for UI only; the processing code must use LightTypeExtent and LightType
        LightShape m_LightShape;

        HDAdditionalLightData[]     m_AdditionalLightDatas;
        AdditionalShadowData[]      m_AdditionalShadowDatas;

        bool m_UpdateAreaLightEmissiveMeshComponents = false;

        HDShadowInitParameters                m_HDShadowInitParameters;
        Dictionary<HDShadowQuality, Action>   m_ShadowAlgorithmUIs;

        //we need this to determine if we not attempt to render it two time the same frame
        //This is needed as we have tried to work outside of Gizmo scope with Handle only for SRP
        int lastRenderedHandleFrame = 0; 

        protected override void OnEnable()
        {
            base.OnEnable();

            // Get & automatically add additional HD data if not present
            m_AdditionalLightDatas = CoreEditorUtils.GetAdditionalData<HDAdditionalLightData>(targets, HDAdditionalLightData.InitDefaultHDAdditionalLightData);
            m_AdditionalShadowDatas = CoreEditorUtils.GetAdditionalData<AdditionalShadowData>(targets, HDAdditionalShadowData.InitDefaultHDAdditionalShadowData);
            m_SerializedAdditionalLightData = new SerializedObject(m_AdditionalLightDatas);
            m_SerializedAdditionalShadowData = new SerializedObject(m_AdditionalShadowDatas);

            using (var o = new PropertyFetcher<HDAdditionalLightData>(m_SerializedAdditionalLightData))
                m_AdditionalLightData = new SerializedLightData
                {
                    intensity = o.Find(x => x.displayLightIntensity),
                    enableSpotReflector = o.Find(x => x.enableSpotReflector),
                    spotInnerPercent = o.Find(x => x.m_InnerSpotPercent),
                    lightDimmer = o.Find(x => x.lightDimmer),
                    volumetricDimmer = o.Find(x => x.volumetricDimmer),
                    lightUnit = o.Find(x => x.lightUnit),
                    displayAreaLightEmissiveMesh = o.Find(x => x.displayAreaLightEmissiveMesh),
                    lightLayers = o.Find(x => x.lightLayers),
                    fadeDistance = o.Find(x => x.fadeDistance),
                    affectDiffuse = o.Find(x => x.affectDiffuse),
                    affectSpecular = o.Find(x => x.affectSpecular),
                    nonLightmappedOnly = o.Find(x => x.nonLightmappedOnly),
                    lightTypeExtent = o.Find(x => x.lightTypeExtent),
                    spotLightShape = o.Find(x => x.spotLightShape),
                    shapeWidth = o.Find(x => x.shapeWidth),
                    shapeHeight = o.Find(x => x.shapeHeight),
                    aspectRatio = o.Find(x => x.aspectRatio),
                    shapeRadius = o.Find(x => x.shapeRadius),
                    maxSmoothness = o.Find(x => x.maxSmoothness),
                    applyRangeAttenuation = o.Find(x => x.applyRangeAttenuation),
                    shadowNearPlane = o.Find(x => x.shadowNearPlane),
                    shadowSoftness = o.Find(x => x.shadowSoftness),
                    blockerSampleCount = o.Find(x => x.blockerSampleCount),
                    filterSampleCount = o.Find(x => x.filterSampleCount),
                    sunDiskSize = o.Find(x => x.sunDiskSize),
                    sunHaloSize = o.Find(x => x.sunHaloSize),

                    // Editor stuff
                    useOldInspector = o.Find(x => x.useOldInspector),
                    showFeatures = o.Find(x => x.featuresFoldout),
                    showAdditionalSettings = o.Find(x => x.showAdditionalSettings)
                };

            // TODO: Review this once AdditionalShadowData is refactored
            using (var o = new PropertyFetcher<AdditionalShadowData>(m_SerializedAdditionalShadowData))
                m_AdditionalShadowData = new SerializedShadowData
                {
                    shadowDimmer = o.Find(x => x.shadowDimmer),
                    volumetricShadowDimmer = o.Find(x => x.volumetricShadowDimmer),
                    fadeDistance = o.Find(x => x.shadowFadeDistance),
                    resolution = o.Find(x => x.shadowResolution),
                    contactShadows = o.Find(x => x.contactShadows),

                    viewBiasMin = o.Find(x => x.viewBiasMin),
                    viewBiasMax = o.Find(x => x.viewBiasMax),
                    viewBiasScale = o.Find(x => x.viewBiasScale),
                    normalBiasMin = o.Find(x => x.normalBiasMin),
                    normalBiasMax = o.Find(x => x.normalBiasMax),
                    normalBiasScale = o.Find(x => x.normalBiasScale),
                    sampleBiasScale = o.Find(x => x.sampleBiasScale),
                    edgeLeakFixup = o.Find(x => x.edgeLeakFixup),
                    edgeToleranceNormal = o.Find(x => x.edgeToleranceNormal),
                    edgeTolerance = o.Find(x => x.edgeTolerance)
                };

            // Update emissive mesh and light intensity when undo/redo
            Undo.undoRedoPerformed += () => {
                m_SerializedAdditionalLightData.ApplyModifiedProperties();
                foreach (var hdLightData in m_AdditionalLightDatas)
                    if (hdLightData != null)
                        hdLightData.UpdateAreaLightEmissiveMesh();
            };

            // If the light is disabled in the editor we force the light upgrade from his inspector
            foreach (var additionalLightData in m_AdditionalLightDatas)
                additionalLightData.UpgradeLight();

            m_HDShadowInitParameters = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineSettings.hdShadowInitParams;
            m_ShadowAlgorithmUIs = new Dictionary<HDShadowQuality, Action>
            {
                {HDShadowQuality.Low, DrawLowShadowSettings},
                {HDShadowQuality.Medium, DrawMediumShadowSettings},
                {HDShadowQuality.High, DrawHighShadowSettings}
            };
        }

        public override void OnInspectorGUI()
        {
            m_SerializedAdditionalLightData.Update();
            m_SerializedAdditionalShadowData.Update();

            // Disable the default light editor for the release, it is just use for development
            /*
            // Temporary toggle to go back to the old editor & separated additional datas
            bool useOldInspector = m_AdditionalLightData.useOldInspector.boolValue;

            if (GUILayout.Button("Toggle default light editor"))
                useOldInspector = !useOldInspector;

            m_AdditionalLightData.useOldInspector.boolValue = useOldInspector;

            if (useOldInspector)
            {
                DrawDefaultInspector();
                ApplyAdditionalComponentsVisibility(false);
                m_SerializedAdditionalShadowData.ApplyModifiedProperties();
                m_SerializedAdditionalLightData.ApplyModifiedProperties();
                return;
            }
            */

            // New editor
            ApplyAdditionalComponentsVisibility(true);
            CheckStyles();

            settings.Update();

            ResolveLightShape();

            DrawFoldout(m_AdditionalLightData.showFeatures, "Features", DrawFeatures);
            DrawFoldout(settings.lightType, "Shape", DrawShape);
            DrawFoldout(settings.intensity, "Light", DrawLightSettings);

            if (settings.shadowsType.enumValueIndex != (int)LightShadows.None)
                DrawFoldout(settings.shadowsType, "Shadows", DrawShadows);

            CoreEditorUtils.DrawSplitter();
            EditorGUILayout.Space();

            m_SerializedAdditionalShadowData.ApplyModifiedProperties();
            m_SerializedAdditionalLightData.ApplyModifiedProperties();
            settings.ApplyModifiedProperties();

            if (m_UpdateAreaLightEmissiveMeshComponents)
                UpdateAreaLightEmissiveMeshComponents();
        }

        protected override void OnSceneGUI()
        {
            Light light = (Light)target;
            if (!Selection.Contains(light.gameObject) || lastRenderedHandleFrame == Time.frameCount)
            {
                return;
            }
            lastRenderedHandleFrame = Time.frameCount;

            m_SerializedAdditionalLightData.Update();

            HDAdditionalLightData src = (HDAdditionalLightData)m_SerializedAdditionalLightData.targetObject;

            Color wireframeColorAbove = light.enabled ? LightEditor.kGizmoLight : LightEditor.kGizmoDisabledLight;
            Color handleColorAbove = CoreLightEditorUtilities.GetLightHandleColor(wireframeColorAbove);
            Color wireframeColorBehind = CoreLightEditorUtilities.GetLightBehindObjectWireframeColor(wireframeColorAbove);
            Color handleColorBehind = CoreLightEditorUtilities.GetLightHandleColor(wireframeColorBehind);

            switch (src.lightTypeExtent)
            {
                case LightTypeExtent.Punctual:
                    switch (light.type)
                    {
                        case LightType.Directional:
                        case LightType.Point:
                            base.OnSceneGUI();  //use legacy handles
                            break;
                        case LightType.Spot:
                            switch (src.spotLightShape)
                            {
                                case SpotLightShape.Cone:
                                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                                    {
                                        Vector3 outterAngleInnerAngleRange = new Vector3(light.spotAngle, light.spotAngle * src.GetInnerSpotPercent01(), light.range);
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                        Handles.color = wireframeColorBehind;
                                        CoreLightEditorUtilities.DrawSpotlightWireframe(outterAngleInnerAngleRange, m_AdditionalLightData.shadowNearPlane.floatValue);
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                        Handles.color = wireframeColorAbove;
                                        CoreLightEditorUtilities.DrawSpotlightWireframe(outterAngleInnerAngleRange, m_AdditionalLightData.shadowNearPlane.floatValue);
                                        EditorGUI.BeginChangeCheck();
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                        Handles.color = handleColorBehind;
                                        outterAngleInnerAngleRange = CoreLightEditorUtilities.DrawSpotlightHandle(outterAngleInnerAngleRange);
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                        Handles.color = handleColorAbove;
                                        outterAngleInnerAngleRange = CoreLightEditorUtilities.DrawSpotlightHandle(outterAngleInnerAngleRange);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            Undo.RecordObjects(new UnityEngine.Object[] { target, src }, "Adjust Cone Spot Light");
                                            src.m_InnerSpotPercent = 100f * outterAngleInnerAngleRange.y / outterAngleInnerAngleRange.x;
                                            light.spotAngle = outterAngleInnerAngleRange.x;
                                            light.range = outterAngleInnerAngleRange.z;
                                        }

                                        // Handles.color reseted at end of scope
                                    }
                                    break;
                                case SpotLightShape.Pyramid:
                                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                                    {
                                        Vector4 aspectFovMaxRangeMinRange = new Vector4(src.aspectRatio, light.spotAngle, light.range);
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                        Handles.color = wireframeColorBehind;
                                        CoreLightEditorUtilities.DrawPyramidFrustumWireframe(aspectFovMaxRangeMinRange);
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                        Handles.color = wireframeColorAbove;
                                        CoreLightEditorUtilities.DrawPyramidFrustumWireframe(aspectFovMaxRangeMinRange);
                                        EditorGUI.BeginChangeCheck();
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                        Handles.color = handleColorBehind;
                                        aspectFovMaxRangeMinRange = CoreLightEditorUtilities.DrawPyramidFrustumHandle(aspectFovMaxRangeMinRange, false);
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                        Handles.color = handleColorAbove;
                                        aspectFovMaxRangeMinRange = CoreLightEditorUtilities.DrawPyramidFrustumHandle(aspectFovMaxRangeMinRange, false);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            Undo.RecordObjects(new UnityEngine.Object[] { target, src }, "Adjust Pyramid Spot Light");
                                            src.aspectRatio = aspectFovMaxRangeMinRange.x;
                                            light.spotAngle = aspectFovMaxRangeMinRange.y;
                                            light.range = aspectFovMaxRangeMinRange.z;
                                        }

                                        // Handles.color reseted at end of scope
                                    }
                                    break;
                                case SpotLightShape.Box:
                                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                                    {
                                        Vector4 widthHeightMaxRangeMinRange = new Vector4(src.shapeWidth, src.shapeHeight, light.range);
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                        Handles.color = wireframeColorBehind;
                                        CoreLightEditorUtilities.DrawOrthoFrustumWireframe(widthHeightMaxRangeMinRange);
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                        Handles.color = wireframeColorAbove;
                                        CoreLightEditorUtilities.DrawOrthoFrustumWireframe(widthHeightMaxRangeMinRange);
                                        EditorGUI.BeginChangeCheck();
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                                        Handles.color = handleColorBehind;
                                        widthHeightMaxRangeMinRange = CoreLightEditorUtilities.DrawOrthoFrustumHandle(widthHeightMaxRangeMinRange, false);
                                        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                                        Handles.color = handleColorAbove;
                                        widthHeightMaxRangeMinRange = CoreLightEditorUtilities.DrawOrthoFrustumHandle(widthHeightMaxRangeMinRange, false);
                                        if (EditorGUI.EndChangeCheck())
                                        {
                                            Undo.RecordObjects(new UnityEngine.Object[] { target, src }, "Adjust Box Spot Light");
                                            src.shapeWidth = widthHeightMaxRangeMinRange.x;
                                            src.shapeHeight = widthHeightMaxRangeMinRange.y;
                                            light.range = widthHeightMaxRangeMinRange.z;
                                        }

                                        // Handles.color reseted at end of scope
                                    }
                                    break;
                            }
                            break;
                    }
                    break;
                case LightTypeExtent.Rectangle:
                case LightTypeExtent.Line:
                    bool withYAxis = src.lightTypeExtent == LightTypeExtent.Rectangle;
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        Vector2 widthHeight = new Vector4(light.areaSize.x, withYAxis ? light.areaSize.y : 0f);
                        float range = light.range;
                        EditorGUI.BeginChangeCheck();
                        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                        Handles.color = wireframeColorBehind;
                        CoreLightEditorUtilities.DrawAreaLightWireframe(widthHeight);
                        range = Handles.RadiusHandle(Quaternion.identity, Vector3.zero, range); //also draw handles
                        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                        Handles.color = wireframeColorAbove;
                        CoreLightEditorUtilities.DrawAreaLightWireframe(widthHeight);
                        range = Handles.RadiusHandle(Quaternion.identity, Vector3.zero, range); //also draw handles
                        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                        Handles.color = handleColorBehind;
                        widthHeight = CoreLightEditorUtilities.DrawAreaLightHandle(widthHeight, withYAxis);
                        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                        Handles.color = handleColorAbove;
                        widthHeight = CoreLightEditorUtilities.DrawAreaLightHandle(widthHeight, withYAxis);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new UnityEngine.Object[] { target, src }, withYAxis ? "Adjust Area Rectangle Light" : "Adjust Area Line Light");
                            light.areaSize = withYAxis ? widthHeight : new Vector2(widthHeight.x, light.areaSize.y);
                            light.range = range;
                        }

                        // Handles.color reseted at end of scope
                    }
                    break;
            }
        }

        void DrawFoldout(SerializedProperty foldoutProperty, string title, Action func)
        {
            CoreEditorUtils.DrawSplitter();

            bool state = foldoutProperty.isExpanded;
            state = CoreEditorUtils.DrawHeaderFoldout(title, state);

            if (state)
            {
                EditorGUI.indentLevel++;
                func();
                EditorGUI.indentLevel--;
                GUILayout.Space(2f);
            }

            foldoutProperty.isExpanded = state;
        }

        void DrawFeatures()
        {
            bool disabledScope = m_LightShape == LightShape.Line || (m_LightShape == LightShape.Rectangle && settings.isRealtime);

            using (new EditorGUI.DisabledScope(disabledScope))
            {
                bool shadowsEnabled = EditorGUILayout.Toggle(CoreEditorUtils.GetContent("Enable Shadows"), settings.shadowsType.enumValueIndex != 0);
                settings.shadowsType.enumValueIndex = shadowsEnabled ? (int)LightShadows.Hard : (int)LightShadows.None;
            }

            EditorGUILayout.PropertyField(m_AdditionalLightData.showAdditionalSettings);
        }

        void DrawShape()
        {
            EditorGUI.BeginChangeCheck(); // For GI we need to detect any change on additional data and call SetLightDirty + For intensity we need to detect light shape change

            EditorGUI.BeginChangeCheck();
            m_LightShape = (LightShape)EditorGUILayout.Popup(s_Styles.shape, (int)m_LightShape, s_Styles.shapeNames);
            if (EditorGUI.EndChangeCheck())
                UpdateLightIntensityUnit();

            if (m_LightShape != LightShape.Directional)
                settings.DrawRange(false);

            // LightShape is HD specific, it need to drive LightType from the original LightType
            // when it make sense, so the GI is still in sync with the light shape
            switch (m_LightShape)
            {
                case LightShape.Directional:
                    settings.lightType.enumValueIndex = (int)LightType.Directional;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;

                    // Sun disk.
                    EditorGUILayout.Slider(m_AdditionalLightData.sunDiskSize, 0f, 45f, s_Styles.sunDiskSize);
                    EditorGUILayout.Slider(m_AdditionalLightData.sunHaloSize, 0f, 1f, s_Styles.sunHaloSize);
                    EditorGUILayout.PropertyField(m_AdditionalLightData.maxSmoothness, s_Styles.maxSmoothness);
                    break;

                case LightShape.Point:
                    settings.lightType.enumValueIndex = (int)LightType.Point;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeRadius, s_Styles.lightRadius);
                    EditorGUILayout.PropertyField(m_AdditionalLightData.maxSmoothness, s_Styles.maxSmoothness);
                    break;

                case LightShape.Spot:
                    settings.lightType.enumValueIndex = (int)LightType.Spot;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.spotLightShape, s_Styles.spotLightShape);
                    var spotLightShape = (SpotLightShape)m_AdditionalLightData.spotLightShape.enumValueIndex;
                    if (spotLightShape == SpotLightShape.Box)
                    {
                        // Box light is a boxed directional light.
                        EditorGUILayout.PropertyField(m_AdditionalLightData.shapeWidth, s_Styles.shapeWidthBox);
                        EditorGUILayout.PropertyField(m_AdditionalLightData.shapeHeight, s_Styles.shapeHeightBox);
                    }
                    else
                    {
                        if (spotLightShape == SpotLightShape.Cone)
                        {
                            settings.DrawSpotAngle();
                            EditorGUILayout.Slider(m_AdditionalLightData.spotInnerPercent, 0f, 100f, s_Styles.spotInnerPercent);
                        }
                        // TODO : replace with angle and ratio
                        else if (spotLightShape == SpotLightShape.Pyramid)
                        {
                            settings.DrawSpotAngle();
                            EditorGUILayout.Slider(m_AdditionalLightData.aspectRatio, 0.05f, 20.0f, s_Styles.aspectRatioPyramid);
                        }

                        EditorGUILayout.PropertyField(m_AdditionalLightData.shapeRadius, s_Styles.lightRadius);
                        EditorGUILayout.PropertyField(m_AdditionalLightData.maxSmoothness, s_Styles.maxSmoothness);
                    }
                    break;

                case LightShape.Rectangle:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_BaseData.type.enumValueIndex = (int)LightType.Rectangle;
                    // In case of change, think to update InitDefaultHDAdditionalLightData()
                    settings.lightType.enumValueIndex = (int)LightType.Point;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Rectangle;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeWidth, s_Styles.shapeWidthRect);
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeHeight, s_Styles.shapeHeightRect);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_AdditionalLightData.shapeWidth.floatValue = Mathf.Max(m_AdditionalLightData.shapeWidth.floatValue, k_MinAreaWidth);
                        m_AdditionalLightData.shapeHeight.floatValue = Mathf.Max(m_AdditionalLightData.shapeHeight.floatValue, k_MinAreaWidth);
                        settings.areaSizeX.floatValue = m_AdditionalLightData.shapeWidth.floatValue;
                        settings.areaSizeY.floatValue = m_AdditionalLightData.shapeHeight.floatValue;
                    }
                    if (settings.isRealtime)
                        settings.shadowsType.enumValueIndex = (int)LightShadows.None;
                    break;

                case LightShape.Line:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_BaseData.type.enumValueIndex = (int)LightType.Rectangle;
                    settings.lightType.enumValueIndex = (int)LightType.Point;
                    m_AdditionalLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Line;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeWidth, s_Styles.shapeWidthLine);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_AdditionalLightData.shapeWidth.floatValue = Mathf.Max(m_AdditionalLightData.shapeWidth.floatValue, k_MinAreaWidth);
                        m_AdditionalLightData.shapeHeight.floatValue = Mathf.Max(m_AdditionalLightData.shapeHeight.floatValue, k_MinAreaWidth);
                        // Fake line with a small rectangle in vanilla unity for GI
                        settings.areaSizeX.floatValue = m_AdditionalLightData.shapeWidth.floatValue;
                        settings.areaSizeY.floatValue = k_MinAreaWidth;
                    }
                    settings.shadowsType.enumValueIndex = (int)LightShadows.None;
                    break;

                case (LightShape)(-1):
                    // don't do anything, this is just to handle multi selection
                    break;

                default:
                    Debug.Assert(false, "Not implemented light type");
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_AdditionalLightData.shapeRadius.floatValue = Mathf.Max(m_AdditionalLightData.shapeRadius.floatValue, 0.0f);
                m_UpdateAreaLightEmissiveMeshComponents = true;
                ((Light)target).SetLightDirty(); // Should be apply only to parameter that's affect GI, but make the code cleaner
            }
        }

        void UpdateLightIntensityUnit()
        {
            if (m_LightShape == LightShape.Directional)
                m_AdditionalLightData.lightUnit.enumValueIndex = (int)DirectionalLightUnit.Lux;
            else
                m_AdditionalLightData.lightUnit.enumValueIndex = (int)LightUnit.Lumen;
        }

        LightUnit LightIntensityUnitPopup(LightShape shape)
        {
            LightUnit     selectedLightUnit;
            LightUnit     oldLigthUnit = (LightUnit)m_AdditionalLightData.lightUnit.enumValueIndex;

            EditorGUI.BeginChangeCheck();
            switch (shape)
            {
                case LightShape.Directional:
                    selectedLightUnit = (LightUnit)EditorGUILayout.EnumPopup((DirectionalLightUnit)m_AdditionalLightData.lightUnit.enumValueIndex);
                    break;
                case LightShape.Point:
                case LightShape.Spot:
                    selectedLightUnit = (LightUnit)EditorGUILayout.EnumPopup((PunctualLightUnit)m_AdditionalLightData.lightUnit.enumValueIndex);
                    break;
                default:
                    selectedLightUnit = (LightUnit)EditorGUILayout.EnumPopup((AreaLightUnit)m_AdditionalLightData.lightUnit.enumValueIndex);
                    break;
            }
            if (EditorGUI.EndChangeCheck())
                ConvertLightIntensity(oldLigthUnit, selectedLightUnit);

            return selectedLightUnit;
        }

        void ConvertLightIntensity(LightUnit oldLightUnit, LightUnit newLightUnit)
        {
            float intensity = m_AdditionalLightData.intensity.floatValue;

            // For punctual lights
            if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Candela)
            {
                if (m_LightShape == LightShape.Spot && m_AdditionalLightData.enableSpotReflector.boolValue)
                {
                    // We have already calculate the correct value, just assign it
                    intensity = ((Light)target).intensity;
                }
                else
                    intensity = LightUtils.ConvertPointLightLumenToCandela(intensity);
            }
            if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lumen)
            {
                if (m_LightShape == LightShape.Spot && m_AdditionalLightData.enableSpotReflector.boolValue)
                {
                    // We just need to multiply candela by solid angle in this case
                    if ((SpotLightShape)m_AdditionalLightData.spotLightShape.enumValueIndex == SpotLightShape.Cone)
                        intensity = LightUtils.ConvertSpotLightCandelaToLumen(intensity, ((Light)target).spotAngle * Mathf.Deg2Rad, true);
                    else if ((SpotLightShape)m_AdditionalLightData.spotLightShape.enumValueIndex == SpotLightShape.Pyramid)
                    {
                        float angleA, angleB;
                        LightUtils.CalculateAnglesForPyramid(m_AdditionalLightData.aspectRatio.floatValue, ((Light)target).spotAngle * Mathf.Deg2Rad, out angleA, out angleB);

                        intensity = LightUtils.ConvertFrustrumLightCandelaToLumen(intensity, angleA, angleB);
                    }
                    else // Box
                        intensity = LightUtils.ConvertPointLightCandelaToLumen(intensity);
                }
                else
                    intensity = LightUtils.ConvertPointLightCandelaToLumen(intensity);
            }

            // For area lights
            if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Luminance)
                intensity = LightUtils.ConvertAreaLightLumenToLuminance((LightTypeExtent)m_AdditionalLightData.lightTypeExtent.enumValueIndex, intensity, m_AdditionalLightData.shapeWidth.floatValue, m_AdditionalLightData.shapeHeight.floatValue);
            if (oldLightUnit == LightUnit.Luminance && newLightUnit == LightUnit.Lumen)
                intensity = LightUtils.ConvertAreaLightLuminanceToLumen((LightTypeExtent)m_AdditionalLightData.lightTypeExtent.enumValueIndex, intensity, m_AdditionalLightData.shapeWidth.floatValue, m_AdditionalLightData.shapeHeight.floatValue);
            if (oldLightUnit == LightUnit.Luminance && newLightUnit == LightUnit.Ev100)
                intensity = LightUtils.ConvertLuminanceToEv(intensity);
            if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Luminance)
                intensity = LightUtils.ConvertEvToLuminance(intensity);
            if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lumen)
                intensity = LightUtils.ConvertAreaLightEvToLumen((LightTypeExtent)m_AdditionalLightData.lightTypeExtent.enumValueIndex, intensity, m_AdditionalLightData.shapeWidth.floatValue, m_AdditionalLightData.shapeHeight.floatValue);
            if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Ev100)
                intensity = LightUtils.ConvertAreaLightLumenToEv((LightTypeExtent)m_AdditionalLightData.lightTypeExtent.enumValueIndex, intensity, m_AdditionalLightData.shapeWidth.floatValue, m_AdditionalLightData.shapeHeight.floatValue);

            m_AdditionalLightData.intensity.floatValue = intensity;
        }

        void UpdateAreaLightEmissiveMeshComponents()
        {
            foreach (var hdLightData in m_AdditionalLightDatas)
            {
                hdLightData.UpdateAreaLightEmissiveMesh();

                MeshRenderer  emissiveMeshRenderer = hdLightData.GetComponent<MeshRenderer>();
                MeshFilter    emissiveMeshFilter = hdLightData.GetComponent<MeshFilter>();

                // If the display emissive mesh is disabled, skip to the next selected light
                if (emissiveMeshFilter == null || emissiveMeshRenderer == null)
                    continue ;

                // We only load the mesh and it's material here, because we can't do that inside HDAdditionalLightData (Editor assembly)
                // Every other properties of the mesh is updated in HDAdditionalLightData to support timeline and editor records
                emissiveMeshFilter.mesh = UnityEditor.Experimental.Rendering.HDPipeline.HDEditorUtils.LoadAsset< Mesh >("Runtime/RenderPipelineResources/Mesh/Quad.FBX");
                if (emissiveMeshRenderer.sharedMaterial == null)
                    emissiveMeshRenderer.material = new Material(Shader.Find("HDRenderPipeline/Unlit"));
            }

            m_UpdateAreaLightEmissiveMeshComponents = false;
        }

        void DrawLightSettings()
        {
            settings.DrawColor();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_AdditionalLightData.intensity, s_Styles.lightIntensity);
            m_AdditionalLightData.lightUnit.enumValueIndex = (int)LightIntensityUnitPopup(m_LightShape);
            EditorGUILayout.EndHorizontal();

            // Only display reflector option if it make sense
            if (m_LightShape == LightShape.Spot)
            {
                var spotLightShape = (SpotLightShape)m_AdditionalLightData.spotLightShape.enumValueIndex;
                if ((spotLightShape == SpotLightShape.Cone || spotLightShape == SpotLightShape.Pyramid)
                    && m_AdditionalLightData.lightUnit.enumValueIndex == (int)PunctualLightUnit.Lumen)
                    EditorGUILayout.PropertyField(m_AdditionalLightData.enableSpotReflector, s_Styles.enableSpotReflector);
            }

            settings.DrawBounceIntensity();

            settings.DrawLightmapping();

            EditorGUI.BeginChangeCheck(); // For GI we need to detect any change on additional data and call SetLightDirty

            // No cookie with area light (maybe in future textured area light ?)
            if (!HDAdditionalLightData.IsAreaLight(m_AdditionalLightData.lightTypeExtent))
            {
                settings.DrawCookie();

                // When directional light use a cookie, it can control the size
                if (settings.cookie != null && m_LightShape == LightShape.Directional)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeWidth, s_Styles.cookieSizeX);
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeHeight, s_Styles.cookieSizeY);
                    EditorGUI.indentLevel--;
                }
            }

            if (m_AdditionalLightData.showAdditionalSettings.boolValue)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Additional Settings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                using (new EditorGUI.DisabledScope(!hdPipeline.asset.renderPipelineSettings.supportLightLayers))
                {
                    m_AdditionalLightData.lightLayers.intValue = Convert.ToInt32(EditorGUILayout.EnumFlagsField(s_Styles.lightLayer, (LightLayerEnum)m_AdditionalLightData.lightLayers.intValue));
                }
                EditorGUILayout.PropertyField(m_AdditionalLightData.affectDiffuse, s_Styles.affectDiffuse);
                EditorGUILayout.PropertyField(m_AdditionalLightData.affectSpecular, s_Styles.affectSpecular);
                if (m_LightShape != LightShape.Directional)
                    EditorGUILayout.PropertyField(m_AdditionalLightData.fadeDistance, s_Styles.fadeDistance);
                EditorGUILayout.PropertyField(m_AdditionalLightData.lightDimmer, s_Styles.lightDimmer);
                EditorGUILayout.PropertyField(m_AdditionalLightData.volumetricDimmer, s_Styles.volumetricDimmer);
                if (m_LightShape != LightShape.Directional)
                    EditorGUILayout.PropertyField(m_AdditionalLightData.applyRangeAttenuation, s_Styles.applyRangeAttenuation);

                // Emissive mesh for area light only
                if (HDAdditionalLightData.IsAreaLight(m_AdditionalLightData.lightTypeExtent))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_AdditionalLightData.displayAreaLightEmissiveMesh, s_Styles.displayAreaLightEmissiveMesh);
                    if (EditorGUI.EndChangeCheck())
                        m_UpdateAreaLightEmissiveMeshComponents = true;
                }

                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_UpdateAreaLightEmissiveMeshComponents = true;
                m_AdditionalLightData.fadeDistance.floatValue = Mathf.Max(m_AdditionalLightData.fadeDistance.floatValue, 0.01f);
                ((Light)target).SetLightDirty(); // Should be apply only to parameter that's affect GI, but make the code cleaner
            }
        }

        void DrawBakedShadowParameters()
        {
            switch ((LightType)settings.lightType.enumValueIndex)
            {
                case LightType.Directional:
                    EditorGUILayout.Slider(settings.bakedShadowAngleProp, 0f, 90f, s_Styles.bakedShadowAngle);
                    break;
                case LightType.Spot:
                case LightType.Point:
                    EditorGUILayout.PropertyField(settings.bakedShadowRadiusProp, s_Styles.bakedShadowRadius);
                    break;
            }


            if (settings.isMixed)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(m_AdditionalLightData.nonLightmappedOnly, s_Styles.nonLightmappedOnly);

                if (EditorGUI.EndChangeCheck())
                {
                    ((Light)target).lightShadowCasterMode = m_AdditionalLightData.nonLightmappedOnly.boolValue ? LightShadowCasterMode.NonLightmappedOnly : LightShadowCasterMode.Everything;
                }
            }
        }

        void DrawShadows()
        {
            if (settings.isCompletelyBaked)
            {
                DrawBakedShadowParameters();
                return;
            }

            EditorGUILayout.PropertyField(m_AdditionalShadowData.resolution, s_Styles.shadowResolution);
            //EditorGUILayout.Slider(settings.shadowsBias, 0.001f, 1f, s_Styles.shadowBias);
            //EditorGUILayout.Slider(settings.shadowsNormalBias, 0.001f, 1f, s_Styles.shadowNormalBias);
            EditorGUILayout.Slider(m_AdditionalShadowData.viewBiasScale, 0.0f, 15.0f, s_Styles.viewBiasScale);
            EditorGUILayout.Slider(m_AdditionalLightData.shadowNearPlane, HDShadowUtils.k_MinShadowNearPlane, 10f, s_Styles.shadowNearPlane);

            if (settings.isBakedOrMixed)
                DrawBakedShadowParameters();

            // Draw shadow settings using the current shadow algorithm
            HDShadowQuality currentAlgorithm;
            if (settings.lightType.enumValueIndex == (int)LightType.Directional)
                currentAlgorithm = (HDShadowQuality)m_HDShadowInitParameters.directionalShadowQuality;
            else
                currentAlgorithm = (HDShadowQuality)m_HDShadowInitParameters.punctualShadowQuality;
            m_ShadowAlgorithmUIs[currentAlgorithm]();

            // There is currently no additional settings for shadow on directional light
            if (m_AdditionalLightData.showAdditionalSettings.boolValue)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Additional Settings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_AdditionalShadowData.contactShadows, s_Styles.contactShadows);

                EditorGUILayout.Slider(m_AdditionalShadowData.shadowDimmer,           0.0f, 1.0f, s_Styles.shadowDimmer);
                EditorGUILayout.Slider(m_AdditionalShadowData.volumetricShadowDimmer, 0.0f, 1.0f, s_Styles.volumetricShadowDimmer);

                if (settings.lightType.enumValueIndex != (int)LightType.Directional)
                {
                    EditorGUILayout.PropertyField(m_AdditionalShadowData.fadeDistance, s_Styles.shadowFadeDistance);
                }

                EditorGUILayout.Slider(m_AdditionalShadowData.viewBiasMin, 0.0f, 5.0f, s_Styles.viewBiasMin);
                //EditorGUILayout.PropertyField(m_AdditionalShadowData.viewBiasMax, s_Styles.viewBiasMax);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.Slider(m_AdditionalShadowData.normalBiasMin, 0.0f, 5.0f, s_Styles.normalBiasMin);
                if (EditorGUI.EndChangeCheck())
                {
                    // Link min to max and don't expose normalBiasScale (useless when min == max)
                    m_AdditionalShadowData.normalBiasMax.floatValue = m_AdditionalShadowData.normalBiasMin.floatValue;
                }
                //EditorGUILayout.PropertyField(m_AdditionalShadowData.normalBiasMax, s_Styles.normalBiasMax);
                //EditorGUILayout.PropertyField(m_AdditionalShadowData.normalBiasScale, s_Styles.normalBiasScale);
                //EditorGUILayout.PropertyField(m_AdditionalShadowData.sampleBiasScale, s_Styles.sampleBiasScale);
                EditorGUILayout.PropertyField(m_AdditionalShadowData.edgeLeakFixup, s_Styles.edgeLeakFixup);
                if (m_AdditionalShadowData.edgeLeakFixup.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_AdditionalShadowData.edgeToleranceNormal, s_Styles.edgeToleranceNormal);
                    EditorGUILayout.Slider(m_AdditionalShadowData.edgeTolerance, 0.0f, 1.0f, s_Styles.edgeTolerance);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
        }

        void DrawLowShadowSettings()
        {
            // Currently there is nothing to display here
        }

        void DrawMediumShadowSettings()
        {

        }

        void DrawHighShadowSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hight Quality Settings", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_AdditionalLightData.shadowSoftness, s_Styles.shadowSoftness);
                EditorGUILayout.PropertyField(m_AdditionalLightData.blockerSampleCount, s_Styles.blockerSampleCount);
                EditorGUILayout.PropertyField(m_AdditionalLightData.filterSampleCount, s_Styles.filterSampleCount);
            }
        }

        // Internal utilities
        void ApplyAdditionalComponentsVisibility(bool hide)
        {
            // UX team decided that we should always show component in inspector.
            // However already authored scene save this settings, so force the component to be visible
            // var flags = hide ? HideFlags.HideInInspector : HideFlags.None;
            var flags = HideFlags.None;

            foreach (var t in m_SerializedAdditionalLightData.targetObjects)
                ((HDAdditionalLightData)t).hideFlags = flags;

            foreach (var t in m_SerializedAdditionalShadowData.targetObjects)
                ((AdditionalShadowData)t).hideFlags = flags;
        }

        void ResolveLightShape()
        {
            var type = settings.lightType;

            // Special case for multi-selection: don't resolve light shape or it'll corrupt lights
            if (type.hasMultipleDifferentValues
                || m_AdditionalLightData.lightTypeExtent.hasMultipleDifferentValues)
            {
                m_LightShape = (LightShape)(-1);
                return;
            }

            var lightTypeExtent = (LightTypeExtent)m_AdditionalLightData.lightTypeExtent.enumValueIndex;

            if (lightTypeExtent == LightTypeExtent.Punctual)
            {
                switch ((LightType)type.enumValueIndex)
                {
                    case LightType.Directional:
                        m_LightShape = LightShape.Directional;
                        break;
                    case LightType.Point:
                        m_LightShape = LightShape.Point;
                        break;
                    case LightType.Spot:
                        m_LightShape = LightShape.Spot;
                        break;
                }
            }
            else
            {
                switch (lightTypeExtent)
                {
                    case LightTypeExtent.Rectangle:
                        m_LightShape = LightShape.Rectangle;
                        break;
                    case LightTypeExtent.Line:
                        m_LightShape = LightShape.Line;
                        break;
                }
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmoForHDAdditionalLightData(HDAdditionalLightData src, GizmoType gizmoType)
        {
            bool selected = (gizmoType & GizmoType.Selected) != 0;

            var light = src.gameObject.GetComponent<Light>();
            Color previousColor = Gizmos.color;
            Gizmos.color = light.enabled ? LightEditor.kGizmoLight : LightEditor.kGizmoDisabledLight;

            if (selected)
            {
                // Trace a ray down to better locate the light location
                Ray ray = new Ray(src.gameObject.transform.position, Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                    using (new Handles.DrawingScope(Color.green))
                    {
                        Handles.DrawLine(src.gameObject.transform.position, hit.point);
                        Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);
                    }

                    Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                    using (new Handles.DrawingScope(Color.red))
                    {
                        Handles.DrawLine(src.gameObject.transform.position, hit.point);
                        Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);
                    }
                }
            }
            Handles.zTest = CompareFunction.Always;
            Gizmos.color = previousColor;
            
            if (Selection.Contains(light.gameObject))
            {
                ((HDLightEditor)Editor.CreateEditor(light)).OnSceneGUI();
            }
        }
    }
}
