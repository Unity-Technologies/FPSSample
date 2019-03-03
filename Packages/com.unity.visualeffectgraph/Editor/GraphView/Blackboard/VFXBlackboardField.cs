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
    class VFXBlackboardField : BlackboardField, IControlledElement<VFXParameterController>
    {
        public VFXBlackboardRow owner
        {
            get; set;
        }

        public VFXBlackboardField() : base()
        {
            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);
            RegisterCallback<MouseCaptureOutEvent>(OnMouseHover);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Rename", (a) => OpenTextEditor(), DropdownMenu.MenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Delete", (a) => GetFirstAncestorOfType<VFXView>().DeleteElements(new GraphElement[] { this }), DropdownMenu.MenuAction.AlwaysEnabled);

            evt.StopPropagation();
        }

        Controller IControlledElement.controller
        {
            get { return owner.controller; }
        }
        public VFXParameterController controller
        {
            get { return owner.controller; }
        }
        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e) {}

        public void SelfChange()
        {
            if (controller.exposed)
            {
                icon = Resources.Load<Texture2D>("VFX/exposed dot");
            }
            else
            {
                icon = null;
            }
        }

        void OnMouseHover(EventBase evt)
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();
            if (view != null)
            {
                foreach (var parameter in view.graphElements.ToList().OfType<VFXParameterUI>().Where(t => t.controller.parentController == controller))
                {
                    if (evt.GetEventTypeId() == MouseEnterEvent.TypeId())
                        parameter.AddToClassList("hovered");
                    else
                        parameter.RemoveFromClassList("hovered");
                }
            }
        }
    }
}
