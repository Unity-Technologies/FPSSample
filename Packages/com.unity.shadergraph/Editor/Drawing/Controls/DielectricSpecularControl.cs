using System;
using System.Globalization;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DielectricSpecularControlAttribute : Attribute, IControlAttribute
    {
        public DielectricSpecularControlAttribute()
        {
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new DielectricSpecularControlView(node, propertyInfo);
        }
    }

    public class DielectricSpecularControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        DielectricSpecularNode.DielectricMaterial m_DielectricMaterial;

        VisualElement m_RangePanel;
        Slider m_RangeSlider;
        FloatField m_RangeField;
        VisualElement m_IORPanel;
        Slider m_IORSlider;
        FloatField m_IORField;

        int m_UndoGroup = -1;

        public DielectricSpecularControlView(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;

            AddStyleSheetPath("Styles/Controls/DielectricSpecularControlView");
            m_DielectricMaterial = (DielectricSpecularNode.DielectricMaterial)m_PropertyInfo.GetValue(m_Node, null);

            if (propertyInfo.PropertyType != typeof(DielectricSpecularNode.DielectricMaterial))
                throw new ArgumentException("Property must be of type DielectricMaterial.", "propertyInfo");

            var enumPanel = new VisualElement { name = "enumPanel" };
            enumPanel.Add(new Label("Material"));
            var enumField = new EnumField(m_DielectricMaterial.type);
            enumField.OnValueChanged(OnEnumChanged);
            enumPanel.Add(enumField);
            Add(enumPanel);

            m_RangePanel = new VisualElement { name = "sliderPanel" };
            m_RangePanel.Add(new Label("Range"));
            Action<float> changedRangeSlider = (s) => { OnChangeRangeSlider(s); };
            m_RangeSlider = new Slider(0.01f, 1, changedRangeSlider) { value = m_DielectricMaterial.range };
            m_RangePanel.Add(m_RangeSlider);
            m_RangeField = AddField(m_RangePanel, m_RangeSlider, 0, m_DielectricMaterial);
            m_RangePanel.SetEnabled(m_DielectricMaterial.type == DielectricMaterialType.Common);
            Add(m_RangePanel);

            m_IORPanel = new VisualElement { name = "sliderPanel" };
            m_IORPanel.Add(new Label("IOR"));
            Action<float> changedIORSlider = (s) => { OnChangeIORSlider(s); };
            m_IORSlider = new Slider(1, 2.5f, changedIORSlider) { value = m_DielectricMaterial.indexOfRefraction };
            m_IORPanel.Add(m_IORSlider);
            m_IORField = AddField(m_IORPanel, m_IORSlider, 1, m_DielectricMaterial);
            m_IORPanel.SetEnabled(m_DielectricMaterial.type == DielectricMaterialType.Custom);
            Add(m_IORPanel);
        }

        void OnEnumChanged(ChangeEvent<Enum> evt)
        {
            if (!evt.newValue.Equals(m_DielectricMaterial.type))
            {
                m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                m_DielectricMaterial.type = (DielectricMaterialType)evt.newValue;
                m_PropertyInfo.SetValue(m_Node, m_DielectricMaterial, null);

                switch (m_DielectricMaterial.type)
                {
                    case DielectricMaterialType.Common:
                        m_RangePanel.SetEnabled(true);
                        m_IORPanel.SetEnabled(false);
                        break;
                    case DielectricMaterialType.Custom:
                        m_RangePanel.SetEnabled(false);
                        m_IORPanel.SetEnabled(true);
                        break;
                    default:
                        m_RangePanel.SetEnabled(false);
                        m_IORPanel.SetEnabled(false);
                        break;
                }
            }
        }

        void OnChangeRangeSlider(float newValue)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Slider Change");
            m_DielectricMaterial.range = newValue;
            m_PropertyInfo.SetValue(m_Node, m_DielectricMaterial, null);
            if (m_RangeField != null)
                m_RangeField.value = newValue;
            this.MarkDirtyRepaint();
        }

        void OnChangeIORSlider(float newValue)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Slider Change");
            m_DielectricMaterial.indexOfRefraction = newValue;
            m_PropertyInfo.SetValue(m_Node, m_DielectricMaterial, null);
            if (m_IORField != null)
                m_IORField.value = newValue;
            this.MarkDirtyRepaint();
        }

        FloatField AddField(VisualElement panel, Slider slider, int index, DielectricSpecularNode.DielectricMaterial initMaterial)
        {
            float initValue;
            if (index == 1)
                initValue = initMaterial.indexOfRefraction;
            else
                initValue = initMaterial.range;

            var field = new FloatField { userData = index, value = initValue };

            field.RegisterCallback<MouseDownEvent>(Repaint);
            field.RegisterCallback<MouseMoveEvent>(Repaint);
            field.OnValueChanged(evt =>
                {
                    var value = (DielectricSpecularNode.DielectricMaterial)m_PropertyInfo.GetValue(m_Node, null);
                    var fieldValue = (float)evt.newValue;
                    if (index == 1)
                    {
                        value.indexOfRefraction = fieldValue;
                        RedrawIORControls(fieldValue);
                    }
                    else
                    {
                        value.range = fieldValue;
                        RedrawRangeControls(fieldValue);
                    }
                    m_PropertyInfo.SetValue(m_Node, value, null);
                    m_UndoGroup = -1;
                    this.MarkDirtyRepaint();
                });
            field.RegisterCallback<InputEvent>(evt =>
                {
                    if (m_UndoGroup == -1)
                    {
                        m_UndoGroup = Undo.GetCurrentGroup();
                        m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    }
                    float newValue;
                    if (!float.TryParse(evt.newData, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out newValue))
                        newValue = 0f;
                    var value = (DielectricSpecularNode.DielectricMaterial)m_PropertyInfo.GetValue(m_Node, null);
                    if (index == 1)
                        value.indexOfRefraction = newValue;
                    else
                        value.range = newValue;
                    m_PropertyInfo.SetValue(m_Node, value, null);
                    this.MarkDirtyRepaint();
                });
            field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                    {
                        Undo.RevertAllDownToGroup(m_UndoGroup);
                        m_UndoGroup = -1;
                        var value = (DielectricSpecularNode.DielectricMaterial)m_PropertyInfo.GetValue(m_Node, null);
                        if (index == 1)
                            RedrawIORControls(value.indexOfRefraction);
                        else
                            RedrawRangeControls(value.range);
                        evt.StopPropagation();
                    }
                    this.MarkDirtyRepaint();
                });
            panel.Add(field);
            return field;
        }

        void RedrawRangeControls(float value)
        {
            value = Mathf.Max(Mathf.Min(value, 1), 0.01f);
            m_RangePanel.Remove(m_RangeSlider);
            Action<float> changedSlider = (s) => { OnChangeRangeSlider(s); };
            m_RangeSlider = new Slider(0.01f, 1, changedSlider) { value = value };
            m_RangePanel.Add(m_RangeSlider);
            m_RangePanel.Remove(m_RangeField);
            m_RangeField.value = value;
            m_RangePanel.Add(m_RangeField);
        }

        void RedrawIORControls(float value)
        {
            value = Mathf.Max(Mathf.Min(value, 5), 1);
            m_IORPanel.Remove(m_IORSlider);
            Action<float> changedSlider = (s) => { OnChangeIORSlider(s); };
            m_IORSlider = new Slider(1, 2.5f, changedSlider)  { value = value };
            m_IORPanel.Add(m_IORSlider);
            m_IORPanel.Remove(m_IORField);
            m_IORField.value = value;
            m_IORPanel.Add(m_IORField);
        }

        void Repaint<T>(MouseEventBase<T> evt) where T : MouseEventBase<T>, new()
        {
            evt.StopPropagation();
            this.MarkDirtyRepaint();
        }
    }
}
