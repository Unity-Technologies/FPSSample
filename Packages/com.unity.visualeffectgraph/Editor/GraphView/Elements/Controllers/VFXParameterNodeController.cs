using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor.Experimental.UIElements.GraphView;

using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;


namespace UnityEditor.VFX.UI
{
    class VFXParameterOutputDataAnchorController : VFXDataAnchorController
    {
        public VFXParameterOutputDataAnchorController(VFXSlot model, VFXParameterNodeController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

        public override Direction direction
        { get { return Direction.Output; } }
        public override string name
        {
            get
            {
                if (depth == 0)
                    return sourceNode.exposedName;
                return base.name;
            }
        }

        public new VFXParameterNodeController sourceNode
        {
            get { return base.sourceNode as VFXParameterNodeController; }
        }
        public override bool expandedSelf
        {
            get
            {
                return sourceNode.infos.expandedSlots != null && sourceNode.infos.expandedSlots.Contains(model);
            }
        }
        public override void ExpandPath()
        {
            if (sourceNode.infos.expandedSlots == null)
                sourceNode.infos.expandedSlots = new List<VFXSlot>();
            bool changed = false;
            if (!sourceNode.infos.expandedSlots.Contains(model))
            {
                sourceNode.infos.expandedSlots.Add(model);
                changed = true;
            }

            if (SlotShouldSkipFirstLevel(model))
            {
                VFXSlot firstChild = model.children.First();
                if (!sourceNode.infos.expandedSlots.Contains(firstChild))
                {
                    sourceNode.infos.expandedSlots.Add(firstChild);
                    changed = true;
                }
            }
            if (changed)
            {
                sourceNode.model.Invalidate(VFXModel.InvalidationCause.kUIChanged);
            }
        }

        public override void RetractPath()
        {
            if (sourceNode.infos.expandedSlots != null)
            {
                bool changed = sourceNode.infos.expandedSlots.Remove(model);
                if (SlotShouldSkipFirstLevel(model))
                {
                    changed |= sourceNode.infos.expandedSlots.Remove(model.children.First());
                }
                if (changed)
                {
                    sourceNode.model.Invalidate(VFXModel.InvalidationCause.kUIChanged);
                }
            }
        }
    }

    class VFXParameterNodeController : VFXNodeController, IPropertyRMProvider
    {
        VFXParameterController m_ParentController;

        int m_Id;

        public VFXParameterNodeController(VFXParameterController controller, VFXParameter.Node infos, VFXViewController viewController) : base(controller.model, viewController)
        {
            m_ParentController = controller;
            m_Id = infos.id;
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            base.ModelChanged(obj);

            foreach (var port in outputPorts)
            {
                port.ApplyChanges(); // call the port ApplyChange because expanded states are stored in the VFXParameter.Node
            }
        }

        public VFXParameter.Node infos
        {
            get { return m_ParentController.model.GetNode(m_Id); }
        }

        protected override VFXDataAnchorController AddDataAnchor(VFXSlot slot, bool input, bool hidden)
        {
            var anchor = new VFXParameterOutputDataAnchorController(slot, this, hidden);
            anchor.portType = slot.property.type;
            return anchor;
        }

        public override string title
        {
            get { return outputPorts.First().name; }
        }

        public string exposedName
        {
            get { return m_ParentController.parameter.exposedName; }
        }
        public bool exposed
        {
            get { return m_ParentController.parameter.exposed; }
        }

        public int order
        {
            get { return m_ParentController.parameter.order; }
        }
        public override bool expanded
        {
            get
            {
                return true;
            }
            set
            {
            }
        }
        public override bool superCollapsed
        {
            get
            {
                return !infos.expanded;
            }
            set
            {
                infos.expanded = !value;
                model.Invalidate(VFXModel.InvalidationCause.kUIChanged);
            }
        }

        public VFXCoordinateSpace space
        {
            get
            {
                return m_ParentController.space;
            }

            set
            {
                m_ParentController.space = value;
            }
        }

