using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Color = UnityEditor.ShaderGraph.ColorNode.Color;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColorControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        ColorMode m_ColorMode;

        public ColorControlAttribute(string label = null, ColorMode colorMode = ColorMode.Default)
        {
            m_Label = label;
            m_ColorMode = colorMode;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ColorControlView(m_Label, m_ColorMode, node, propertyInfo);
        }
    }

    public class ColorControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        Color m_Color;
        ColorField m_ColorField;

        public ColorControlView(string label, ColorMode colorMode, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            AddStyleSheetPath("Styles/Controls/ColorControlView");
            if (propertyInfo.PropertyType != typeof(Color))
                throw new ArgumentException("Property must be of type Color.", "propertyInfo");
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            m_Color = (Color)m_PropertyInfo.GetValue(m_Node, null);

            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            m_ColorField = new ColorField { value = m_Color.color, hdr = m_Color.mode == ColorMode.HDR, showEyeDropper = false };
            m_ColorField.OnValueChanged(OnChange);
            Add(m_ColorField);

            VisualElement enumPanel = new VisualElement { name = "enumPanel" };
            enumPanel.Add(new Label("Mode"));
            var enumField = new EnumField(m_Color.mode);
            enumField.OnValueChanged(OnModeChanged);
            enumPanel.Add(enumField);
            Add(enumPanel);
        }

        void OnChange(ChangeEvent<UnityEngine.Color> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Color Change");
            m_Color.color = evt.newValue;
            m_PropertyInfo.SetValue(m_Node, m_Color, null);
            this.MarkDirtyRepaint();
        }

        void OnModeChanged(ChangeEvent<Enum> evt)
        {
            if (!evt.newValue.Equals(m_Color.mode))
            {
                m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                m_Color.mode = (ColorMode)evt.newValue;
                m_ColorField.hdr = m_Color.mode == ColorMode.HDR;
                m_PropertyInfo.SetValue(m_Node, m_Color, null);
            }
        }
    }
}
