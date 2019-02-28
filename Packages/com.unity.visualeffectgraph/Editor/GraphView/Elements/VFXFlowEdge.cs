using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.VFX.UI
{
    internal class VFXFlowEdge : VFXEdge, IControlledElement<VFXFlowEdgeController>
    {
        public VFXFlowEdge()
        {
            AddStyleSheetPath("VFXFlowEdge");

            edgeControl.inputOrientation = Orientation.Vertical;
            edgeControl.outputOrientation = Orientation.Vertical;
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

                var newInput = view.GetFlowAnchorByController(controller.input);

                if (base.input != newInput)
                {
                    if (base.input != null)
                    {
                        base.input.Disconnect(this);
                    }
                    base.input = newInput;
                    base.input.Connect(this);
                }

                var newOutput = view.GetFlowAnchorByController(controller.output);

                if (base.output != newOutput)
                {
                    if (base.output != null)
                    {
                        base.output.Disconnect(this);
                    }
                    base.output = newOutput;
                    base.output.Connect(this);
                }
            }
            edgeControl.UpdateLayout();
        }

        VFXFlowEdgeController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXFlowEdgeController controller
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
        public new VFXFlowAnchor input
        {
            get { return base.input as VFXFlowAnchor; }
        }
        public new VFXFlowAnchor output
        {
            get { return base.output as VFXFlowAnchor; }
        }
    }
}
