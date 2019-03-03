using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    internal class VFXEdge : Edge
    {
        public VFXEdge()
        {
            edgeControl.style.overflow = Overflow.Hidden;
        }
    }

    internal class VFXDataEdge : VFXEdge, IControlledElement<VFXDataEdgeController>
    {
        VFXDataEdgeController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXDataEdgeController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }
        public VFXDataEdge()
        {
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange();
            }
        }

        protected virtual void SelfChange()
        {
            if (controller != null)
            {
                VFXView view = GetFirstAncestorOfType<VFXView>();

                var newInput = view.GetDataAnchorByController(controller.input);

                if (base.input != newInput)
                {
                    if (base.input != null)
                    {
                        base.input.Disconnect(this);
                    }
                    base.input = newInput;
                    base.input.Connect(this);
                }

                var newOutput = view.GetDataAnchorByController(controller.output);

                if (base.output != newOutput)
                {
                    if (base.output != null)
                    {
                        base.output.Disconnect(this);
                    }
                    base.output = newOutput;
                    base.output.Connect(this);
                }

                UpdateEdgeControl();
            }
        }

        public new VFXDataAnchor input
        {
            get { return base.input as VFXDataAnchor; }
        }
        public new VFXDataAnchor output
        {
            get { return base.output as VFXDataAnchor; }
        }

        public override void OnPortChanged(bool isInput)
        {
            base.OnPortChanged(isInput);
        }

        public override void OnSelected()
        {
            base.OnSelected();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
        }
    }
}
