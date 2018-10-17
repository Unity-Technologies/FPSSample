using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class IntegerControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public IntegerControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new IntegerControlView(m_Label, node, propertyInfo);
        }
    }

    public class IntegerControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        public IntegerControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            AddStyleSheetPath("Styles/Controls/IntegerControlView");
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (propertyInfo.PropertyType != typeof(int))
                throw new ArgumentException("Property must be of type integer.", "propertyInfo");
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            var intField = new IntegerField { value = (int)m_PropertyInfo.GetValue(m_Node, null) };
            intField.OnValueChanged(OnChange);

            Add(intField);
        }

        void OnChange(ChangeEvent<int> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Integer Change");
            m_PropertyInfo.SetValue(m_Node, evt.newValue, null);
            this.MarkDirtyRepaint();
        }
    }
}
