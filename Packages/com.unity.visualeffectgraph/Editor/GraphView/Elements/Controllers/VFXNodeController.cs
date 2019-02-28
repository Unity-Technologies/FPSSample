using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Object = UnityEngine.Object;
using System.Collections.ObjectModel;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.UI
{
    abstract class VFXNodeController : VFXController<VFXModel>, IGizmoController
    {
        protected List<VFXDataAnchorController> m_InputPorts = new List<VFXDataAnchorController>();

        protected List<VFXDataAnchorController> m_OutputPorts = new List<VFXDataAnchorController>();

        public ReadOnlyCollection<VFXDataAnchorController> inputPorts
        {
            get { return m_InputPorts.AsReadOnly(); }
        }

        public ReadOnlyCollection<VFXDataAnchorController> outputPorts
        {
            get { return m_OutputPorts.AsReadOnly(); }
        }

        public VFXNodeController(VFXModel model, VFXViewController viewController) : base(viewController, model)
        {
            var settings = model.GetSettings(true);
            m_Settings = new VFXSettingController[settings.Count()];
            int cpt = 0;
            foreach (var setting in settings)
            {
                var settingController = new VFXSettingController();
                settingController.Init(this.slotContainer, setting.Name, setting.FieldType);
                m_Settings[cpt++] = settingController;
            }
        }

        public virtual void ForceUpdate()
        {
            ModelChanged(model);
        }

        protected virtual void NewInputSet(List<VFXDataAnchorController> newInputs)
        {
        }

        public bool CouldLink(VFXDataAnchorController myAnchor, VFXDataAnchorController otherAnchor, VFXDataAnchorController.CanLinkCache cache)
        {
            if (myAnchor.direction == Direction.Input)
                return CouldLinkMyInputTo(myAnchor, otherAnchor, cache);
            else
                return otherAnchor.sourceNode.CouldLinkMyInputTo(otherAnchor, myAnchor, cache);
        }

        protected virtual bool CouldLinkMyInputTo(VFXDataAnchorController myInput, VFXDataAnchorController otherOutput, VFXDataAnchorController.CanLinkCache cache)
        {
            return false;
        }

        public void UpdateAllEditable()
        {
            foreach (var port in inputPorts)
            {
                port.UpdateEditable();
            }
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            var inputs = inputPorts;
            var newAnchors = new List<VFXDataAnchorController>();

            m_SyncingSlots = true;
            bool changed = UpdateSlots(newAnchors, slotContainer.inputSlots, true, true);
            NewInputSet(newAnchors);

            foreach (var anchorController in m_InputPorts.Except(newAnchors))
            {
                anchorController.OnDisable();
            }
            m_InputPorts = newAnchors;


            newAnchors = new List<VFXDataAnchorController>();
            changed |= UpdateSlots(newAnchors, slotContainer.outputSlots, true, false);

            foreach (var anchorController in m_OutputPorts.Except(newAnchors))
            {
                anchorController.OnDisable();
            }
            m_OutputPorts = newAnchors;
            m_SyncingSlots = false;

            // Call after base.ModelChanged which ensure the ui has been refreshed before we try to create potiential new edges.
            if (changed)
                viewController.DataEdgesMightHaveChanged();

            NotifyChange(AnyThing);
        }

        public virtual Vector2 position
        {
            get
            {
                return model.position;
            }
            set
            {
                model.position = new Vector2(Mathf.Round(value.x), Mathf.Round(value.y));
            }
        }
        public virtual bool superCollapsed
        {
            get
            {
                return model.superCollapsed;
            }
            set
            {
                model.superCollapsed = value;
                if (model.superCollapsed)
                {
                    model.collapsed = false;
                }
            }
        }
        public virtual string title
        {
            get { return model.name; }
        }

        public override IEnumerable<Controller> allChildren
        {
            get { return inputPorts.Cast<Controller>().Concat(outputPorts.Cast<Controller>()).Concat(m_Settings.Cast<Controller>()); }
        }

        public virtual int id
        {
            get {return 0; }
        }

        bool m_SyncingSlots;
        public void DataEdgesMightHaveChanged()
        {
            if (viewController != null && !m_SyncingSlots)
            {
                viewController.DataEdgesMightHaveChanged();
            }
        }

        public virtual void NodeGoingToBeRemoved()
        {
            var outputEdges = outputPorts.SelectMany(t => t.connections);

            foreach (var edge in outputEdges)
            {
                edge.input.sourceNode.OnEdgeGoingToBeRemoved(edge.input);
            }
        }

        public virtual void OnEdgeGoingToBeRemoved(VFXDataAnchorController myInput)
        {
        }

        public virtual void WillCreateLink(ref VFXSlot myInput, ref VFXSlot otherOutput)
        {
        }

        protected virtual bool UpdateSlots(List<VFXDataAnchorController> newAnchors, IEnumerable<VFXSlot> slotList, bool expanded, bool input)
        {
            VFXSlot[] slots = slotList.ToArray();
            bool changed = false;
            foreach (VFXSlot slot in slots)
            {
                VFXDataAnchorController propController = GetPropertyController(slot, input);

                if (propController == null)
                {
                    propController = AddDataAnchor(slot, input, !expanded);
                    changed = true;
                }
                newAnchors.Add(propController);

                if (!VFXDataAnchorController.SlotShouldSkipFirstLevel(slot))
                {
                    changed |= UpdateSlots(newAnchors, slot.children, expanded && propController.expandedSelf, input);
                }
                else
                {
                    VFXSlot firstSlot = slot.children.First();
                    changed |= UpdateSlots(newAnchors, firstSlot.children, expanded && propController.expandedSelf, input);
                }
            }

            return changed;
        }

        public VFXDataAnchorController GetPropertyController(VFXSlot slot, bool input)
        {
            VFXDataAnchorController result = null;

            if (input)
                result = inputPorts.Cast<VFXDataAnchorController>().Where(t => t.model == slot).FirstOrDefault();
            else
                result = outputPorts.Cast<VFXDataAnchorController>().Where(t => t.model == slot).FirstOrDefault();

            return result;
        }

        protected abstract VFXDataAnchorController AddDataAnchor(VFXSlot slot, bool input, bool hidden);

        public IVFXSlotContainer slotContainer { get { return model as IVFXSlotContainer; } }

        public VFXSettingController[] settings
        {
            get { return m_Settings; }
        }

        public virtual bool expanded
        {
            get
            {
                return !slotContainer.collapsed;
            }

            set
            {
                if (value != !slotContainer.collapsed)
                {
                    slotContainer.collapsed = !value;
                }
            }
        }

        public virtual void DrawGizmos(VisualEffect component)
        {
            m_GizmoableAnchors.Clear();
            foreach (VFXDataAnchorController controller in inputPorts)
            {
                if (controller.model != null && controller.model.IsMasterSlot() && VFXGizmoUtility.HasGizmo(controller.portType))
                {
                    m_GizmoableAnchors.Add(controller);
                }
            }

            if (m_GizmoedAnchor == null)
            {
                m_GizmoedAnchor = m_GizmoableAnchors.FirstOrDefault();
            }

            if (m_GizmoedAnchor != null)
            {
                ((VFXDataAnchorController)m_GizmoedAnchor).DrawGizmo(component);
            }
        }

        public virtual Bounds GetGizmoBounds(VisualEffect component)
        {
            if (m_GizmoedAnchor != null)
                return ((VFXDataAnchorController)m_GizmoedAnchor).GetGizmoBounds(component);

            return new Bounds();
        }

        public virtual bool gizmoNeedsComponent
        {
            get
            {
                if (m_GizmoedAnchor == null)
                    return false;
                return ((VFXDataAnchorController)m_GizmoedAnchor).gizmoNeedsComponent;
            }
        }

        public virtual bool gizmoIndeterminate
        {
            get
            {
                if (m_GizmoedAnchor == null)
                    return true;
                return ((VFXDataAnchorController)m_GizmoedAnchor).gizmoIndeterminate;
            }
        }

        IGizmoable m_GizmoedAnchor;
        protected List<IGizmoable> m_GizmoableAnchors = new List<IGizmoable>();

        public ReadOnlyCollection<IGizmoable> gizmoables
        {
            get { return m_GizmoableAnchors.AsReadOnly(); }
        }

        public IGizmoable currentGizmoable
        {
            get { return m_GizmoedAnchor; }
            set { m_GizmoedAnchor = value; }
        }

        private VFXSettingController[] m_Settings;
    }
}