        public bool spaceableAndMasterOfSpace
        {
            get
            {
                return m_ParentController.spaceableAndMasterOfSpace;
            }
        }

        public bool IsSpaceInherited()
        {
            return m_ParentController.IsSpaceInherited();
        }

        bool IPropertyRMProvider.expanded
        {
            get
            {
                return false;
            }
        }
        bool IPropertyRMProvider.editable
        {
            get { return true; }
        }

        bool IPropertyRMProvider.expandable { get { return false; } }


        public object value
        {
            get
            {
                return m_ParentController.value;
            }
            set
            {
                m_ParentController.value = value;
            }
        }

        string IPropertyRMProvider.name { get { return "Value"; } }

        object[] IPropertyRMProvider.customAttributes { get { return new object[] {}; } }

        VFXPropertyAttribute[] IPropertyRMProvider.attributes { get { return new VFXPropertyAttribute[] {}; } }

        public Type portType
        {
            get
            {
                return m_ParentController.parameter.GetOutputSlot(0).property.type;
            }
        }

        int IPropertyRMProvider.depth { get { return 0; } }

        void IPropertyRMProvider.ExpandPath()
        {
            throw new NotImplementedException();
        }

        void IPropertyRMProvider.RetractPath()
        {
            throw new NotImplementedException();
        }

        public override void DrawGizmos(VisualEffect component)
        {
            if (VFXGizmoUtility.HasGizmo(m_ParentController.portType))
            {
                m_ParentController.DrawGizmos(component);

                m_GizmoableAnchors.Add(m_ParentController);
            }
        }

        public override Bounds GetGizmoBounds(VisualEffect component)
        {
            return m_ParentController.GetGizmoBounds(component);
        }

        public override bool gizmoNeedsComponent
        {
            get
            {
                return m_ParentController.gizmoNeedsComponent;
            }
        }

        public override int id
        {
            get { return m_Id; }
        }

        public override Vector2 position
        {
            get
            {
                return infos.position;
            }
            set
            {
                infos.position = value;
                model.Invalidate(VFXModel.InvalidationCause.kUIChanged);
            }
        }

        public VFXParameterController parentController
        {
            get { return m_ParentController; }
        }

        public void ConvertToInline()
        {
            VFXInlineOperator op = ScriptableObject.CreateInstance<VFXInlineOperator>();
            op.SetSettingValue("m_Type", (SerializableType)parentController.model.type);

            viewController.graph.AddChild(op);

            op.position = position;

            if (infos.linkedSlots != null)
            {
                foreach (var link in infos.linkedSlots.ToArray())
                {
                    var ancestors = new List<VFXSlot>();
                    ancestors.Add(link.outputSlot);
                    VFXSlot parent = link.outputSlot.GetParent();
                    while (parent != null)
                    {
                        ancestors.Add(parent);

                        parent = parent.GetParent();
                    }
                    int index = parentController.model.GetSlotIndex(ancestors.Last());

                    if (index >= 0 && index < op.GetNbOutputSlots())
                    {
                        VFXSlot slot = op.outputSlots[index];
                        for (int i = ancestors.Count() - 2; i >= 0; --i)
                        {
                            int subIndex = ancestors[i + 1].GetIndex(ancestors[i]);

                            if (subIndex >= 0 && subIndex < slot.GetNbChildren())
                            {
                                slot = slot[subIndex];
                            }
                            else
                            {
                                slot = null;
                                break;
                            }
                        }
                        if (slot.path != link.outputSlot.path.Substring(1)) // parameters output are still named 0, inline outputs have no name.
                        {
                            Debug.LogError("New inline don't have the same subslot as old parameter");
                        }
                        else
                        {
                            link.outputSlot.Unlink(link.inputSlot);
                            slot.Link(link.inputSlot);
                        }
                    }
                }
            }

            op.inputSlots[0].value = value;
            viewController.LightApplyChanges();
            viewController.PutInSameGroupNodeAs(viewController.GetNodeController(op, 0), this);
            viewController.RemoveElement(this);
        }
    }
}
