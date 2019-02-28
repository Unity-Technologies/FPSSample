using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Linq;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    class VFXFlowAnchor : Port, IControlledElement<VFXFlowAnchorController>, IEdgeConnectorListener
    {
        VFXFlowAnchorController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXFlowAnchorController controller
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
        public override bool ContainsPoint(Vector2 localPoint)
        {
            return (new Rect(0.0f, 0.0f, layout.width, layout.height)).Contains(localPoint);
        }

        public static VFXFlowAnchor Create(VFXFlowAnchorController controller)
        {
            var anchor = new VFXFlowAnchor(controller.orientation, controller.direction, typeof(int));
            anchor.m_EdgeConnector = new EdgeConnector<VFXFlowEdge>(anchor);
            anchor.AddManipulator(anchor.m_EdgeConnector);
            anchor.controller = controller;
            return anchor;
        }

        protected VFXFlowAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type) : base(anchorOrientation, anchorDirection, Capacity.Multi, type)
        {
            AddStyleSheetPath("VFXFlow");
            AddToClassList("EdgeConnector");
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange();
            }
        }

        void SelfChange()
        {
            m_ConnectorText.text = controller.title;

            if (controller.connected)
                AddToClassList("connected");
            else
                RemoveFromClassList("connected");


            var type = controller.direction == Direction.Input ? controller.context.model.inputType : controller.context.model.outputType;

            switch (controller.direction)
            {
                case Direction.Input:
                {
                    RemoveFromClassList("Output");
                    AddToClassList("Input");
                }
                break;
                case Direction.Output:
                    RemoveFromClassList("Input");
                    AddToClassList("Output");
                    break;
            }


            foreach (var value in System.Enum.GetNames(typeof(VFXDataType)))
            {
                RemoveFromClassList("type" + VFXContextUI.ContextEnumToClassName(value));
            }
            AddToClassList("type" + VFXContextUI.ContextEnumToClassName(type.ToString()));
        }

        void IEdgeConnectorListener.OnDrop(GraphView graphView, Edge edge)
        {
            VFXView view = graphView as VFXView;
            VFXFlowEdge flowEdge = edge as VFXFlowEdge;
            VFXFlowEdgeController edgeController = new VFXFlowEdgeController(flowEdge.input.controller, flowEdge.output.controller);

            if (flowEdge.controller != null)
            {
                view.controller.RemoveElement(flowEdge.controller);
            }

            view.controller.AddElement(edgeController);
        }

        bool ProviderFilter(VFXNodeProvider.Descriptor d)
        {
            if (!(d.modelDescriptor is VFXModelDescriptor<VFXContext>)) return false;

            var desc = d.modelDescriptor as VFXModelDescriptor<VFXContext>;

            if (direction == Direction.Input)
            {
                return VFXContext.CanLink(desc.model, controller.context.model);
            }
            else
            {
                return VFXContext.CanLink(controller.context.model, desc.model);
            }
        }

        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);
            UpdateCapColor();
        }

        void AddLinkedContext(VFXNodeProvider.Descriptor d, Vector2 mPos)
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();
            if (view == null) return;
            Vector2 tPos = view.ChangeCoordinatesTo(view.contentViewContainer, mPos);

            VFXContext context = view.controller.AddVFXContext(tPos, d.modelDescriptor as VFXModelDescriptor<VFXContext>);

            if (context == null) return;


            if (direction == Direction.Input)
            {
                controller.context.model.LinkFrom(context, 0, controller.slotIndex);
            }
            else
            {
                controller.context.model.LinkTo(context, controller.slotIndex, 0);
            }
        }

        void IEdgeConnectorListener.OnDropOutsidePort(Edge edge, Vector2 position)
        {
            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            VFXViewController viewController = view.controller;


            VFXContextUI endContext = null;
            foreach (var node in view.GetAllContexts())
            {
                if (node.worldBound.Contains(position))
                {
                    endContext = node;
                }
            }

            VFXFlowEdge flowEdge  = edge as VFXFlowEdge;
            bool exists = false;
            if (flowEdge.controller != null)
            {
                view.controller.RemoveElement(flowEdge.controller);
                exists = true;
            }

            if (endContext != null)
            {
                VFXContextController nodeController = endContext.controller;

                var compatibleAnchors = viewController.GetCompatiblePorts(controller, null);

                if (controller.direction == Direction.Input)
                {
                    foreach (var outputAnchor in nodeController.flowOutputAnchors)
                    {
                        if (compatibleAnchors.Contains(outputAnchor))
                        {
                            VFXFlowEdgeController edgeController = new VFXFlowEdgeController(controller, outputAnchor);

                            viewController.AddElement(edgeController);
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var inputAnchor in nodeController.flowInputAnchors)
                    {
                        if (compatibleAnchors.Contains(inputAnchor))
                        {
                            VFXFlowEdgeController edgeController = new VFXFlowEdgeController(inputAnchor, controller);

                            viewController.AddElement(edgeController);
                            break;
                        }
                    }
                }
            }
            else if (!exists)
            {
                VFXFilterWindow.Show(VFXViewWindow.currentWindow, Event.current.mousePosition, view.ViewToScreenPosition(Event.current.mousePosition),  new VFXNodeProvider(viewController, AddLinkedContext, ProviderFilter, new Type[] { typeof(VFXContext)}));
            }
        }
    }
}
