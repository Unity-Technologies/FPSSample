using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class Texture3DControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public Texture3DControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new Texture3DControlView(m_Label, node, propertyInfo);
        }
    }

    public class Texture3DControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        public Texture3DControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (propertyInfo.PropertyType != typeof(Texture3D))
                throw new ArgumentException("Property must be of type Texture 3D.", "propertyInfo");
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            var textureField = new ObjectField { value = (Texture3D)m_PropertyInfo.GetValue(m_Node, null), objectType = typeof(Texture3D) };
            textureField.OnValueChanged(OnChange);
            Add(textureField);
        }

        void OnChange(ChangeEvent<UnityEngine.Object> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Texture Change");
            m_PropertyInfo.SetValue(m_Node, evt.newValue, null);
            this.MarkDirtyRepaint();
        }
    }
}
