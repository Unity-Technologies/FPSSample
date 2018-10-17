using System;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [Serializable]
    public struct ToggleData
    {
        public bool isOn;
        public bool isEnabled;

        public ToggleData(bool on, bool enabled)
        {
            isOn = on;
            isEnabled = enabled;
        }

        public ToggleData(bool on)
        {
            isOn = on;
            isEnabled = true;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ToggleControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public ToggleControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ToggleControlView(m_Label, node, propertyInfo);
        }
    }

    public class ToggleControlView : VisualElement, INodeModificationListener
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        UnityEngine.Experimental.UIElements.Toggle m_Toggle;

        public ToggleControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            AddStyleSheetPath("Styles/Controls/ToggleControlView");

            if (propertyInfo.PropertyType != typeof(ToggleData))
                throw new ArgumentException("Property must be a Toggle.", "propertyInfo");

            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            var value = (ToggleData)m_PropertyInfo.GetValue(m_Node, null);
            var panel = new VisualElement { name = "togglePanel" };
            if (!string.IsNullOrEmpty(label))
                panel.Add(new Label(label));
            m_Toggle = new Toggle();
            m_Toggle.OnToggleChanged(OnChangeToggle);
            m_Toggle.SetEnabled(value.isEnabled);
            m_Toggle.value = value.isOn;
            panel.Add(m_Toggle);
            Add(panel);
        }

        public void OnNodeModified(ModificationScope scope)
        {
            var value = (ToggleData)m_PropertyInfo.GetValue(m_Node, null);
            m_Toggle.SetEnabled(value.isEnabled);

            if (scope == ModificationScope.Graph)
            {
                this.MarkDirtyRepaint();
            }
        }

        void OnChangeToggle(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Toggle Change");
            var value = (ToggleData)m_PropertyInfo.GetValue(m_Node, null);
            value.isOn = evt.newValue;
            m_PropertyInfo.SetValue(m_Node, value, null);
            this.MarkDirtyRepaint();
        }
    }
}
