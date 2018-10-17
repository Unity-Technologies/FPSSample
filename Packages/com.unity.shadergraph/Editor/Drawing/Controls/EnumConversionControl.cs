using System;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    public interface IEnumConversion
    {
        Enum from { get; set; }
        Enum to { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class EnumConversionControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new EnumConversionControlView(node, propertyInfo);
        }
    }

    public class EnumConversionControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        IEnumConversion value
        {
            get { return (IEnumConversion)m_PropertyInfo.GetValue(m_Node, null); }
            set { m_PropertyInfo.SetValue(m_Node, value, null); }
        }

        public EnumConversionControlView(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!propertyInfo.PropertyType.GetInterfaces().Any(t => t == typeof(IEnumConversion)))
                throw new ArgumentException("Property type must implement IEnumConversion.");

            m_Node = node;
            m_PropertyInfo = propertyInfo;
            AddStyleSheetPath("Styles/Controls/EnumConversionControlView");
            var currentValue = value;

            var ec = (IEnumConversion)propertyInfo.GetValue(m_Node, null);
            propertyInfo.SetValue(m_Node, ec, null);

            var fromField = new EnumField(currentValue.from);
            fromField.OnValueChanged(OnFromChanged);
            Add(fromField);

            var arrowLabel = new Label("➔");
            Add(arrowLabel);

            var toField = new EnumField(currentValue.to);
            toField.OnValueChanged(OnToChanged);
            Add(toField);
        }

        void OnFromChanged(ChangeEvent<Enum> evt)
        {
            var currentValue = value;
            if (!Equals(currentValue.from, evt.newValue))
            {
                m_Node.owner.owner.RegisterCompleteObjectUndo("Change Colorspace From");
                currentValue.from = evt.newValue;
                value = currentValue;
            }
        }

        void OnToChanged(ChangeEvent<Enum> evt)
        {
            var currentValue = value;
            if (!Equals(currentValue.to, evt.newValue))
            {
                m_Node.owner.owner.RegisterCompleteObjectUndo("Change Colorspace To");
                currentValue.to = evt.newValue;
                value = currentValue;
            }
        }
    }
}
