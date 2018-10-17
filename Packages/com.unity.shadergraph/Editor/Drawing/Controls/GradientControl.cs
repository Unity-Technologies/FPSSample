using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class GradientControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public GradientControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new GradientControlView(m_Label, node, propertyInfo);
        }
    }

    [Serializable]
    public class GradientObject : ScriptableObject
    {
        public Gradient gradient = new Gradient();
    }

    public class GradientControlView : VisualElement
    {
        GUIContent m_Label;

        AbstractMaterialNode m_Node;

        PropertyInfo m_PropertyInfo;

        [SerializeField]
        GradientObject m_GradientObject;

        [SerializeField]
        SerializedObject m_SerializedObject;

        public GradientControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            AddStyleSheetPath("Styles/Controls/GradientControlView");

            if (propertyInfo.PropertyType != typeof(Gradient))
                throw new ArgumentException("Property must be of type Gradient.", "propertyInfo");
            new GUIContent(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name));

            m_GradientObject = ScriptableObject.CreateInstance<GradientObject>();
            m_GradientObject.gradient = new Gradient();
            m_SerializedObject = new SerializedObject(m_GradientObject);

            var gradient = (Gradient)m_PropertyInfo.GetValue(m_Node, null);
            m_GradientObject.gradient.SetKeys(gradient.colorKeys, gradient.alphaKeys);
            m_GradientObject.gradient.mode = gradient.mode;

            var gradientPanel = new VisualElement { name = "gradientPanel" };
            if (!string.IsNullOrEmpty(label))
                gradientPanel.Add(new Label(label));

            var gradientField = new GradientField() { value = m_GradientObject.gradient };
            gradientField.OnValueChanged(OnValueChanged);
            gradientPanel.Add(gradientField);

            Add(gradientPanel);
        }

        void OnValueChanged(ChangeEvent<Gradient> evt)
        {
            m_SerializedObject.Update();
            var value = (Gradient)m_PropertyInfo.GetValue(m_Node, null);
            if (!evt.newValue.Equals(value))
            {
                m_GradientObject.gradient.SetKeys(evt.newValue.colorKeys, evt.newValue.alphaKeys);
                m_GradientObject.gradient.mode = evt.newValue.mode;
                m_SerializedObject.ApplyModifiedProperties();

                m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                m_PropertyInfo.SetValue(m_Node, m_GradientObject.gradient, null);
                this.MarkDirtyRepaint();
            }
        }
    }
}
