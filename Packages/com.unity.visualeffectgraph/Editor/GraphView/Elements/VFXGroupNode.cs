using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXGroupNode : Group, IControlledElement<VFXGroupNodeController>, IVFXMovable
    {
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXGroupNodeController controller
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

        VFXGroupNodeController m_Controller;


        VisualElement m_GroupDropArea;

        public VFXGroupNode()
        {
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            m_GroupDropArea = this.Query("dropArea");

            RegisterCallback<DragPerformEvent>(DragPerform);
            RegisterCallback<DragUpdatedEvent>(DragUpdated);
            RegisterCallback<DragLeaveEvent>(DragLeave);
        }

        public bool CanAcceptDrop()
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();
            if (view == null)
                return false;
            return view.selection.Any(t => t is BlackboardField && (t as BlackboardField).GetFirstAncestorOfType<VFXBlackboardRow>() != null);
        }

        public void DragUpdated(DragUpdatedEvent evt)
        {
            if (CanAcceptDrop())
                m_GroupDropArea.AddToClassList("dragEntered");
        }

        public void DragPerform(DragPerformEvent evt)
        {
            m_GroupDropArea.RemoveFromClassList("dragEntered");
        }

        public void DragLeave(DragLeaveEvent evt)
        {
            m_GroupDropArea.RemoveFromClassList("dragEntered");
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }

        public void OnMoved()
        {
            controller.position = GetPosition();

            foreach (var node in containedElements.OfType<IVFXMovable>())
            {
                node.OnMoved();
            }
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange();
            }
        }

        public void SelfChange()
        {
            // use are custom data changed from the view because we can't listen simply to the VFXUI, because the VFXUI might have been modified because we were removed and the datawatch might call us before the view
            VFXView view = this.GetFirstAncestorOfType<VFXView>();
            if (view == null) return;


            m_ModificationFromController = true;
            inRemoveElement = true;
            title = controller.title;


            var presenterContent = new HashSet<Controller>(controller.nodes);
            var elementContent = containedElements.OfType<IControlledElement>().Where(t => t.controller is VFXNodeController || t.controller is VFXStickyNoteController).ToArray();

            bool elementsChanged = false;
            var elementToDelete = elementContent.Where(t => !presenterContent.Contains(t.controller)).ToArray();
            foreach (var element in elementToDelete)
            {
                this.RemoveElement(element as GraphElement);
                elementsChanged = true;
            }

            if (presenterContent.Count() != elementContent.Count())
            {
                var elementToAdd = presenterContent.Select(t => view.GetGroupNodeElement(t)).Except(elementContent.Cast<GraphElement>()).ToArray();

                //bool someNodeNotFound = false;
                foreach (var element in elementToAdd)
                {
                    if (element != null)
                    {
                        this.AddElement(element as GraphElement);
                        elementsChanged = true;
                    }
                    else
                    {
                        //someNodeNotFound = true;
                    }
                }
            }

            // only update position if the groupnode is empty otherwise the size should be computed from the content.
            if (presenterContent.Count() == 0)
            {
                SetPosition(controller.position);
            }
            else
            {
                if (elementsChanged)
                    UpdateGeometryFromContent();
            }

            m_ModificationFromController = false;
            inRemoveElement = false;
        }

        bool m_ModificationFromController;

        public static bool inRemoveElement {get; set; }


        public void UpdateControllerFromContent()
        {
            /*bool changed = false;
            Controller[] content = this.containedElements.Where(t => t is VFXStickyNote || t is VFXNodeUI).Cast<IControlledElement>().Select(t => t.controller).ToArray();
            Controller[] controllerContent = controller.nodes.ToArray();


            var stickyNoteControllers = new List<VFXStickyNoteController>();
            var nodeControllers = new List<VFXNodeController>();


            foreach (var remove in controllerContent.Except(content))
            {
                if (remove is VFXStickyNoteController)
                {
                    stickyNoteControllers.Add(remove as VFXStickyNoteController);
                }
                else
                {
                    nodeControllers.Add(remove as VFXNodeController);
                }
            }

            if (nodeControllers.Count > 0)
            {
                controller.RemoveNodes(nodeControllers);
                changed = true;
            }
            if (stickyNoteControllers.Count > 0)
            {
                controller.RemoveStickyNotes(stickyNoteControllers);
                changed = true;
            }

            stickyNoteControllers.Clear();
            nodeControllers.Clear();

            foreach (var add in content.Except(controllerContent))
            {
                if (add is VFXStickyNoteController)
                {
                    stickyNoteControllers.Add(add as VFXStickyNoteController);
                }
                else
                {
                    nodeControllers.Add(add as VFXNodeController);
                }
            }
            if (nodeControllers.Count > 0)
            {
                controller.AddNodes(nodeControllers);
                changed = true;
            }

            if (stickyNoteControllers.Count > 0)
            {
                controller.AddStickyNotes(stickyNoteControllers);
                changed = true;
            }
            if (changed)
            {
                OnMoved();
            }*/
        }

        public void ElementsAddedToGroupNode(IEnumerable<GraphElement> elements)
        {
            if (!m_ModificationFromController)
            {
                controller.AddNodes(elements.OfType<ISettableControlledElement<VFXNodeController>>().Select(t => t.controller));
                controller.AddStickyNotes(elements.OfType<VFXStickyNote>().Select(t => t.controller));
                OnMoved();
            }
        }

        public void ElementsRemovedFromGroupNode(IEnumerable<GraphElement> elements)
        {
            if (!m_ModificationFromController && !inRemoveElement)
            {
                controller.RemoveNodes(elements.OfType<ISettableControlledElement<VFXNodeController>>().Select(t => t.controller));
                controller.RemoveStickyNotes(elements.OfType<VFXStickyNote>().Select(t => t.controller));
            }
        }

        public void GroupNodeTitleChanged(string title)
        {
            if (!m_ModificationFromController)
            {
                controller.title = title;
            }
        }
    }
}
