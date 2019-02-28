using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    interface IVFXSlotContainer
    {
        ReadOnlyCollection<VFXSlot> inputSlots     { get; }
        ReadOnlyCollection<VFXSlot> outputSlots    { get; }

        int GetNbInputSlots();
        int GetNbOutputSlots();

        VFXSlot GetInputSlot(int index);
        VFXSlot GetOutputSlot(int index);

        void AddSlot(VFXSlot slot, int index = -1);
        void RemoveSlot(VFXSlot slot);

        int GetSlotIndex(VFXSlot slot);

        void UpdateOutputExpressions();

        void Invalidate(VFXModel.InvalidationCause cause);
        void Invalidate(VFXModel model, VFXModel.InvalidationCause cause);

        void SetSettingValue(string name, object value);

        void OnCopyLinksOtherSlot(VFXSlot mySlot, VFXSlot prevOtherSlot, VFXSlot newOtherSlot);
        void OnCopyLinksMySlot(VFXSlot myPrevSlot, VFXSlot myNewSlot, VFXSlot otherSlot);

        bool collapsed { get; set; }

        VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot slot);
    }

    abstract class VFXSlotContainerModel<ParentType, ChildrenType> : VFXModel<ParentType, ChildrenType>, IVFXSlotContainer
        where ParentType : VFXModel
        where ChildrenType : VFXModel
    {
        public virtual ReadOnlyCollection<VFXSlot> inputSlots  { get { return m_InputSlots.AsReadOnly(); } }
        public virtual ReadOnlyCollection<VFXSlot> outputSlots { get { return m_OutputSlots.AsReadOnly(); } }

        public virtual int GetNbInputSlots()            { return m_InputSlots.Count; }
        public virtual int GetNbOutputSlots()           { return m_OutputSlots.Count; }

        public virtual VFXSlot GetInputSlot(int index)  { return m_InputSlots[index]; }
        public virtual VFXSlot GetOutputSlot(int index) { return m_OutputSlots[index]; }

        protected virtual IEnumerable<VFXPropertyWithValue> inputProperties { get { return PropertiesFromType(GetInputPropertiesTypeName()); } }
        protected virtual IEnumerable<VFXPropertyWithValue> outputProperties { get { return PropertiesFromType(GetOutputPropertiesTypeName()); } }

        // Get properties with value from nested class fields
        protected IEnumerable<VFXPropertyWithValue> PropertiesFromType(string typeName)
        {
            //using are own GetRecursiveNestedType is needed for .net 4.0 compability
            return PropertiesFromType(GetType().GetRecursiveNestedType(typeName));
        }

        // Get properties with value from type fields
        protected static IEnumerable<VFXPropertyWithValue> PropertiesFromType(Type type)
        {
            if (type == null)
                return Enumerable.Empty<VFXPropertyWithValue>();

            var instance = System.Activator.CreateInstance(type);
            return type.GetFields()
                .Where(f => !f.IsStatic)
                .Select(f => {
                    var p = new VFXPropertyWithValue();
                    p.property = new VFXProperty(f);
                    p.value = f.GetValue(instance);
                    return p;
                });
        }

        // Get properties with values from slots
        protected static IEnumerable<VFXPropertyWithValue> PropertiesFromSlots(IEnumerable<VFXSlot> slots)
        {
            return slots.Select(s =>
            {
                var p = new VFXPropertyWithValue();
                p.property = s.property;
                p.value = s.value;
                return p;
            });
        }

        // Get properties with values from slots if any or initialize from default inner class name
        protected IEnumerable<VFXPropertyWithValue> PropertiesFromSlotsOrDefaultFromClass(VFXSlot.Direction direction)
        {
            bool isInput = direction == VFXSlot.Direction.kInput;
            var slots = isInput ? inputSlots : outputSlots;
            if (slots.Count() == 0)
                return PropertiesFromType(isInput ? GetInputPropertiesTypeName() : GetOutputPropertiesTypeName());
            else
                return PropertiesFromSlots(slots);
        }

        protected static string GetInputPropertiesTypeName()
        {
            return "InputProperties";
        }

        protected static string GetOutputPropertiesTypeName()
        {
            return "OutputProperties";
        }

        public virtual void AddSlot(VFXSlot slot, int index = -1) { InnerAddSlot(slot, index, true); }
        private void InnerAddSlot(VFXSlot slot, int index, bool notify)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;

            if (!slot.IsMasterSlot())
                throw new ArgumentException("InnerAddSlot expect only a masterSlot");

            if (slot.owner != this as IVFXSlotContainer)
            {
                if (slot.owner != null)
                    slot.owner.RemoveSlot(slot);

                int realIndex = index == -1 ? slotList.Count : index;
                slotList.Insert(realIndex, slot);
                slot.SetOwner(this);
                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        void IVFXSlotContainer.Invalidate(VFXModel model, InvalidationCause cause)
        {
            Invalidate(model, cause);
        }

        public virtual void OnCopyLinksOtherSlot(VFXSlot mySlot, VFXSlot prevOtherSlot, VFXSlot newOtherSlot)
        {
        }

        public virtual void OnCopyLinksMySlot(VFXSlot myPrevSlot, VFXSlot myNewSlot, VFXSlot otherSlot)
        {
        }

        public virtual void RemoveSlot(VFXSlot slot) { InnerRemoveSlot(slot, true); }
        private void InnerRemoveSlot(VFXSlot slot, bool notify)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;

            if (!slot.IsMasterSlot())
                throw new ArgumentException();

            if (slot.owner == this as IVFXSlotContainer)
            {
                slotList.Remove(slot);
                slot.SetOwner(null);
                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        public int GetSlotIndex(VFXSlot slot)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;
            return slotList.IndexOf(slot);
        }

        protected VFXSlotContainerModel()
        {}

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_InputSlots == null)
            {
                m_InputSlots = new List<VFXSlot>();
                SyncSlots(VFXSlot.Direction.kInput, false); // Initial slot creation
            }
            else
            {
                int nbRemoved = m_InputSlots.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} input slot(s) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }

            if (m_OutputSlots == null)
            {
                m_OutputSlots = new List<VFXSlot>();
                SyncSlots(VFXSlot.Direction.kOutput, false); // Initial slot creation
            }
            else
            {
                int nbRemoved = m_OutputSlots.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} output slot(s) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }
        }

        public override void Sanitize(int version)
        {
            base.Sanitize(version);
            if (ResyncSlots(true))
                Debug.Log(string.Format("Slots have been resynced in {0} of type {1}", name, GetType()));
        }

        public override void OnUnknownChange()
        {
            base.OnUnknownChange();
            SyncSlots(VFXSlot.Direction.kInput, false);
            SyncSlots(VFXSlot.Direction.kOutput, false);
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs)
        {
            base.CollectDependencies(objs);
            foreach (var slot in m_InputSlots.Concat(m_OutputSlots))
            {
                objs.Add(slot);
                slot.CollectDependencies(objs);
            }
        }

        public virtual bool ResyncSlots(bool notify)
        {
            bool changed = false;
            changed |= SyncSlots(VFXSlot.Direction.kInput, notify);
            changed |= SyncSlots(VFXSlot.Direction.kOutput, notify);
            return changed;
        }

        public void MoveSlots(VFXSlot.Direction direction, int movedIndex, int targetIndex)
        {
            VFXSlot movedSlot = m_InputSlots[movedIndex];
            if (movedIndex < targetIndex)
            {
                m_InputSlots.Insert(targetIndex, movedSlot);
                m_InputSlots.RemoveAt(movedIndex);
            }
            else
            {
                m_InputSlots.RemoveAt(movedIndex);
                m_InputSlots.Insert(targetIndex, movedSlot);
            }
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (model == this && cause == InvalidationCause.kSettingChanged)
                ResyncSlots(true);

            base.OnInvalidate(model, cause);
        }

        static public IEnumerable<VFXNamedExpression> GetExpressionsFromSlots(IVFXSlotContainer slotContainer)
        {
            foreach (var master in slotContainer.inputSlots)
            {
                foreach (var slot in master.GetExpressionSlots())
                {
                    var expression = slot.GetExpression();
                    yield return new VFXNamedExpression(expression, slot.fullName);
                }
            }
        }

        protected void InitSlotsFromProperties(IEnumerable<VFXPropertyWithValue> properties, VFXSlot.Direction direction)
        {
            foreach (var p in properties)
            {
                var slot = VFXSlot.Create(p, direction);
                InnerAddSlot(slot, -1, false);
            }
        }

        protected bool SyncSlots(VFXSlot.Direction direction, bool notify)
        {
            bool isInput = direction == VFXSlot.Direction.kInput;

            var expectedProperties = (isInput ? inputProperties : outputProperties).ToArray();
            int nbSlots = isInput ? GetNbInputSlots() : GetNbOutputSlots();
            var currentSlots = isInput ? inputSlots : outputSlots;

            // check all slots owner (TODO Still useful?)
            for (int i = 0; i < nbSlots; ++i)
            {
                VFXSlot slot = currentSlots[i];
                var slotOwner = slot.owner as VFXSlotContainerModel<ParentType, ChildrenType>;
                if (slotOwner != this)
                {
                    Debug.LogError("Slot :" + slot.name + " of Container" + name + "Has a wrong owner.");
                    slot.SetOwner(this); // make sure everything works even if the owner was lost for some reason.
                }
            }

            bool recreate = false;
            if (nbSlots != expectedProperties.Length)
                recreate = true;
            else
            {
                for (int i = 0; i < nbSlots; ++i)
                    if (!currentSlots[i].property.Equals(expectedProperties[i].property))
                    {
                        recreate = true;
                        break;
                    }
            }

            if (recreate)
            {
                var existingSlots = new List<VFXSlot>(currentSlots);

                // Remove all slots
                for (int i = nbSlots - 1; i >= 0; --i)
                    InnerRemoveSlot(currentSlots[i], false);

                var newSlotCount = expectedProperties.Length;
                var newSlots = new VFXSlot[newSlotCount];
                var createdSlots = new List<VFXSlot>(newSlotCount);

                // Reuse slots that already exists or create a new one if not
                for (int i = 0; i < newSlotCount; ++i)
                {
                    var p = expectedProperties[i];
                    var slot = existingSlots.Find(s => p.property.Equals(s.property));
                    if (slot != null)
                    {
                        slot.UpdateAttributes(p.property.attributes);
                        existingSlots.Remove(slot);
                    }
                    else
                    {
                        slot = VFXSlot.Create(p, direction);
                        createdSlots.Add(slot);
                    }

                    newSlots[i] = slot;
                }

                for (int i = 0; i < createdSlots.Count; ++i)
                {
                    var dstSlot = createdSlots[i];

                    // Try to keep links and value for slots of same name and compatible types
                    var srcSlot = existingSlots.FirstOrDefault(s => s.property.name == dstSlot.property.name);

                    // Find the first slot with same type (should perform a more clever selection based on name distance)
                    if (srcSlot == null)
                        srcSlot = existingSlots.FirstOrDefault(s => s.property.type == dstSlot.property.type);

                    // Try to find a slot that can be implicitely converted
                    if (srcSlot == null)
                        srcSlot = existingSlots.FirstOrDefault(s => VFXConverter.CanConvertTo(s.property.type,dstSlot.property.type));

                    if (srcSlot != null)
                    {
                        VFXSlot.CopyLinksAndValue(dstSlot, srcSlot, notify);
                        srcSlot.UnlinkAll(true, notify);
                        existingSlots.Remove(srcSlot);
                    }
                }

                // Remove all remaining links
                foreach (var slot in existingSlots)
                    slot.UnlinkAll(true, notify);

                // Add all slots
                foreach (var s in newSlots)
                    InnerAddSlot(s, -1, false);

                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
            else
            {
                // Update properties
                for (int i = 0; i < nbSlots; ++i)
                {
                    VFXProperty prop = currentSlots[i].property;
                    currentSlots[i].UpdateAttributes(expectedProperties[i].property.attributes);
                }
            }

            return recreate;
        }

        public void ExpandPath(string fieldPath)
        {
            m_expandedPaths.Add(fieldPath);
            Invalidate(InvalidationCause.kParamChanged);
        }

        public void RetractPath(string fieldPath)
        {
            m_expandedPaths.Remove(fieldPath);
            Invalidate(InvalidationCause.kParamChanged);
        }

        public bool IsPathExpanded(string fieldPath)
        {
            return m_expandedPaths.Contains(fieldPath);
        }

        protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);
        }

        public virtual void UpdateOutputExpressions() {}

        public virtual VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            return (VFXCoordinateSpace)int.MaxValue;
        }

        //[SerializeField]
        HashSet<string> m_expandedPaths = new HashSet<string>();

        [SerializeField]
        List<VFXSlot> m_InputSlots;

        [SerializeField]
        List<VFXSlot> m_OutputSlots;
    }
}
