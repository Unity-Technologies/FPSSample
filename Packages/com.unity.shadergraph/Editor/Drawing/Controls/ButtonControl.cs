using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ButtonControlAttribute : Attribute, IControlAttribute
    {
        public ButtonControlAttribute()
        {
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ButtonControlView(node, propertyInfo);
        }
    }

    [Serializable]
    public struct ButtonConfig
    {
        public string text;
        public Action action;
    }

    public class ButtonControlView : VisualElement
    {
        public ButtonControlView(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            AbstractMaterialNode m_Node;

            m_Node = node;

            Type type = propertyInfo.PropertyType;
            if (type != typeof(ButtonConfig))
            {
                throw new ArgumentException("Property must be a ButtonConfig.", "propertyInfo");
            }
            var value = (ButtonConfig)propertyInfo.GetValue(m_Node, null);

            Add(new Button(value.action) { text = value.text});
        }
    }
}
