using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ChannelEnumControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        int m_SlotId;

        public ChannelEnumControlAttribute(string label = null, int slotId = 0)
        {
            m_Label = label;
            m_SlotId = slotId;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ChannelEnumControlView(m_Label, m_SlotId, node, propertyInfo);
        }
    }

    public class ChannelEnumControlView : VisualElement, INodeModificationListener
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        int m_SlotId;

        PopupField<string> m_PopupField;
        string[] m_ValueNames;

        int m_PreviousChannelCount = -1;

        public ChannelEnumControlView(string label, int slotId, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            AddStyleSheetPath("Styles/Controls/ChannelEnumControlView");
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            m_SlotId = slotId;
            if (!propertyInfo.PropertyType.IsEnum)
                throw new ArgumentException("Property must be an enum.", "propertyInfo");
            Add(new Label(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name)));

            var value = (Enum)m_PropertyInfo.GetValue(m_Node, null);
            m_ValueNames = Enum.GetNames(value.GetType());

            CreatePopup();
        }

        void OnValueChanged(ChangeEvent<string> evt)
        {
            var index = m_PopupField.index;
            var value = (int)m_PropertyInfo.GetValue(m_Node, null);
            if (!index.Equals(value))
            {
                m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                m_PropertyInfo.SetValue(m_Node, index, null);
            }

            CreatePopup();
        }

        public void OnNodeModified(ModificationScope scope)
        {
            if (scope == ModificationScope.Node)
            {
                CreatePopup();
                m_PopupField.MarkDirtyRepaint();
            }
        }

        void CreatePopup()
        {
            int channelCount = SlotValueHelper.GetChannelCount(m_Node.FindSlot<MaterialSlot>(m_SlotId).concreteValueType);

            if (m_PopupField != null)
            {
                if (channelCount == m_PreviousChannelCount)
                    return;

                Remove(m_PopupField);
            }

            m_PreviousChannelCount = channelCount;
            List<string> popupEntries = new List<string>();
            for (int i = 0; i < channelCount; i++)
                popupEntries.Add(m_ValueNames[i]);

            var value = (int)m_PropertyInfo.GetValue(m_Node, null);
            if (value >= channelCount)
                value = 0;

            m_PopupField = new PopupField<string>(popupEntries, value);
            m_PopupField.OnValueChanged(OnValueChanged);
            Add(m_PopupField);
        }
    }
}
