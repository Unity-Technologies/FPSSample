#define WORKAROUND_TIMELINE

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.VFX;

using UnityEditor.Experimental.VFX;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX
{
#if WORKAROUND_TIMELINE
    class FakeObject : MonoBehaviour
#else
    class FakeObject : ScriptableObject
#endif
    {
        public float aFloat = 0.0f;
        public Vector2 aVector2 = Vector2.zero;
        public Vector3 aVector3 = Vector3.zero;
        public Vector4 aVector4 = Vector4.zero;
        public Color aColor = Color.black;
        public UnityObject anObject = null;
        public AnimationCurve anAnimationCurve = null;
        public Gradient aGradient = null;
        public int anInt = 0;
        public long anUInt = 0u;
        public bool aBool = false;
    }


    public static class VisualEffectControl
    {
        public static void ControlStop(this VisualEffect effect)
        {
            effect.Reinit();
            effect.pause = true;
        }

        public static void ControlPlayPause(this VisualEffect effect)
        {
            effect.pause = !effect.pause;
        }

        public static void ControlStep(this VisualEffect effect)
        {
            effect.pause = true;
            effect.AdvanceOneFrame();
        }

        public static void ControlRestart(this VisualEffect effect)
        {
            effect.Reinit();
            effect.pause = false;
        }

        public const float minSlider = 1;
        public const float maxSlider = 4000;

        public const float playRateToValue = 100.0f;
        public const float valueToPlayRate = 1.0f / playRateToValue;


        public const float sliderPower = 10;

        public static readonly int[] setPlaybackValues = new int[] { 1, 10, 50, 100, 200, 500, 1000, 4000 };
    }


    //[CustomEditor(typeof(VisualEffect))]
    public class VisualEffectEditor : Editor
    {
        protected SerializedProperty m_VisualEffectAsset;
        SerializedProperty m_ReseedOnPlay;
        SerializedProperty m_RandomSeed;
        SerializedProperty m_VFXPropertySheet;

#if ! WORKAROUND_TIMELINE
        static FakeObject s_FakeObjectCache;
#endif
        static SerializedObject s_FakeObjectSerializedCache;

        static List<VisualEffectEditor> s_AllEditors = new List<VisualEffectEditor>();

        private VFXRenderer[] m_Renderers;
        private SerializedObject m_SerializedRenderers;
        private SerializedProperty m_RendererTransparentPriority;
        private SerializedProperty m_RendererRenderingLayerMask;

        static public void RepaintAllEditors()
        {
            foreach (var ed in s_AllEditors)
            {
                ed.Repaint();
            }
        }

        protected void OnEnable()
        {
            s_AllEditors.Add(this);
            m_RandomSeed = serializedObject.FindProperty("m_StartSeed");
            m_ReseedOnPlay = serializedObject.FindProperty("m_ResetSeedOnPlay");
            m_VisualEffectAsset = serializedObject.FindProperty("m_Asset");
            m_VFXPropertySheet = serializedObject.FindProperty("m_PropertySheet");

            m_Renderers = targets.Cast<Component>().Select(t => t.GetComponent<VFXRenderer>()).ToArray();
            m_SerializedRenderers = new SerializedObject(m_Renderers);
            m_RendererTransparentPriority = m_SerializedRenderers.FindProperty("m_RendererPriority");
            m_RendererRenderingLayerMask = m_SerializedRenderers.FindProperty("m_RenderingLayerMask");

#if WORKAROUND_TIMELINE
            s_FakeObjectSerializedCache = new SerializedObject(target);
#endif
        }

        protected void OnDisable()
        {
            VisualEffect effect = ((VisualEffect)targets[0]);
            if (effect != null)
            {
                effect.pause = false;
                effect.playRate = 1.0f;
            }
            s_AllEditors.Remove(this);
        }

        protected const float overrideWidth = 16;

        SerializedProperty GetFakeProperty(ref VFXParameterInfo parameter)
        {
            if (parameter.defaultValue == null)
                return null;
            Type type = parameter.defaultValue.type;
            if (type == null)
                return null;

            if (typeof(float) == type)
            {
                return s_FakeObjectSerializedCache.FindProperty("aFloat");
            }
            else if (typeof(Vector2) == type)
            {
                return s_FakeObjectSerializedCache.FindProperty("aVector2");
            }
            else if (typeof(Vector3) == type)
            {
                return s_FakeObjectSerializedCache.FindProperty("aVector3");
            }
            else if (typeof(Vector4) == type)
            {
                return s_FakeObjectSerializedCache.FindProperty("aVector4");
            }
            else if (typeof(Color) == type)
            {
                return s_FakeObjectSerializedCache.FindProperty("aColor");
            }
            else if (typeof(Gradient) == type)
            {
                return s_FakeObjectSerializedCache.FindProperty("aGradient");
            }
            else if (typeof(AnimationCurve) == type)
            {
                return s_FakeObjectSerializedCache.FindProperty("anAnimationCurve");
            }
            else if (typeof(UnityObject).IsAssignableFrom(type))
            {
                return s_FakeObjectSerializedCache.FindProperty("anObject");
            }
            else if (typeof(int) == type)
            {
                return s_FakeObjectSerializedCache.FindProperty("anInt");
            }
            else if (typeof(uint) == type)
            {
                return s_FakeObjectSerializedCache.FindProperty("anUInt");
            }
            else if (typeof(bool) == type)
            {
                return s_FakeObjectSerializedCache.FindProperty("aBool");
            }

            return null;
        }

        void DisplayProperty(ref VFXParameterInfo parameter, SerializedProperty overrideProperty, SerializedProperty property)
        {
            EditorGUILayout.BeginHorizontal();

            GUIContent nameContent = GetGUIContent(parameter.name, parameter.tooltip);

            EditorGUI.BeginChangeCheck();
            bool result = EditorGUILayout.Toggle(overrideProperty.hasMultipleDifferentValues ? false : overrideProperty.boolValue, overrideProperty.hasMultipleDifferentValues ? Styles.toggleMixedStyle : Styles.toggleStyle, GUILayout.Width(overrideWidth));
            if (EditorGUI.EndChangeCheck())
            {
                overrideProperty.boolValue = result;
            }

            SerializedProperty originalProperty = property;

            if (!overrideProperty.boolValue)
            {
                s_FakeObjectSerializedCache.Update();
                property = s_FakeObjectSerializedCache.FindProperty(originalProperty.propertyPath);
                if (property != null)
                {
                    SetObjectValue(property,parameter.defaultValue.Get());
                }
            }

            if (property != null)
            {
                EditorGUI.BeginChangeCheck();
                if (parameter.min != Mathf.NegativeInfinity && parameter.max != Mathf.Infinity)
                {
                    if (property.propertyType == SerializedPropertyType.Float)
                        EditorGUILayout.Slider(property, parameter.min, parameter.max, nameContent);
                    else
                        EditorGUILayout.IntSlider(property, (int)parameter.min, (int)parameter.max, nameContent);
                }
                else if (parameter.realType == typeof(Color).Name)
                {
                    Vector4 vVal = property.vector4Value;
                    Color c = new Color(vVal.x, vVal.y, vVal.z, vVal.w);
                    c = EditorGUILayout.ColorField(nameContent, c, true, true, true);

                    if (GUI.changed)
                        property.vector4Value = new Vector4(c.r, c.g, c.b, c.a);
                }
                else if (parameter.realType == typeof(Gradient).Name)
                {
                    Gradient newGradient = EditorGUILayout.GradientField(nameContent, property.gradientValue, true);

                    if (GUI.changed)
                        property.gradientValue = newGradient;
                }
                else if (property.propertyType == SerializedPropertyType.Vector4)
                {
                    var oldVal = property.vector4Value;
                    var newVal = EditorGUILayout.Vector4Field(nameContent, oldVal);
                    if (oldVal.x != newVal.x || oldVal.y != newVal.y || oldVal.z != newVal.z || oldVal.w != newVal.w)
                        property.vector4Value = newVal;
                }
                else if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    Type objTyp = typeof(UnityObject);
                    if (!string.IsNullOrEmpty(parameter.realType))
                    {
                        if (parameter.realType.StartsWith("Texture") || parameter.realType.StartsWith("Cubemap"))
                        {
                            objTyp = typeof(Texture);
                        }
                        else if (parameter.realType == "Mesh")
                        {
                            objTyp = typeof(Mesh);
                        }
                    }
                    Rect r = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.objectField, null);
                    EditorGUI.ObjectField(r, property, objTyp, nameContent);
                }
                else
                {
                    EditorGUILayout.PropertyField(property, nameContent, true);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    if (overrideProperty.boolValue == false)
                    {
                        if (originalProperty.propertyType == property.propertyType)
                        {
                            SetObjectValue(originalProperty, GetObjectValue(property));
                        }
                        overrideProperty.boolValue = true;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndHorizontal();
        }

        protected static object GetObjectValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                    return prop.floatValue;
                case SerializedPropertyType.Vector3:
                    return prop.vector3Value;
                case SerializedPropertyType.Vector2:
                    return prop.vector2Value;
                case SerializedPropertyType.Vector4:
                    return prop.vector4Value;
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue;
                case SerializedPropertyType.Integer:
                    return prop.intValue;
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Gradient:
                    return prop.gradientValue;
                case SerializedPropertyType.AnimationCurve:
                    return prop.animationCurveValue;
            }
            return null;
        }

        protected static void SetObjectValue(SerializedProperty prop, object value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                    prop.floatValue = (float)value;
                    return;
                case SerializedPropertyType.Vector3:
                    prop.vector3Value = (Vector3)value;
                    return;
                case SerializedPropertyType.Vector2:
                    prop.vector2Value = (Vector2)value;
                    return;
                case SerializedPropertyType.Vector4:
                    if (value is Color)
                        prop.vector4Value = (Vector4)(Color)value;
                    else
                        prop.vector4Value = (Vector4)value;
                    return;
                case SerializedPropertyType.ObjectReference:
                    prop.objectReferenceValue = (UnityEngine.Object)value;
                    return;
                case SerializedPropertyType.Integer:
                    if( value is uint)
                        prop.longValue = (int)(uint)value;
                    else
                        prop.intValue = (int)value;
                    return;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = (bool)value;
                    return;
                case SerializedPropertyType.Gradient:
                    prop.gradientValue = (Gradient)value;
                    return;
                case SerializedPropertyType.AnimationCurve:
                    prop.animationCurveValue = (AnimationCurve)value;
                    return;
            }
        }

        protected virtual void SceneViewGUICallback(UnityObject target, SceneView sceneView)
        {
            VisualEffect effect = ((VisualEffect)targets[0]);

            var buttonWidth = GUILayout.Width(50);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Stop), buttonWidth))
            {
                effect.ControlStop();
            }
            if (effect.pause)
            {
                if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Play), buttonWidth))
                {
                    effect.ControlPlayPause();
                }
            }
            else
            {
                if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Pause), buttonWidth))
                {
                    effect.ControlPlayPause();
                }
            }


            if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Step), buttonWidth))
            {
                effect.ControlStep();
            }
            if (GUILayout.Button(Contents.GetIcon(Contents.Icon.Restart), buttonWidth))
            {
                effect.ControlRestart();
            }
            GUILayout.EndHorizontal();

            float playRate = effect.playRate * VisualEffectControl.playRateToValue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(Contents.playRate, GUILayout.Width(44));
            playRate = EditorGUILayout.PowerSlider("", playRate, VisualEffectControl.minSlider, VisualEffectControl.maxSlider, VisualEffectControl.sliderPower, GUILayout.Width(124));
            effect.playRate = playRate * VisualEffectControl.valueToPlayRate;

            var eventType = Event.current.type;
            if (EditorGUILayout.DropdownButton(Contents.setPlayRate, FocusType.Passive, GUILayout.Width(36)))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var value in VisualEffectControl.setPlaybackValues)
                {
                    menu.AddItem(EditorGUIUtility.TextContent(string.Format("{0}%", value)), false, SetPlayRate, value);
                }
                var savedEventType = Event.current.type;
                Event.current.type = eventType;
                Rect buttonRect = GUILayoutUtility.GetLastRect();
                Event.current.type = savedEventType;
                menu.DropDown(buttonRect);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            
            GUILayout.Label("Show Bounds", GUILayout.Width(192));

            VisualEffectUtility.renderBounds = EditorGUILayout.Toggle(VisualEffectUtility.renderBounds, GUILayout.Width(18));

            GUILayout.EndHorizontal();
        }

        void SetPlayRate(object value)
        {
            float rate = (float)((int)value)  * VisualEffectControl.valueToPlayRate;
            VisualEffect effect = ((VisualEffect)targets[0]);
            effect.playRate = rate;
        }

        protected virtual void OnSceneGUI()
        {
            SceneViewOverlay.Window(Contents.headerPlayControls, SceneViewGUICallback, (int)SceneViewOverlay.Ordering.ParticleEffect, SceneViewOverlay.WindowDisplayOption.OneWindowPerTitle);
        }

        private VisualEffectAsset m_asset;
        private VFXGraph m_graph;

        protected struct NameNTooltip
        {
            public string name;
            public string tooltip;

            public override int GetHashCode()
            {
                if (name == null)
                    return 0;

                if (tooltip == null)
                    return name.GetHashCode();

                return name.GetHashCode() ^ (tooltip.GetHashCode() << sizeof(int) * 4);
            }
        }


        static Dictionary<NameNTooltip, GUIContent> s_ContentCache = new Dictionary<NameNTooltip, GUIContent>();


        protected GUIContent GetGUIContent(string name, string tooltip = null)
        {
            GUIContent result = null;
            var nnt = new NameNTooltip { name = name, tooltip = tooltip };
            if (!s_ContentCache.TryGetValue(nnt, out result))
            {
                s_ContentCache[nnt] = result = new GUIContent(name, tooltip);
            }

            return result;
        }

        protected virtual void EmptyLineControl(string name, string tooltip, int depth)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(overrideWidth + 4); // the 4 is so that Labels are aligned with elements having an override toggle.
            EditorGUILayout.LabelField(GetGUIContent(name, tooltip));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        protected virtual void EditorModeInspectorButton()
        {
        }

        public static bool ShowHeader(GUIContent nameContent, bool hasHeader, bool hasFooter, bool displayToggle, bool toggleState)
        {
            if (hasHeader)
                GUILayout.Space(Styles.headerTopMargin);
            float height = Styles.categoryHeader.CalcHeight(nameContent, 4000);
            Rect rect = GUILayoutUtility.GetRect(1, height - 1);

            rect.width += rect.x;
            rect.x = 0;
            rect.height++;
            if (Event.current.type == EventType.Repaint)
                Styles.categoryHeader.Draw(rect, nameContent, false, true, true, false);

            bool result = false;
            if (displayToggle)
            {
                rect.x += 2;
                rect.y += 2;
                rect.width -= 2;
                result = EditorGUI.Toggle(rect, toggleState, Styles.toggleStyle);
            }
            if (hasFooter)
                GUILayout.Space(Styles.headerTopMargin);

            return result;
        }

        protected virtual void AssetField()
        {
            var component = (VisualEffect)target;
            EditorGUILayout.PropertyField(m_VisualEffectAsset, Contents.assetPath);
        }

        protected virtual bool SeedField()
        {
            var component = (VisualEffect)target;
            //Seed
            EditorGUI.BeginChangeCheck();
            using (new GUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledGroupScope(m_ReseedOnPlay.boolValue))
                {
                    EditorGUILayout.PropertyField(m_RandomSeed, Contents.randomSeed);
                    if (GUILayout.Button(Contents.setRandomSeed, EditorStyles.miniButton, Styles.MiniButtonWidth))
                    {
                        m_RandomSeed.intValue = UnityEngine.Random.Range(0, int.MaxValue);
                        component.startSeed = (uint)m_RandomSeed.intValue; // As accessors are bypassed with serialized properties...
                    }
                }
            }
            EditorGUILayout.PropertyField(m_ReseedOnPlay, Contents.reseedOnPlay);
            return EditorGUI.EndChangeCheck();
        }

        public override void OnInspectorGUI()
        {
            AssetField();
            bool reinit = SeedField();


            var component = (VisualEffect)target;
            //Display properties only if all the VisualEffects share the same graph
            VisualEffectAsset asset = component.visualEffectAsset;
            if (targets.Length > 1)
            {
                foreach (VisualEffect effect in targets)
                {
                    if (effect.visualEffectAsset != asset)
                    {
                        return;
                    }
                }
            }

            EditorModeInspectorButton();

            DrawRendererProperties();
            DrawParameters();

            serializedObject.ApplyModifiedProperties();
            if (reinit)
            {
                component.Reinit();
            }

            GUI.enabled = true;
        }

        protected virtual void DrawParameters()
        {
#if !WORKAROUND_TIMELINE
            if (s_FakeObjectCache == null)
            {
                s_FakeObjectCache = ScriptableObject.CreateInstance<FakeObject>();
                s_FakeObjectSerializedCache = new SerializedObject(s_FakeObjectCache);
            }
#endif
            var component = (VisualEffect)target;
            if (m_graph == null || m_asset != component.visualEffectAsset)
            {
                m_asset = component.visualEffectAsset;
                if (m_asset != null)
                {
                    m_graph = m_asset.GetResource().GetOrCreateGraph();
                }
            }

            GUI.enabled = true;

            if (m_graph != null)
            {
                if (m_graph.m_ParameterInfo == null)
                {
                    m_graph.BuildParameterInfo();
                }

                if (m_graph.m_ParameterInfo != null)
                {
                    ShowHeader(Contents.headerParameters, false, false, false, false);
                    List<int> stack = new List<int>();
                    int currentCount = m_graph.m_ParameterInfo.Length;
                    if (currentCount == 0)
                    {
                        GUILayout.Label("No Parameter exposed in the asset");
                    }

                    bool ignoreUntilNextCat = false;

                    foreach (var param in m_graph.m_ParameterInfo)
                    {
                        --currentCount;

                        var parameter = param;

                        if (parameter.descendantCount > 0)
                        {
                            stack.Add(currentCount);
                            currentCount = parameter.descendantCount;
                        }

                        if (currentCount == 0 && stack.Count > 0)
                        {
                            do
                            {
                                currentCount = stack.Last();
                                stack.RemoveAt(stack.Count - 1);
                            }
                            while (currentCount == 0);
                        }

                        if (string.IsNullOrEmpty(parameter.sheetType))
                        {
                            if (!string.IsNullOrEmpty(parameter.name))
                            {
                                if (string.IsNullOrEmpty(parameter.realType)) // This is a category
                                {
                                    bool wasIgnored = ignoreUntilNextCat;
                                    ignoreUntilNextCat = false;
                                    var nameContent = GetGUIContent(parameter.name);

                                    bool prevState = EditorPrefs.GetBool("VFX-category-" + parameter.name, true);
                                    bool currentState = ShowHeader(nameContent, !wasIgnored, false, true, prevState);
                                    if (currentState != prevState)
                                    {
                                        EditorPrefs.SetBool("VFX-category-" + parameter.name, currentState);
                                    }

                                    if (!currentState)
                                        ignoreUntilNextCat = true;
                                    else
                                        GUILayout.Space(Styles.headerBottomMargin);
                                }
                                else if (!ignoreUntilNextCat)
                                    EmptyLineControl(parameter.name, parameter.tooltip, stack.Count);
                            }
                        }
                        else if (!ignoreUntilNextCat)
                        {
                            var vfxField = m_VFXPropertySheet.FindPropertyRelative(parameter.sheetType + ".m_Array");
                            SerializedProperty property = null;
                            if (vfxField != null)
                            {
                                for (int i = 0; i < vfxField.arraySize; ++i)
                                {
                                    property = vfxField.GetArrayElementAtIndex(i);
                                    var nameProperty = property.FindPropertyRelative("m_Name").stringValue;
                                    if (nameProperty == parameter.path)
                                    {
                                        break;
                                    }

                                    property = null;
                                }
                            }

                            if (property != null)
                            {
                                SerializedProperty overrideProperty = property.FindPropertyRelative("m_Overridden");
                                property = property.FindPropertyRelative("m_Value");
                                string firstpropName = property.name;

                                Color previousColor = GUI.color;
                                var animated = AnimationMode.IsPropertyAnimated(target, property.propertyPath);
                                if (animated)
                                {
                                    GUI.color = AnimationMode.animatedPropertyColor;
                                }

                                DisplayProperty(ref parameter, overrideProperty, property);

                                if (animated)
                                {
                                    GUI.color = previousColor;
                                }
                            }
                        }

                        EditorGUI.indentLevel = stack.Count;
                    }
                }
            }
            GUILayout.Space(1); // Space for the line if the last category is closed.
        }

        private void DrawRendererProperties()
        {
            ShowHeader(Contents.headerRenderer, false, false, false, false);

            m_SerializedRenderers.Update();

            if (m_RendererTransparentPriority != null)
                EditorGUILayout.PropertyField(m_RendererTransparentPriority, Contents.rendererPriorityStyle);

            if (m_RendererRenderingLayerMask != null)
            {
                RenderPipelineAsset srpAsset = GraphicsSettings.renderPipelineAsset;
                if (srpAsset != null)
                {
                    var layerNames = srpAsset.GetRenderingLayerMaskNames();
                    if (layerNames != null)
                    {
                        var mask = (int)m_Renderers[0].renderingLayerMask;

                        // EditorGUI.showMixedValue = m_RendererRenderingLayerMask.hasMultipleDifferentValues;
                        var rect = EditorGUILayout.GetControlRect();

                        EditorGUI.BeginProperty(rect, Contents.renderingLayerMaskStyle, m_RendererRenderingLayerMask);
                        EditorGUI.BeginChangeCheck();

                        mask = EditorGUI.MaskField(rect, Contents.renderingLayerMaskStyle, mask, layerNames);

                        if (EditorGUI.EndChangeCheck())
                            m_RendererRenderingLayerMask.intValue = mask;

                        EditorGUI.EndProperty();

                        // EditorGUI.showMixedValue = false;
                    }
                }
            }

            m_SerializedRenderers.ApplyModifiedProperties();
        }

        protected static class Contents
        {
            public static readonly GUIContent headerPlayControls = EditorGUIUtility.TrTextContent("Play Controls");
            public static readonly GUIContent headerParameters = EditorGUIUtility.TrTextContent("Parameters");
            public static readonly GUIContent headerRenderer = EditorGUIUtility.TrTextContent("Renderer");

            public static readonly GUIContent assetPath = EditorGUIUtility.TrTextContent("Asset Template");
            public static readonly GUIContent randomSeed = EditorGUIUtility.TrTextContent("Random Seed");
            public static readonly GUIContent reseedOnPlay = EditorGUIUtility.TrTextContent("Reseed on play");
            public static readonly GUIContent openEditor = EditorGUIUtility.TrTextContent("Edit");
            public static readonly GUIContent setRandomSeed = EditorGUIUtility.TrTextContent("Reseed");
            public static readonly GUIContent setPlayRate = EditorGUIUtility.TrTextContent("Set");
            public static readonly GUIContent playRate = EditorGUIUtility.TrTextContent("Rate");

            public static readonly GUIContent renderingLayerMaskStyle = EditorGUIUtility.TrTextContent("Rendering Layer Mask", "Mask that can be used with SRP DrawRenderers command to filter renderers outside of the normal layering system.");
            public static readonly GUIContent rendererPriorityStyle = EditorGUIUtility.TrTextContent("Transparency Priority", "Priority used for sorting objects on top of material render queue.");

            static readonly GUIContent[] m_Icons;

            public enum Icon
            {
                Pause,
                Play,
                Restart,
                Step,
                Stop
            }
            static Contents()
            {
                m_Icons = new GUIContent[1 + (int)Icon.Stop];
                for (int i = 0; i <= (int)Icon.Stop; ++i)
                {
                    Icon icon = (Icon)i;
                    string name = icon.ToString();

                    //TODO replace with editor default resource call when going to trunk
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(VisualEffectGraphPackageInfo.assetPackagePath + "/Editor/SceneWindow/Textures/" + name + ".png");
                    if (texture == null)
                    {
                        Debug.LogError("Can't find icon for " + name + " in Styles");
                        continue;
                    }
                    m_Icons[i] = new GUIContent(texture);
                }
            }

            public static GUIContent GetIcon(Icon icon)
            {
                return m_Icons[(int)icon];
            }
        }

        public static GUISkin GetCurrentSkin()
        {
            return EditorGUIUtility.isProSkin ? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene) : EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
        }

        protected static class Styles
        {
            public static readonly GUIStyle toggleStyle;
            public static readonly GUIStyle toggleMixedStyle;

            public static readonly GUIStyle categoryHeader;
            public const float headerTopMargin = 8;
            public const float headerBottomMargin = 4;

            public static readonly GUILayoutOption MiniButtonWidth = GUILayout.Width(48);
            public static readonly GUILayoutOption PlayControlsHeight = GUILayout.Height(24);

            static Styles()
            {
                var builtInSkin = GetCurrentSkin();
                toggleStyle = builtInSkin.GetStyle("ShurikenCheckMark");
                toggleMixedStyle = builtInSkin.GetStyle("ShurikenCheckMarkMixed");
                categoryHeader = new GUIStyle(builtInSkin.label);
                categoryHeader.fontStyle = FontStyle.Bold;
                categoryHeader.border.left = 2;
                categoryHeader.padding.left = 14;
                categoryHeader.border.right = 2;
                //TODO change to editor resources calls
                categoryHeader.normal.background = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "VFX/cat-background-dark" : "VFX/cat-background-light");
            }
        }
    }
}
