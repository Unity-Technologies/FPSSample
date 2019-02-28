using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;

using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Text;
using UnityEditor.Graphs;
using UnityEditor.SceneManagement;

namespace  UnityEditor.VFX.UI
{
    class VFXBlackboardRow : BlackboardRow, IControlledElement<VFXParameterController>
    {
        VFXBlackboardField m_Field;

        VFXBlackboardPropertyView m_Properties;
        public VFXBlackboardRow() : this(new VFXBlackboardField() { name = "vfx-field" }, new VFXBlackboardPropertyView() { name = "vfx-properties" })
        {
            Button button = this.Q<Button>("expandButton");

            if (button != null)
            {
                button.clickable.clicked += OnExpand;
            }

            clippingOptions = ClippingOptions.ClipAndCacheContents;
        }

        void OnExpand()
        {
            controller.expanded = expanded;
        }

        public VFXBlackboardField field
        {
            get
            {
                return m_Field;
            }
        }

        private VFXBlackboardRow(VFXBlackboardField field, VFXBlackboardPropertyView property) : base(field, property)
        {
            m_Field = field;
            m_Properties = property;

            m_Field.owner = this;
            m_Properties.owner = this;
        }

        public int m_CurrentOrder;
        public bool m_CurrentExposed;

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            m_Field.text = controller.exposedName;
            m_Field.typeText = controller.portType != null ? controller.portType.UserFriendlyName() : "null";

            // if the order or exposed change, let the event be caught by the VFXBlackboard
            if (controller.order == m_CurrentOrder && controller.exposed == m_CurrentExposed)
            {
                e.StopPropagation();
            }
            m_CurrentOrder = controller.order;
            m_CurrentExposed = controller.exposed;

            expanded = controller.expanded;

            m_Properties.SelfChange(e.change);

            m_Field.SelfChange();
            RemoveFromClassList("hovered");
        }

        VFXParameterController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXParameterController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }
                    m_Controller = value;
                    m_Properties.Clear();

                    if (m_Controller != null)
                    {
                        m_CurrentOrder = m_Controller.order;
                        m_CurrentExposed = m_Controller.exposed;
                        m_Controller.RegisterHandler(this);
                    }
                }
            }
        }
    }
}
