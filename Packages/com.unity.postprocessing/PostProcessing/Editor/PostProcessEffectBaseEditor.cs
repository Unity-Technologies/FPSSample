using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    /// <summary>
    /// The base class for all post-processing effect related editors. If you want to customize the
    /// look of a custom post-processing effect, inherit from <see cref="PostProcessEffectEditor{T}"/>
    /// instead.
    /// </summary>
    /// <seealso cref="PostProcessEffectEditor{T}"/>
    public class PostProcessEffectBaseEditor
    {
        internal PostProcessEffectSettings target { get; private set; }
        internal SerializedObject serializedObject { get; private set; }

        internal SerializedProperty baseProperty;
        internal SerializedProperty activeProperty;

        SerializedProperty m_Enabled;
        Editor m_Inspector;

        internal PostProcessEffectBaseEditor()
        {
        }

        /// <summary>
        /// Repaints the inspector.
        /// </summary>
        public void Repaint()
        {
            m_Inspector.Repaint();
        }

        internal void Init(PostProcessEffectSettings target, Editor inspector)
        {
            this.target = target;
            m_Inspector = inspector;
            serializedObject = new SerializedObject(target);
            m_Enabled = serializedObject.FindProperty("enabled.value");
            activeProperty = serializedObject.FindProperty("active");
            OnEnable();
        }

        /// <summary>
        /// Called when the editor is initialized.
        /// </summary>
        public virtual void OnEnable()
        {
        }

        /// <summary>
        /// Called when the editor is de-initialized.
        /// </summary>
        public virtual void OnDisable()
        {
        }

        internal void OnInternalInspectorGUI()
        {
            serializedObject.Update();
            TopRowFields();
            OnInspectorGUI();
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Called every time the inspector is being redrawn. This is where you should add your UI
        /// drawing code.
        /// </summary>
        public virtual void OnInspectorGUI()
        {
        }

        /// <summary>
        /// Returns the label to use as the effect title. You can override this to return a custom
        /// label, else it will use the effect type as the title.
        /// </summary>
        /// <returns>The label to use as the effect title</returns>
        public virtual string GetDisplayTitle()
        {
            return ObjectNames.NicifyVariableName(target.GetType().Name);
        }

        void TopRowFields()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(EditorUtilities.GetContent("All|Toggle all overrides on. To maximize performances you should only toggle overrides that you actually need."), Styling.miniLabelButton, GUILayout.Width(17f), GUILayout.ExpandWidth(false)))
                    SetAllOverridesTo(true);

                if (GUILayout.Button(EditorUtilities.GetContent("None|Toggle all overrides off."), Styling.miniLabelButton, GUILayout.Width(32f), GUILayout.ExpandWidth(false)))
                    SetAllOverridesTo(false);

                GUILayout.FlexibleSpace();

                bool enabled = m_Enabled.boolValue;
                enabled = GUILayout.Toggle(enabled, EditorUtilities.GetContent("On|Enable this effect."), EditorStyles.miniButtonLeft, GUILayout.Width(35f), GUILayout.ExpandWidth(false));
                enabled = !GUILayout.Toggle(!enabled, EditorUtilities.GetContent("Off|Disable this effect."), EditorStyles.miniButtonRight, GUILayout.Width(35f), GUILayout.ExpandWidth(false));
                m_Enabled.boolValue = enabled;
            }
        }

        void SetAllOverridesTo(bool state)
        {
            Undo.RecordObject(target, "Toggle All");
            target.SetAllOverridesTo(state);
            serializedObject.Update();
        }

        /// <summary>
        /// Draws a property UI element.
        /// </summary>
        /// <param name="property">The property to draw</param>
        protected void PropertyField(SerializedParameterOverride property)
        {
            var title = EditorUtilities.GetContent(property.displayName);
            PropertyField(property, title);
        }

        /// <summary>
        /// Draws a property UI element with a custom title and/or tooltip.
        /// </summary>
        /// <param name="property">The property to draw</param>
        /// <param name="title">A custom title and/or tooltip</param>
        protected void PropertyField(SerializedParameterOverride property, GUIContent title)
        {
            // Check for DisplayNameAttribute first
            var displayNameAttr = property.GetAttribute<DisplayNameAttribute>();
            if (displayNameAttr != null)
                title.text = displayNameAttr.displayName;
            
            // Add tooltip if it's missing and an attribute is available
            if (string.IsNullOrEmpty(title.tooltip))
            {
                var tooltipAttr = property.GetAttribute<TooltipAttribute>();
                if (tooltipAttr != null)
                    title.tooltip = tooltipAttr.tooltip;
            }

            // Look for a compatible attribute decorator
            AttributeDecorator decorator = null;
            Attribute attribute = null;

            foreach (var attr in property.attributes)
            {
                // Use the first decorator we found
                if (decorator == null)
                {
                    decorator = EditorUtilities.GetDecorator(attr.GetType());
                    attribute = attr;
                }

                // Draw unity built-in Decorators (Space, Header)
                if (attr is PropertyAttribute)
                {
                    if (attr is SpaceAttribute)
                    {
                        EditorGUILayout.GetControlRect(false, (attr as SpaceAttribute).height);
                    }
                    else if (attr is HeaderAttribute)
                    {
                        var rect = EditorGUILayout.GetControlRect(false, 24f);
                        rect.y += 8f;
                        rect = EditorGUI.IndentedRect(rect);
                        EditorGUI.LabelField(rect, (attr as HeaderAttribute).header, Styling.headerLabel);
                    }
                }
            }

            bool invalidProp = false;

            if (decorator != null && !decorator.IsAutoProperty())
            {
                if (decorator.OnGUI(property.value, property.overrideState, title, attribute))
                    return;
                
                // Attribute is invalid for the specified property; use default unity field instead
                invalidProp = true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                // Override checkbox
                var overrideRect = GUILayoutUtility.GetRect(17f, 17f, GUILayout.ExpandWidth(false));
                overrideRect.yMin += 4f;
                EditorUtilities.DrawOverrideCheckbox(overrideRect, property.overrideState);

                // Property
                using (new EditorGUI.DisabledScope(!property.overrideState.boolValue))
                {
                    if (decorator != null && !invalidProp)
                    {
                        if (decorator.OnGUI(property.value, property.overrideState, title, attribute))
                            return;
                    }

                    // Default unity field
                    if (property.value.hasVisibleChildren
                        && property.value.propertyType != SerializedPropertyType.Vector2
                        && property.value.propertyType != SerializedPropertyType.Vector3)
                    {
                        GUILayout.Space(12f);
                        EditorGUILayout.PropertyField(property.value, title, true);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(property.value, title);
                    }
                }
            }
        }
    }
}
