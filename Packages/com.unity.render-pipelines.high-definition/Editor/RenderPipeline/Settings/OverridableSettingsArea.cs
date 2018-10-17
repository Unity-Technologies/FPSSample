using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal struct OverridableSettingsArea
    {
        static readonly GUIContent overrideTooltip = CoreEditorUtils.GetContent("|Override this setting in component.");

        private struct Field
        {
            public SerializedProperty property;
            public GUIContent content;
            public Action<bool> setter;
            public Func<bool> getter;
            public Func<bool> enabler;
            public object defaultValue;
            public int indent;
            public bool forceOverride;
            public bool enabled { get { return enabler == null || enabler(); } }
        }
        private List<Field> fields;

        public OverridableSettingsArea(int capacity)
        {
            fields = new List<Field>(capacity);
        }

        /// <summary>Add an overrideable field to be draw when Draw(bool) will be called.</summary>
        /// <param name="property">The overrideable property to draw in inspector</param>
        /// <param name="content">The GUIContent of this property</param>
        /// <param name="getter">The getter will be used to check if property is overrided</param>
        /// <param name="setter">The setter will be used to change the "is overrided" status of this field</param>
        /// <param name="enabler">The enabler will be used to check if this field could be overrided. If null or have a return value at true, it will be overrided.</param>
        /// <param name="defaultValue">The value to display when the property is not overrided. If null, use the actual value of it.</param>
        /// <param name="indent">Add this value number of indent when drawing this field.</param>
        public void Add(SerializedProperty property, GUIContent content, Func<bool> getter, Action<bool> setter, Func<bool> enabler = null, object defaultValue = null, int indent = 0, bool forceOverride = false)
        {
            if (fields == null)
                fields = new List<Field>();
            fields.Add(new Field { property = property, content = content, getter = getter, setter = setter, enabler = enabler, defaultValue = defaultValue, indent = indent, forceOverride = forceOverride });
        }

        public void Draw(bool withOverride)
        {
            if (fields == null)
            {
                return;
            }
            if (withOverride & GUI.enabled)
            {
                OverridesHeaders();
            }
            for (int i = 0; i< fields.Count; ++i)
            {
                DrawField(fields[i], withOverride);
            }
        }

        void DrawField(Field field, bool withOverride)
        {
            if (field.indent == 0)
            {
                --EditorGUI.indentLevel;    //alignment provided by the space for override checkbox
            }
            else
            {
                for (int i = field.indent - 1; i > 0; --i)
                {
                    ++EditorGUI.indentLevel;
                }
            }
            bool enabled = field.enabled;
            withOverride |= field.forceOverride;
            withOverride &= enabled & GUI.enabled;
            bool shouldBeDisabled = withOverride || !enabled || !GUI.enabled;
            using (new EditorGUILayout.HorizontalScope())
            {
                var overrideRect = GUILayoutUtility.GetRect(15f, 17f, GUILayout.ExpandWidth(false)); //15 = kIndentPerLevel
                if (withOverride)
                {
                    bool originalValue = field.getter();
                    bool modifiedValue = originalValue;
                    overrideRect.yMin += 4f;
                    modifiedValue = GUI.Toggle(overrideRect, originalValue, overrideTooltip, CoreEditorStyles.smallTickbox);

                    if (originalValue ^ modifiedValue)
                    {
                        field.setter(modifiedValue);
                    }

                    shouldBeDisabled = !modifiedValue;
                }
                using (new EditorGUI.DisabledScope(shouldBeDisabled))
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        //the following block will display a default value if provided instead of actual value (case if(true))
                        if (shouldBeDisabled && field.defaultValue != null)
                        {
                            DrawDefaultValue(field);
                        }
                        else
                        {

                            EditorGUILayout.PropertyField(field.property, field.content);
                        }
                    }
                }
            }
            if (field.indent == 0)
            {
                ++EditorGUI.indentLevel;
            }
            else
            {
                for (int i = field.indent - 1; i > 0; --i)
                {
                    --EditorGUI.indentLevel;
                }
            }
        }

        void DrawDefaultValue(Field field)
        {
            if (field.defaultValue is GUIContent)
            {
                //replacing value by a text
                EditorGUILayout.LabelField(field.content, (GUIContent)field.defaultValue);
            }
            else
            {
                switch (field.property.propertyType)
                {
                    case SerializedPropertyType.String:
                        EditorGUILayout.TextField(field.content, (string)field.defaultValue);
                        break;
                    case SerializedPropertyType.Boolean:
                        EditorGUILayout.Toggle(field.content, (bool)field.defaultValue);
                        break;
                    case SerializedPropertyType.Integer:
                        EditorGUILayout.IntField(field.content, (int)field.defaultValue);
                        break;
                    case SerializedPropertyType.Float:
                        EditorGUILayout.FloatField(field.content, (float)field.defaultValue);
                        break;
                    case SerializedPropertyType.Color:
                        EditorGUILayout.ColorField(field.content, (Color)field.defaultValue);
                        break;
                    case SerializedPropertyType.Enum:
                        EditorGUILayout.EnumPopup(field.content, (Enum)field.defaultValue);
                        break;
                    case SerializedPropertyType.LayerMask:
                        EditorGUILayout.MaskField(field.content, (LayerMask)field.defaultValue, GraphicsSettings.renderPipelineAsset.GetRenderingLayerMaskNames());
                        break;
                    case SerializedPropertyType.ObjectReference:
                        EditorGUILayout.ObjectField(field.content, (UnityEngine.Object)field.defaultValue, field.defaultValue.GetType(), true);
                        break;
                    case SerializedPropertyType.Generic:
                        EditorGUILayout.PropertyField(field.property, includeChildren: true);
                        break;
                    default:
                        EditorGUILayout.LabelField(field.content, new GUIContent("Unsupported type"));
                        Debug.LogError("Unsupported format " + field.property.propertyType + " in OverridableSettingsArea.cs. Please add it!");
                        break;
                }
            }
        }

        void OverridesHeaders()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayoutUtility.GetRect(0f, 17f, GUILayout.ExpandWidth(false));
                if (GUILayout.Button(CoreEditorUtils.GetContent("All|Toggle all overrides on. To maximize performances you should only toggle overrides that you actually need."), CoreEditorStyles.miniLabelButton, GUILayout.Width(17f), GUILayout.ExpandWidth(false)))
                {
                    foreach (var field in fields)
                    {
                        if (field.enabled)
                            field.setter(true);
                    }
                }

                if (GUILayout.Button(CoreEditorUtils.GetContent("None|Toggle all overrides off."), CoreEditorStyles.miniLabelButton, GUILayout.Width(32f), GUILayout.ExpandWidth(false)))
                {
                    foreach (var field in fields)
                    {
                        if (field.enabled)
                            field.setter(false);
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }
    }
}
