using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PopupControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public PopupControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new PopupControlView(m_Label, node, propertyInfo);
        }
    }

    [Serializable]
    public struct PopupList
    {
        public int selectedEntry;
        public List<string> popupEntries;
    }

    public class PopupControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        PopupField<string> m_PopupField;

        public PopupControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            AddStyleSheetPath("Styles/Controls/PopupControlView");
            m_Node = node;
            m_PropertyInfo = propertyInfo;

            Type type = propertyInfo.PropertyType;
            if (type != typeof(PopupList))
            {
                throw new ArgumentException("Property must be a PopupList.", "propertyInfo");
            }

            Add(new Label(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name)));
            var value = (PopupList)propertyInfo.GetValue(m_Node, null);
            m_PopupField = new PopupField<string>(value.popupEntries, value.selectedEntry);
            m_PopupField.OnValueChanged(OnValueChanged);
            Add(m_PopupField);
        }

        void OnValueChanged(ChangeEvent<string> evt)
        {
            var value = (PopupList)m_PropertyInfo.GetValue(m_Node, null);
            value.selectedEntry = m_PopupField.index;
            m_PropertyInfo.SetValue(m_Node, value, null);
            m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
        }
    }
}
