using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(ColorGrading))]
    internal sealed class ColorGradingEditor : PostProcessEffectEditor<ColorGrading>
    {
        SerializedParameterOverride m_GradingMode;

        static GUIContent[] s_Curves =
        {
            new GUIContent("Master"),
            new GUIContent("Red"),
            new GUIContent("Green"),
            new GUIContent("Blue"),
            new GUIContent("Hue Vs Hue"),
            new GUIContent("Hue Vs Sat"),
            new GUIContent("Sat Vs Sat"),
            new GUIContent("Lum Vs Sat")
        };

        SerializedParameterOverride m_ExternalLut;

        SerializedParameterOverride m_Tonemapper;
        SerializedParameterOverride m_ToneCurveToeStrength;
        SerializedParameterOverride m_ToneCurveToeLength;
        SerializedParameterOverride m_ToneCurveShoulderStrength;
        SerializedParameterOverride m_ToneCurveShoulderLength;
        SerializedParameterOverride m_ToneCurveShoulderAngle;
        SerializedParameterOverride m_ToneCurveGamma;

        SerializedParameterOverride m_LdrLut;
        SerializedParameterOverride m_LdrLutContribution;

        SerializedParameterOverride m_Temperature;
        SerializedParameterOverride m_Tint;

        SerializedParameterOverride m_ColorFilter;
        SerializedParameterOverride m_HueShift;
        SerializedParameterOverride m_Saturation;
        SerializedParameterOverride m_Brightness;
        SerializedParameterOverride m_PostExposure;
        SerializedParameterOverride m_Contrast;

        SerializedParameterOverride m_MixerRedOutRedIn;
        SerializedParameterOverride m_MixerRedOutGreenIn;
        SerializedParameterOverride m_MixerRedOutBlueIn;

        SerializedParameterOverride m_MixerGreenOutRedIn;
        SerializedParameterOverride m_MixerGreenOutGreenIn;
        SerializedParameterOverride m_MixerGreenOutBlueIn;

        SerializedParameterOverride m_MixerBlueOutRedIn;
        SerializedParameterOverride m_MixerBlueOutGreenIn;
        SerializedParameterOverride m_MixerBlueOutBlueIn;

        SerializedParameterOverride m_Lift;
        SerializedParameterOverride m_Gamma;
        SerializedParameterOverride m_Gain;

        SerializedParameterOverride m_MasterCurve;
        SerializedParameterOverride m_RedCurve;
        SerializedParameterOverride m_GreenCurve;
        SerializedParameterOverride m_BlueCurve;

        SerializedParameterOverride m_HueVsHueCurve;
        SerializedParameterOverride m_HueVsSatCurve;
        SerializedParameterOverride m_SatVsSatCurve;
        SerializedParameterOverride m_LumVsSatCurve;

        // Internal references to the actual animation curves
        // Needed for the curve editor
        SerializedProperty m_RawMasterCurve;
        SerializedProperty m_RawRedCurve;
        SerializedProperty m_RawGreenCurve;
        SerializedProperty m_RawBlueCurve;

        SerializedProperty m_RawHueVsHueCurve;
        SerializedProperty m_RawHueVsSatCurve;
        SerializedProperty m_RawSatVsSatCurve;
        SerializedProperty m_RawLumVsSatCurve;

        CurveEditor m_CurveEditor;
        Dictionary<SerializedProperty, Color> m_CurveDict;

        // Custom tone curve drawing
        const int k_CustomToneCurveResolution = 48;
        const float k_CustomToneCurveRangeY = 1.025f;
        readonly Vector3[] m_RectVertices = new Vector3[4];
        readonly Vector3[] m_LineVertices = new Vector3[2];
        readonly Vector3[] m_CurveVertices = new Vector3[k_CustomToneCurveResolution];
        Rect m_CustomToneCurveRect;
        readonly HableCurve m_HableCurve = new HableCurve();

        public override void OnEnable()
        {
            m_GradingMode = FindParameterOverride(x => x.gradingMode);

            m_ExternalLut = FindParameterOverride(x => x.externalLut);

            m_Tonemapper = FindParameterOverride(x => x.tonemapper);
            m_ToneCurveToeStrength = FindParameterOverride(x => x.toneCurveToeStrength);
            m_ToneCurveToeLength = FindParameterOverride(x => x.toneCurveToeLength);
            m_ToneCurveShoulderStrength = FindParameterOverride(x => x.toneCurveShoulderStrength);
            m_ToneCurveShoulderLength = FindParameterOverride(x => x.toneCurveShoulderLength);
            m_ToneCurveShoulderAngle = FindParameterOverride(x => x.toneCurveShoulderAngle);
            m_ToneCurveGamma = FindParameterOverride(x => x.toneCurveGamma);

            m_LdrLut = FindParameterOverride(x => x.ldrLut);
            m_LdrLutContribution = FindParameterOverride(x => x.ldrLutContribution);

            m_Temperature = FindParameterOverride(x => x.temperature);
            m_Tint = FindParameterOverride(x => x.tint);

            m_ColorFilter = FindParameterOverride(x => x.colorFilter);
            m_HueShift = FindParameterOverride(x => x.hueShift);
            m_Saturation = FindParameterOverride(x => x.saturation);
            m_Brightness = FindParameterOverride(x => x.brightness);
            m_PostExposure = FindParameterOverride(x => x.postExposure);
            m_Contrast = FindParameterOverride(x => x.contrast);

            m_MixerRedOutRedIn = FindParameterOverride(x => x.mixerRedOutRedIn);
            m_MixerRedOutGreenIn = FindParameterOverride(x => x.mixerRedOutGreenIn);
            m_MixerRedOutBlueIn = FindParameterOverride(x => x.mixerRedOutBlueIn);

            m_MixerGreenOutRedIn = FindParameterOverride(x => x.mixerGreenOutRedIn);
            m_MixerGreenOutGreenIn = FindParameterOverride(x => x.mixerGreenOutGreenIn);
            m_MixerGreenOutBlueIn = FindParameterOverride(x => x.mixerGreenOutBlueIn);

            m_MixerBlueOutRedIn = FindParameterOverride(x => x.mixerBlueOutRedIn);
            m_MixerBlueOutGreenIn = FindParameterOverride(x => x.mixerBlueOutGreenIn);
            m_MixerBlueOutBlueIn = FindParameterOverride(x => x.mixerBlueOutBlueIn);

            m_Lift = FindParameterOverride(x => x.lift);
            m_Gamma = FindParameterOverride(x => x.gamma);
            m_Gain = FindParameterOverride(x => x.gain);

            m_MasterCurve = FindParameterOverride(x => x.masterCurve);
            m_RedCurve = FindParameterOverride(x => x.redCurve);
            m_GreenCurve = FindParameterOverride(x => x.greenCurve);
            m_BlueCurve = FindParameterOverride(x => x.blueCurve);

            m_HueVsHueCurve = FindParameterOverride(x => x.hueVsHueCurve);
            m_HueVsSatCurve = FindParameterOverride(x => x.hueVsSatCurve);
            m_SatVsSatCurve = FindParameterOverride(x => x.satVsSatCurve);
            m_LumVsSatCurve = FindParameterOverride(x => x.lumVsSatCurve);

            m_RawMasterCurve = FindProperty(x => x.masterCurve.value.curve);
            m_RawRedCurve = FindProperty(x => x.redCurve.value.curve);
            m_RawGreenCurve = FindProperty(x => x.greenCurve.value.curve);
            m_RawBlueCurve = FindProperty(x => x.blueCurve.value.curve);

            m_RawHueVsHueCurve = FindProperty(x => x.hueVsHueCurve.value.curve);
            m_RawHueVsSatCurve = FindProperty(x => x.hueVsSatCurve.value.curve);
            m_RawSatVsSatCurve = FindProperty(x => x.satVsSatCurve.value.curve);
            m_RawLumVsSatCurve = FindProperty(x => x.lumVsSatCurve.value.curve);

            m_CurveEditor = new CurveEditor();
            m_CurveDict = new Dictionary<SerializedProperty, Color>();

            // Prepare the curve editor
            SetupCurve(m_RawMasterCurve, new Color(1f, 1f, 1f), 2, false);
            SetupCurve(m_RawRedCurve, new Color(1f, 0f, 0f), 2, false);
            SetupCurve(m_RawGreenCurve, new Color(0f, 1f, 0f), 2, false);
            SetupCurve(m_RawBlueCurve, new Color(0f, 0.5f, 1f), 2, false);
            SetupCurve(m_RawHueVsHueCurve, new Color(1f, 1f, 1f), 0, true);
            SetupCurve(m_RawHueVsSatCurve, new Color(1f, 1f, 1f), 0, true);
            SetupCurve(m_RawSatVsSatCurve, new Color(1f, 1f, 1f), 0, false);
            SetupCurve(m_RawLumVsSatCurve, new Color(1f, 1f, 1f), 0, false);
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_GradingMode);

            var gradingMode = (GradingMode)m_GradingMode.value.intValue;

            // Check if we're in gamma or linear and display a warning if we're trying to do hdr
            // color grading while being in gamma mode
            if (gradingMode != GradingMode.LowDefinitionRange)
            {
                if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                    EditorGUILayout.HelpBox("ColorSpace in project settings is set to Gamma, HDR color grading won't look correct. Switch to Linear or use LDR color grading mode instead.", MessageType.Warning);
            }

            if (m_GradingMode.overrideState.boolValue && gradingMode == GradingMode.External)
            {
                if (!SystemInfo.supports3DRenderTextures || !SystemInfo.supportsComputeShaders)
                    EditorGUILayout.HelpBox("HDR color grading requires compute shader & 3D render texture support.", MessageType.Warning);
            }

            if (gradingMode == GradingMode.LowDefinitionRange)
                DoStandardModeGUI(false);
            else if (gradingMode == GradingMode.HighDefinitionRange)
                DoStandardModeGUI(true);
            else if (gradingMode == GradingMode.External)
                DoExternalModeGUI();

            EditorGUILayout.Space();
        }

        void SetupCurve(SerializedProperty prop, Color color, uint minPointCount, bool loop)
        {
            var state = CurveEditor.CurveState.defaultState;
            state.color = color;
            state.visible = false;
            state.minPointCount = minPointCount;
            state.onlyShowHandlesOnSelection = true;
            state.zeroKeyConstantValue = 0.5f;
            state.loopInBounds = loop;
            m_CurveEditor.Add(prop, state);
            m_CurveDict.Add(prop, color);
        }

        void DoExternalModeGUI()
        {
            PropertyField(m_ExternalLut);

            var lut = m_ExternalLut.value.objectReferenceValue;
            if (lut != null)
            {
                if (lut.GetType() == typeof(Texture3D))
                {
                    var o = (Texture3D)lut;
                    if (o.width == o.height && o.height == o.depth)
                        return;
                }
                else if (lut.GetType() == typeof(RenderTexture))
                {
                    var o = (RenderTexture)lut;
                    if (o.width == o.height && o.height == o.volumeDepth)
                        return;
                }

                EditorGUILayout.HelpBox("Custom LUTs have to be log-encoded 3D textures or 3D render textures with cube format.", MessageType.Warning);
            }
        }

        void DoStandardModeGUI(bool hdr)
        {
            if (!hdr)
            {
                PropertyField(m_LdrLut);
                PropertyField(m_LdrLutContribution);

                var lut = (target as ColorGrading).ldrLut.value;
                CheckLutImportSettings(lut);
            }

            if (hdr)
            {
                EditorGUILayout.Space();
                EditorUtilities.DrawHeaderLabel("Tonemapping");
                PropertyField(m_Tonemapper);

                if (m_Tonemapper.value.intValue == (int)Tonemapper.Custom)
                {
                    DrawCustomToneCurve();
                    PropertyField(m_ToneCurveToeStrength);
                    PropertyField(m_ToneCurveToeLength);
                    PropertyField(m_ToneCurveShoulderStrength);
                    PropertyField(m_ToneCurveShoulderLength);
                    PropertyField(m_ToneCurveShoulderAngle);
                    PropertyField(m_ToneCurveGamma);
                }
            }

            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("White Balance");

            PropertyField(m_Temperature);
            PropertyField(m_Tint);

            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("Tone");

            if (hdr)
                PropertyField(m_PostExposure);

            PropertyField(m_ColorFilter);
            PropertyField(m_HueShift);
            PropertyField(m_Saturation);

            if (!hdr)
                PropertyField(m_Brightness);

            PropertyField(m_Contrast);

            EditorGUILayout.Space();
            int currentChannel = GlobalSettings.currentChannelMixer;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Channel Mixer", GUIStyle.none, Styling.headerLabel);

                EditorGUI.BeginChangeCheck();
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayoutUtility.GetRect(9f, 18f, GUILayout.ExpandWidth(false)); // Dirty hack to do proper right column alignement
                        if (GUILayout.Toggle(currentChannel == 0, EditorUtilities.GetContent("Red|Red output channel."), EditorStyles.miniButtonLeft)) currentChannel = 0;
                        if (GUILayout.Toggle(currentChannel == 1, EditorUtilities.GetContent("Green|Green output channel."), EditorStyles.miniButtonMid)) currentChannel = 1;
                        if (GUILayout.Toggle(currentChannel == 2, EditorUtilities.GetContent("Blue|Blue output channel."), EditorStyles.miniButtonRight)) currentChannel = 2;
                    }
                }
                if (EditorGUI.EndChangeCheck())
                    GUI.FocusControl(null);
            }

            GlobalSettings.currentChannelMixer = currentChannel;

            if (currentChannel == 0)
            {
                PropertyField(m_MixerRedOutRedIn);
                PropertyField(m_MixerRedOutGreenIn);
                PropertyField(m_MixerRedOutBlueIn);
            }
            else if (currentChannel == 1)
            {
                PropertyField(m_MixerGreenOutRedIn);
                PropertyField(m_MixerGreenOutGreenIn);
                PropertyField(m_MixerGreenOutBlueIn);
            }
            else
            {
                PropertyField(m_MixerBlueOutRedIn);
                PropertyField(m_MixerBlueOutGreenIn);
                PropertyField(m_MixerBlueOutBlueIn);
            }

            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("Trackballs");

            using (new EditorGUILayout.HorizontalScope())
            {
                PropertyField(m_Lift);
                GUILayout.Space(4f);
                PropertyField(m_Gamma);
                GUILayout.Space(4f);
                PropertyField(m_Gain);
            }

            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("Grading Curves");

            DoCurvesGUI(hdr);
        }

        void CheckLutImportSettings(Texture lut)
        {
            if (lut != null)
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(lut)) as TextureImporter;

                // Fails when using an internal texture as you can't change import settings on
                // builtin resources, thus the check for null
                if (importer != null)
                {
                    bool valid = importer.anisoLevel == 0
                        && importer.mipmapEnabled == false
                        && importer.sRGBTexture == false
                        && importer.textureCompression == TextureImporterCompression.Uncompressed
                        && importer.wrapMode == TextureWrapMode.Clamp;

                    if (!valid)
                        EditorUtilities.DrawFixMeBox("Invalid LUT import settings.", () => SetLutImportSettings(importer));
                }

                if (lut.width != lut.height * lut.height)
                {
                    EditorGUILayout.HelpBox("The Lookup Texture size is invalid. Width should be Height * Height.", MessageType.Error);
                }
            }
        }

        void SetLutImportSettings(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.anisoLevel = 0;
            importer.sRGBTexture = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        void DrawCustomToneCurve()
        {
            EditorGUILayout.Space();

            // Reserve GUI space
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                m_CustomToneCurveRect = GUILayoutUtility.GetRect(128, 80);
            }

            if (Event.current.type != EventType.Repaint)
                return;

            // Prepare curve data
            float toeStrength = m_ToneCurveToeStrength.value.floatValue;
            float toeLength = m_ToneCurveToeLength.value.floatValue;
            float shoulderStrength = m_ToneCurveShoulderStrength.value.floatValue;
            float shoulderLength = m_ToneCurveShoulderLength.value.floatValue;
            float shoulderAngle = m_ToneCurveShoulderAngle.value.floatValue;
            float gamma = m_ToneCurveGamma.value.floatValue;
            m_HableCurve.Init(
                toeStrength,
                toeLength,
                shoulderStrength,
                shoulderLength,
                shoulderAngle,
                gamma
            );

            float endPoint = m_HableCurve.whitePoint;

            // Background
            m_RectVertices[0] = PointInRect(0f, 0f, endPoint);
            m_RectVertices[1] = PointInRect(endPoint, 0f, endPoint);
            m_RectVertices[2] = PointInRect(endPoint, k_CustomToneCurveRangeY, endPoint);
            m_RectVertices[3] = PointInRect(0f, k_CustomToneCurveRangeY, endPoint);
            Handles.DrawSolidRectangleWithOutline(m_RectVertices, Color.white * 0.1f, Color.white * 0.4f);

            // Vertical guides
            if (endPoint < m_CustomToneCurveRect.width / 3)
            {
                int steps = Mathf.CeilToInt(endPoint);
                for (var i = 1; i < steps; i++)
                    DrawLine(i, 0, i, k_CustomToneCurveRangeY, 0.4f, endPoint);
            }

            // Label
            Handles.Label(m_CustomToneCurveRect.position + Vector2.right, "Custom Tone Curve", EditorStyles.miniLabel);

            // Draw the acual curve
            var vcount = 0;
            while (vcount < k_CustomToneCurveResolution)
            {
                float x = endPoint * vcount / (k_CustomToneCurveResolution - 1);
                float y = m_HableCurve.Eval(x);

                if (y < k_CustomToneCurveRangeY)
                {
                    m_CurveVertices[vcount++] = PointInRect(x, y, endPoint);
                }
                else
                {
                    if (vcount > 1)
                    {
                        // Extend the last segment to the top edge of the rect.
                        var v1 = m_CurveVertices[vcount - 2];
                        var v2 = m_CurveVertices[vcount - 1];
                        var clip = (m_CustomToneCurveRect.y - v1.y) / (v2.y - v1.y);
                        m_CurveVertices[vcount - 1] = v1 + (v2 - v1) * clip;
                    }
                    break;
                }
            }

            if (vcount > 1)
            {
                Handles.color = Color.white * 0.9f;
                Handles.DrawAAPolyLine(2f, vcount, m_CurveVertices);
            }
        }

        void DrawLine(float x1, float y1, float x2, float y2, float grayscale, float rangeX)
        {
            m_LineVertices[0] = PointInRect(x1, y1, rangeX);
            m_LineVertices[1] = PointInRect(x2, y2, rangeX);
            Handles.color = Color.white * grayscale;
            Handles.DrawAAPolyLine(2f, m_LineVertices);
        }

        Vector3 PointInRect(float x, float y, float rangeX)
        {
            x = Mathf.Lerp(m_CustomToneCurveRect.x, m_CustomToneCurveRect.xMax, x / rangeX);
            y = Mathf.Lerp(m_CustomToneCurveRect.yMax, m_CustomToneCurveRect.y, y / k_CustomToneCurveRangeY);
            return new Vector3(x, y, 0);
        }

        void ResetVisibleCurves()
        {
            foreach (var curve in m_CurveDict)
            {
                var state = m_CurveEditor.GetCurveState(curve.Key);
                state.visible = false;
                m_CurveEditor.SetCurveState(curve.Key, state);
            }
        }

        void SetCurveVisible(SerializedProperty rawProp, SerializedProperty overrideProp)
        {
            var state = m_CurveEditor.GetCurveState(rawProp);
            state.visible = true;
            state.editable = overrideProp.boolValue;
            m_CurveEditor.SetCurveState(rawProp, state);
        }

        void CurveOverrideToggle(SerializedProperty overrideProp)
        {
            overrideProp.boolValue = GUILayout.Toggle(overrideProp.boolValue, EditorUtilities.GetContent("Override"), EditorStyles.toolbarButton);
        }

        static Material s_MaterialGrid;

        void DoCurvesGUI(bool hdr)
        {
            EditorGUILayout.Space();
            ResetVisibleCurves();

            using (new EditorGUI.DisabledGroupScope(serializedObject.isEditingMultipleObjects))
            {
                int curveEditingId = 0;
                SerializedProperty currentCurveRawProp = null;

                // Top toolbar
                using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    curveEditingId = DoCurveSelectionPopup(GlobalSettings.currentCurve, hdr);
                    curveEditingId = Mathf.Clamp(curveEditingId, hdr ? 4 : 0, 7);

                    EditorGUILayout.Space();

                    switch (curveEditingId)
                    {
                        case 0:
                            CurveOverrideToggle(m_MasterCurve.overrideState);
                            SetCurveVisible(m_RawMasterCurve, m_MasterCurve.overrideState);
                            currentCurveRawProp = m_RawMasterCurve;
                            break;
                        case 1:
                            CurveOverrideToggle(m_RedCurve.overrideState);
                            SetCurveVisible(m_RawRedCurve, m_RedCurve.overrideState);
                            currentCurveRawProp = m_RawRedCurve;
                            break;
                        case 2:
                            CurveOverrideToggle(m_GreenCurve.overrideState);
                            SetCurveVisible(m_RawGreenCurve, m_GreenCurve.overrideState);
                            currentCurveRawProp = m_RawGreenCurve;
                            break;
                        case 3:
                            CurveOverrideToggle(m_BlueCurve.overrideState);
                            SetCurveVisible(m_RawBlueCurve, m_BlueCurve.overrideState);
                            currentCurveRawProp = m_RawBlueCurve;
                            break;
                        case 4:
                            CurveOverrideToggle(m_HueVsHueCurve.overrideState);
                            SetCurveVisible(m_RawHueVsHueCurve, m_HueVsHueCurve.overrideState);
                            currentCurveRawProp = m_RawHueVsHueCurve;
                            break;
                        case 5:
                            CurveOverrideToggle(m_HueVsSatCurve.overrideState);
                            SetCurveVisible(m_RawHueVsSatCurve, m_HueVsSatCurve.overrideState);
                            currentCurveRawProp = m_RawHueVsSatCurve;
                            break;
                        case 6:
                            CurveOverrideToggle(m_SatVsSatCurve.overrideState);
                            SetCurveVisible(m_RawSatVsSatCurve, m_SatVsSatCurve.overrideState);
                            currentCurveRawProp = m_RawSatVsSatCurve;
                            break;
                        case 7:
                            CurveOverrideToggle(m_LumVsSatCurve.overrideState);
                            SetCurveVisible(m_RawLumVsSatCurve, m_LumVsSatCurve.overrideState);
                            currentCurveRawProp = m_RawLumVsSatCurve;
                            break;
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
                    {
                        switch (curveEditingId)
                        {
                            case 0: m_RawMasterCurve.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                                break;
                            case 1: m_RawRedCurve.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                                break;
                            case 2: m_RawGreenCurve.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                                break;
                            case 3: m_RawBlueCurve.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                                break;
                            case 4: m_RawHueVsHueCurve.animationCurveValue = new AnimationCurve();
                                break;
                            case 5: m_RawHueVsSatCurve.animationCurveValue = new AnimationCurve();
                                break;
                            case 6: m_RawSatVsSatCurve.animationCurveValue = new AnimationCurve();
                                break;
                            case 7: m_RawLumVsSatCurve.animationCurveValue = new AnimationCurve();
                                break;
                        }
                    }

                    GlobalSettings.currentCurve = curveEditingId;
                }

                // Curve area
                var settings = m_CurveEditor.settings;
                var rect = GUILayoutUtility.GetAspectRect(2f);
                var innerRect = settings.padding.Remove(rect);

                if (Event.current.type == EventType.Repaint)
                {
                    // Background
                    EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

                    if (curveEditingId == 4 || curveEditingId == 5)
                        DrawBackgroundTexture(innerRect, 0);
                    else if (curveEditingId == 6 || curveEditingId == 7)
                        DrawBackgroundTexture(innerRect, 1);

                    // Bounds
                    Handles.color = Color.white * (GUI.enabled ? 1f : 0.5f);
                    Handles.DrawSolidRectangleWithOutline(innerRect, Color.clear, new Color(0.8f, 0.8f, 0.8f, 0.5f));

                    // Grid setup
                    Handles.color = new Color(1f, 1f, 1f, 0.05f);
                    int hLines = (int)Mathf.Sqrt(innerRect.width);
                    int vLines = (int)(hLines / (innerRect.width / innerRect.height));

                    // Vertical grid
                    int gridOffset = Mathf.FloorToInt(innerRect.width / hLines);
                    int gridPadding = ((int)(innerRect.width) % hLines) / 2;

                    for (int i = 1; i < hLines; i++)
                    {
                        var offset = i * Vector2.right * gridOffset;
                        offset.x += gridPadding;
                        Handles.DrawLine(innerRect.position + offset, new Vector2(innerRect.x, innerRect.yMax - 1) + offset);
                    }

                    // Horizontal grid
                    gridOffset = Mathf.FloorToInt(innerRect.height / vLines);
                    gridPadding = ((int)(innerRect.height) % vLines) / 2;

                    for (int i = 1; i < vLines; i++)
                    {
                        var offset = i * Vector2.up * gridOffset;
                        offset.y += gridPadding;
                        Handles.DrawLine(innerRect.position + offset, new Vector2(innerRect.xMax - 1, innerRect.y) + offset);
                    }
                }

                // Curve editor
                if (m_CurveEditor.OnGUI(rect))
                {
                    Repaint();
                    GUI.changed = true;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    // Borders
                    Handles.color = Color.black;
                    Handles.DrawLine(new Vector2(rect.x, rect.y - 18f), new Vector2(rect.xMax, rect.y - 18f));
                    Handles.DrawLine(new Vector2(rect.x, rect.y - 19f), new Vector2(rect.x, rect.yMax));
                    Handles.DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
                    Handles.DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.y - 18f));

                    bool editable = m_CurveEditor.GetCurveState(currentCurveRawProp).editable;
                    string editableString = editable ? string.Empty : "(Not Overriding)\n";

                    // Selection info
                    var selection = m_CurveEditor.GetSelection();
                    var infoRect = innerRect;
                    infoRect.x += 5f;
                    infoRect.width = 100f;
                    infoRect.height = 30f;

                    if (selection.curve != null && selection.keyframeIndex > -1)
                    {
                        var key = selection.keyframe.Value;
                        GUI.Label(infoRect, string.Format("{0}\n{1}", key.time.ToString("F3"), key.value.ToString("F3")), Styling.preLabel);
                    }
                    else
                    {
                        GUI.Label(infoRect, editableString, Styling.preLabel);
                    }
                }
            }
        }

        void DrawBackgroundTexture(Rect rect, int pass)
        {
            if (s_MaterialGrid == null)
                s_MaterialGrid = new Material(Shader.Find("Hidden/PostProcessing/Editor/CurveGrid")) { hideFlags = HideFlags.HideAndDontSave };

            float scale = EditorGUIUtility.pixelsPerPoint;

        #if UNITY_2018_1_OR_NEWER
            const RenderTextureReadWrite kReadWrite = RenderTextureReadWrite.sRGB;
        #else
            const RenderTextureReadWrite kReadWrite = RenderTextureReadWrite.Linear;
        #endif

            var oldRt = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(Mathf.CeilToInt(rect.width * scale), Mathf.CeilToInt(rect.height * scale), 0, RenderTextureFormat.ARGB32, kReadWrite);
            s_MaterialGrid.SetFloat("_DisabledState", GUI.enabled ? 1f : 0.5f);
            s_MaterialGrid.SetFloat("_PixelScaling", EditorGUIUtility.pixelsPerPoint);

            Graphics.Blit(null, rt, s_MaterialGrid, pass);
            RenderTexture.active = oldRt;

            GUI.DrawTexture(rect, rt);
            RenderTexture.ReleaseTemporary(rt);
        }

        int DoCurveSelectionPopup(int id, bool hdr)
        {
            GUILayout.Label(s_Curves[id], EditorStyles.toolbarPopup, GUILayout.MaxWidth(150f));

            var lastRect = GUILayoutUtility.GetLastRect();
            var e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && lastRect.Contains(e.mousePosition))
            {
                var menu = new GenericMenu();

                for (int i = 0; i < s_Curves.Length; i++)
                {
                    if (i == 4)
                        menu.AddSeparator("");

                    if (hdr && i < 4)
                        menu.AddDisabledItem(s_Curves[i]);
                    else
                    {
                        int current = i; // Capture local for closure
                        menu.AddItem(s_Curves[i], current == id, () => GlobalSettings.currentCurve = current);
                    }
                }

                menu.DropDown(new Rect(lastRect.xMin, lastRect.yMax, 1f, 1f));
            }

            return id;
        }
    }
}
