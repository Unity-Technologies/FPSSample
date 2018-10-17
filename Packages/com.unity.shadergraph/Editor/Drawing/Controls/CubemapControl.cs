using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CubemapControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public CubemapControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new CubemapControlView(m_Label, node, propertyInfo);
        }
    }

    public class CubemapControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        public CubemapControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (propertyInfo.PropertyType != typeof(Cubemap))
                throw new ArgumentException("Property must be of type Texture.", "propertyInfo");
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            var cubemapField = new ObjectField { value = (Cubemap)m_PropertyInfo.GetValue(m_Node, null), objectType = typeof(Cubemap) };
            cubemapField.OnValueChanged(OnChange);
            Add(cubemapField);
        }

        void OnChange(ChangeEvent<UnityEngine.Object> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Cubemap Change");
            m_PropertyInfo.SetValue(m_Node, evt.newValue, null);
            this.MarkDirtyRepaint();
        }
    }
}
