using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class PortInputView : GraphElement, IDisposable
    {
        const string k_EdgeColorProperty = "edge-color";

        StyleValue<Color> m_EdgeColor;

        public Color edgeColor
        {
            get { return m_EdgeColor.GetSpecifiedValueOrDefault(Color.red); }
        }

        public MaterialSlot slot
        {
            get { return m_Slot; }
        }

        MaterialSlot m_Slot;
        ConcreteSlotValueType m_SlotType;
        VisualElement m_Control;
        VisualElement m_Container;
        EdgeControl m_EdgeControl;

        public PortInputView(MaterialSlot slot)
        {
            AddStyleSheetPath("Styles/PortInputView");
            pickingMode = PickingMode.Ignore;
            ClearClassList();
            m_Slot = slot;
            m_SlotType = slot.concreteValueType;
            AddToClassList("type" + m_SlotType);

            m_EdgeControl = new EdgeControl
            {
                @from = new Vector2(212f - 21f, 11.5f),
                to = new Vector2(212f, 11.5f),
                edgeWidth = 2,
                pickingMode = PickingMode.Ignore
            };
            Add(m_EdgeControl);

            m_Container = new VisualElement { name = "container" };
            {
                m_Control = this.slot.InstantiateControl();
                if (m_Control != null)
                    m_Container.Add(m_Control);

                var slotElement = new VisualElement { name = "slot" };
                {
                    slotElement.Add(new VisualElement { name = "dot" });
                }
                m_Container.Add(slotElement);
            }
            Add(m_Container);

            m_Container.visible = m_EdgeControl.visible = m_Control != null;
        }

        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);
            styles.ApplyCustomProperty(k_EdgeColorProperty, ref m_EdgeColor);
            m_EdgeControl.UpdateLayout();
            m_EdgeControl.inputColor = edgeColor;
            m_EdgeControl.outputColor = edgeColor;
        }

        public void UpdateSlot(MaterialSlot newSlot)
        {
            m_Slot = newSlot;
            Recreate();
        }

        public void UpdateSlotType()
        {
            if (slot.concreteValueType != m_SlotType)
                Recreate();
        }

        void Recreate()
        {
            RemoveFromClassList("type" + m_SlotType);
            m_SlotType = slot.concreteValueType;
            AddToClassList("type" + m_SlotType);
            if (m_Control != null)
            {
                var disposable = m_Control as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
                m_Container.Remove(m_Control);
            }
            m_Control = slot.InstantiateControl();
            if (m_Control != null)
                m_Container.Insert(0, m_Control);

            m_Container.visible = m_EdgeControl.visible = m_Control != null;
        }

        public void Dispose()
        {
            var disposable = m_Control as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }
    }
}
