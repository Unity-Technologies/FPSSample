using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    using UnityObject = UnityEngine.Object;

    public static class CoreEditorUtils
    {
        // GUIContent cache utilities
        static Dictionary<string, GUIContent> s_GUIContentCache = new Dictionary<string, GUIContent>();

        public static GUIContent GetContent(string textAndTooltip)
        {
            if (string.IsNullOrEmpty(textAndTooltip))
                return GUIContent.none;

            GUIContent content;

            if (!s_GUIContentCache.TryGetValue(textAndTooltip, out content))
            {
                var s = textAndTooltip.Split('|');
                content = new GUIContent(s[0]);

                if (s.Length > 1 && !string.IsNullOrEmpty(s[1]))
                    content.tooltip = s[1];

                s_GUIContentCache.Add(textAndTooltip, content);
            }

            return content;
        }

        // Serialization helpers
        public static string FindProperty<T, TValue>(Expression<Func<T, TValue>> expr)
        {
            // Get the field path as a string
            MemberExpression me;
            switch (expr.Body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    me = expr.Body as MemberExpression;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var members = new List<string>();
            while (me != null)
            {
                // For field, get the field name
                // For properties, get the name of the backing field
                var name = me.Member is FieldInfo
                    ? me.Member.Name
                    : "m_" + me.Member.Name.Substring(0, 1).ToUpper() + me.Member.Name.Substring(1);
                members.Add(name);
                me = me.Expression as MemberExpression;
            }

            var sb = new StringBuilder();
            for (int i = members.Count - 1; i >= 0; i--)
            {
                sb.Append(members[i]);
                if (i > 0) sb.Append('.');
            }

            return sb.ToString();
        }

        // UI Helpers

        public static void DrawMultipleFields(string label, SerializedProperty[] ppts, GUIContent[] lbls)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(GetContent(label));
            GUILayout.BeginVertical();
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 45;
            for (var i = 0; i < ppts.Length; ++i)
                EditorGUILayout.PropertyField(ppts[i], lbls[i]);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = labelWidth;
        }

        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;
            
            if (isBoxed)
            {
                rect.xMin = EditorGUIUtility.singleLineHeight - 2;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        public static void DrawHeader(string title)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
        }

        public static bool DrawHeaderFoldout(string title, bool state, bool isBoxed = false)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            if (isBoxed)
            {
                labelRect.xMin += 5;
                foldoutRect.xMin += 5;
                backgroundRect.xMin = EditorGUIUtility.singleLineHeight;
                backgroundRect.width -= 3;
            }

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }

            return state;
        }

        public static bool DrawHeaderToggle(string title, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction = null)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 32f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            var toggleRect = backgroundRect;
            toggleRect.x += 16f;
            toggleRect.y += 2f;
            toggleRect.width = 13f;
            toggleRect.height = 13f;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            using (new EditorGUI.DisabledScope(!activeField.boolValue))
                EditorGUI.LabelField(labelRect, GetContent(title), EditorStyles.boldLabel);

            // Foldout
            group.serializedObject.Update();
            group.isExpanded = GUI.Toggle(foldoutRect, group.isExpanded, GUIContent.none, EditorStyles.foldout);
            group.serializedObject.ApplyModifiedProperties();

            // Active checkbox
            activeField.serializedObject.Update();
            activeField.boolValue = GUI.Toggle(toggleRect, activeField.boolValue, GUIContent.none, CoreEditorStyles.smallTickbox);
            activeField.serializedObject.ApplyModifiedProperties();

            // Context menu
            var menuIcon = CoreEditorStyles.paneOptionsIcon;
            var menuRect = new Rect(labelRect.xMax + 4f, labelRect.y + 4f, menuIcon.width, menuIcon.height);

            if (contextAction != null)
                GUI.DrawTexture(menuRect, menuIcon);

            // Handle events
            var e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (contextAction != null && menuRect.Contains(e.mousePosition))
                {
                    contextAction(new Vector2(menuRect.x, menuRect.yMax));
                    e.Use();
                }
                else if (labelRect.Contains(e.mousePosition))
                {
                    if (e.button == 0)
                        group.isExpanded = !group.isExpanded;
                    else if (contextAction != null)
                        contextAction(e.mousePosition);

                    e.Use();
                }
            }

            return group.isExpanded;
        }

        static readonly GUIContent[] k_DrawVector6_Label =
        {
            new GUIContent("X"),
            new GUIContent("Y"),
            new GUIContent("Z"),
        };
        const int k_DrawVector6Slider_LabelSize = 60;
        const int k_DrawVector6Slider_FieldSize = 80;

        public static void DrawVector6(GUIContent label, SerializedProperty positive, SerializedProperty negative, Vector3 min, Vector3 max, Color[][] colors = null)
        {
            if (colors != null && (colors.Length != 2 || colors[0].Length != 3 || colors[1].Length != 3))
                    throw new System.ArgumentException("Colors must be a 2x3 array.");

            GUILayout.BeginVertical();
            Rect rect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight));
            if (label != GUIContent.none)
            {
                var labelRect = rect;
                labelRect.x -= 12f;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.x += EditorGUIUtility.labelWidth - 12f;
                rect.width -= EditorGUIUtility.labelWidth - 12f;
            }
            
            var v = positive.vector3Value;
            EditorGUI.BeginChangeCheck();
            v = DrawVector3(rect, k_DrawVector6_Label, v, min, max, false, colors == null ? null : colors[0]);
            if (EditorGUI.EndChangeCheck())
                positive.vector3Value = v;

            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            rect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(0, float.MaxValue, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight));
            rect.x += EditorGUIUtility.labelWidth - 12f;
            rect.width -= EditorGUIUtility.labelWidth - 12f;
            v = negative.vector3Value;
            EditorGUI.BeginChangeCheck();
            v = DrawVector3(rect, k_DrawVector6_Label, v, min, max, true, colors == null ? null : colors[1]);
            if (EditorGUI.EndChangeCheck())
                negative.vector3Value = v;
            GUILayout.EndVertical();
        }

        static Vector3 DrawVector3(Rect rect, GUIContent[] labels, Vector3 value, Vector3 min, Vector3 max, bool addMinusPrefix, Color[] colors)
        {
            float[] multifloat = new float[] { value.x, value.y, value.z };
            //rect = EditorGUI.IndentedRect(rect);
            float fieldWidth = rect.width / 3f;
            EditorGUI.BeginChangeCheck();
            EditorGUI.MultiFloatField(rect, labels, multifloat);
            if(EditorGUI.EndChangeCheck())
            {
                value.x = Mathf.Max(Mathf.Min(multifloat[0], max.x), min.x);
                value.y = Mathf.Max(Mathf.Min(multifloat[1], max.y), min.y);
                value.z = Mathf.Max(Mathf.Min(multifloat[2], max.z), min.z);
            }

            //Suffix is a hack as sublabel only work with 1 character
            if(addMinusPrefix)
            {
                Rect suffixRect = new Rect(rect.x-19, rect.y, 100, rect.height);
                for(int i = 0; i < 3; ++i)
                {
                    EditorGUI.LabelField(suffixRect, "-");
                    suffixRect.x += fieldWidth + .66f;
                }
            }

            //Color is a hack as nothing is done to handle this at the moment
            if(colors != null)
            {
                if (colors.Length != 3)
                    throw new System.ArgumentException("colors must have 3 elements.");

                Rect suffixRect = new Rect(rect.x - 8, rect.y, 100, rect.height);
                GUIStyle colorMark = new GUIStyle(EditorStyles.label);
                colorMark.normal.textColor = colors[0];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += fieldWidth  - .5f;
                colorMark.normal.textColor = colors[1];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += fieldWidth  + .5f;
                colorMark.normal.textColor = colors[2];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
            }
            return value;
        }

        public static void DrawPopup(GUIContent label, SerializedProperty property, string[] options)
        {
            var mode = property.intValue;
            EditorGUI.BeginChangeCheck();

            if (mode >= options.Length)
                Debug.LogError(string.Format("Invalid option while trying to set {0}", label.text));

            mode = EditorGUILayout.Popup(label, mode, options);
            if (EditorGUI.EndChangeCheck())
                property.intValue = mode;
        }

        public static void DrawCascadeSplitGUI<T>(ref SerializedProperty shadowCascadeSplit)
        {
            float[] cascadePartitionSizes = null;

            System.Type type = typeof(T);
            if (type == typeof(float))
            {
                cascadePartitionSizes = new float[] { shadowCascadeSplit.floatValue };
            }
            else if (type == typeof(Vector3))
            {
                Vector3 splits = shadowCascadeSplit.vector3Value;
                cascadePartitionSizes = new float[]
                {
                    Mathf.Clamp(splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[1] - splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[2] - splits[1], 0.0f, 1.0f)
                };
            }

            if (cascadePartitionSizes != null)
            {
                EditorGUI.BeginChangeCheck();
                ShadowCascadeSplitGUI.HandleCascadeSliderGUI(ref cascadePartitionSizes);
                if (EditorGUI.EndChangeCheck())
                {
                    if (type == typeof(float))
                        shadowCascadeSplit.floatValue = cascadePartitionSizes[0];
                    else
                    {
                        Vector3 updatedValue = new Vector3();
                        updatedValue[0] = cascadePartitionSizes[0];
                        updatedValue[1] = updatedValue[0] + cascadePartitionSizes[1];
                        updatedValue[2] = updatedValue[1] + cascadePartitionSizes[2];
                        shadowCascadeSplit.vector3Value = updatedValue;
                    }
                }
            }
        }

        public static void RemoveMaterialKeywords(Material material)
        {
            material.shaderKeywords = null;
        }

        public static T[] GetAdditionalData<T>(UnityEngine.Object[] targets, Action<T> initDefault = null)
            where T : Component
        {
            // Handles multi-selection
            var data = targets.Cast<Component>()
                .Select(t => t.GetComponent<T>())
                .ToArray();

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == null)
                {
                    data[i] = Undo.AddComponent<T>(((Component)targets[i]).gameObject);
                    if (initDefault != null)
                    {
                        initDefault(data[i]);
                    }
                }
            }

            return data;
        }

        static public GameObject CreateGameObject(GameObject parent, string name, params Type[] types)
        {
            return ObjectFactory.CreateGameObject(GameObjectUtility.GetUniqueNameForSibling(parent != null ? parent.transform : null, name), types);
        }

        static public string GetCurrentProjectVersion()
        {
            string[] readText = File.ReadAllLines("ProjectSettings/ProjectVersion.txt");
            // format is m_EditorVersion: 2018.2.0b7
            string[] versionText = readText[0].Split(' ');
            return versionText[1];
        }

        static public void CheckOutFile(bool VCSEnabled, UnityObject mat)
        {
            if (VCSEnabled)
            {
                UnityEditor.VersionControl.Task task = UnityEditor.VersionControl.Provider.Checkout(mat, UnityEditor.VersionControl.CheckoutMode.Both);

                if (!task.success)
                {
                    Debug.Log(task.text + " " + task.resultCode);
                }
            }
        }
    }
}
