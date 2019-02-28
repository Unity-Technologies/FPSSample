#define _RESTRICT_SOURCE_CURRENT_ATTRIBUTE
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;

using Object = UnityEngine.Object;
using System.Collections.ObjectModel;
using System.Reflection;

namespace UnityEditor.VFX.UI
{
    class VFXGraphValidation
    {
        VFXGraph m_Graph;

        public VFXGraphValidation(VFXGraph graph)
        {
            m_Graph = graph;
        }

        public void ValidateGraph()
        {
            foreach (var child in m_Graph.children)
            {
                if (child is IVFXSlotContainer)
                {
                    ValidateSlotContainer(child as IVFXSlotContainer, m_Graph);
                }
            }

            //Validate links
            foreach (var child in m_Graph.children)
            {
                if (child is IVFXSlotContainer)
                {
                    ValidateSlotContainerLinks(child as IVFXSlotContainer, m_Graph);
                }
            }
        }

        void LogError(object error)
        {
            Debug.LogError(error);
        }

        string GetVFXModelDesc(VFXModel model)
        {
            if (model == null)
                return "null(VFXModel)";
            return string.Format("'{0}'({1})", model.name, model.GetType().Name);
        }

        string GetVFXModelDesc(IVFXSlotContainer model)
        {
            return GetVFXModelDesc(model as VFXModel);
        }

        bool ValidateVFXModel(VFXModel model, VFXModel expectedParent)
        {
            if (model == null)
            {
                LogError("Model error : null. in parent:" + GetVFXModelDesc(expectedParent));
                return false;
            }
            if (model.GetParent() != expectedParent)
            {
                LogError("Model error : wrong parent. expected:" + GetVFXModelDesc(expectedParent) + " actual:" + GetVFXModelDesc(model.GetParent()));
            }
            if (!(model is VFXSlot) &&  model.GetGraph() != m_Graph)
            {
                LogError("Model error : " + GetVFXModelDesc(model) + " wrong graph. expected:" + GetVFXModelDesc(m_Graph) + " actual:" + GetVFXModelDesc(model.GetParent()));
            }
            return true;
        }

        Dictionary<VFXSlot, IVFXSlotContainer> m_SlotOwners = new Dictionary<VFXSlot, IVFXSlotContainer>();

        void ValidateSlotContainer(IVFXSlotContainer slotContainer, VFXModel expectedParent)
        {
            if (!ValidateVFXModel(slotContainer as VFXModel, expectedParent))
                return;

            ValidateSlots(slotContainer.inputSlots, slotContainer);
            ValidateSlots(slotContainer.outputSlots, slotContainer);

            if (slotContainer is VFXContext)
            {
                VFXContext context = slotContainer as VFXContext;
                foreach (var block in context.children)
                {
                    ValidateSlotContainer(block, context);
                }
            }
        }

        void ValidateSlotContainerLinks(IVFXSlotContainer slotContainer, VFXModel expectedParent)
        {
            ValidateSlotsLinks(slotContainer.inputSlots, slotContainer);
            ValidateSlotsLinks(slotContainer.outputSlots, slotContainer);

            if (slotContainer is VFXContext)
            {
                VFXContext context = slotContainer as VFXContext;
                foreach (var block in context.children)
                {
                    ValidateSlotContainerLinks(block, context);
                }
            }
        }

        void ValidateSlots(IEnumerable<VFXSlot> slots, IVFXSlotContainer expectedOwner)
        {
            foreach (var slot in slots)
            {
                ValidateSlot(slot, expectedOwner, null);
            }
        }

        void ValidateSlotsLinks(IEnumerable<VFXSlot> slots, IVFXSlotContainer expectedOwner)
        {
            foreach (var slot in slots)
            {
                ValidateSlotLinks(slot);
            }
        }

        void ValidateSlot(VFXSlot slot, IVFXSlotContainer expectedOwner, VFXSlot expectedParent)
        {
            if (!ValidateVFXModel(slot, expectedParent))
            {
                return;
            }
            m_SlotOwners[slot] = expectedOwner;
            if (slot.owner != expectedOwner)
            {
                LogError("Slot error : wrong owner. expected:" + GetVFXModelDesc(expectedOwner) + " actual:" + GetVFXModelDesc(slot.owner));
            }
            foreach (var subSlot in slot.children)
            {
                ValidateSlot(subSlot, expectedOwner, slot);
            }
        }

        void ValidateSlotLinks(VFXSlot slot)
        {
            foreach (var link in slot.LinkedSlots)
            {
                if (!m_SlotOwners.ContainsKey(link))
                {
                    LogError("Slot :" + GetVFXModelDesc(slot) + "of owner " + GetVFXModelDesc(slot.owner) + " has invalid link :" + GetVFXModelDesc(link));
                }
            }
            foreach (var subSlot in slot.children)
            {
                ValidateSlotLinks(subSlot);
            }
        }
    }
}
