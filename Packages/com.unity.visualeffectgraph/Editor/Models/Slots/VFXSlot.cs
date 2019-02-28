using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using System.Reflection;

namespace UnityEditor.VFX
{
    [Serializable]
    class VFXSlot : VFXModel<VFXSlot, VFXSlot>
    {
        public enum Direction
        {
            kInput,
            kOutput,
        }

        public Direction direction      { get { return m_Direction; } }
        public VFXProperty property     { get { return m_Property; } }
        public override string name     { get { return m_Property.name; } }

        protected VFXSlot() {onModified += t => ValueModified(); }


        FieldInfo m_FieldInfoCache;

        void ValueModified()
        {
            m_IsValueCached = false;
            PropagateToChildren(t => t.m_IsValueCached = false);
        }

        [System.NonSerialized]
        bool m_IsValueCached;

        [System.NonSerialized]
        object m_CachedValue;

        public object value
        {
            get
            {
                if (m_IsValueCached)
                {
                    return m_CachedValue;
                }
                try
                {
                    // m_IsValueCached = true; // TODO Reactivate once invalidation is fixed
                    if (IsMasterSlot())
                    {
                        m_CachedValue = GetMasterData().m_Value.Get();
                    }
                    else
                    {
                        object parentValue = GetParent().value;

                        if (m_FieldInfoCache == null)
                        {
                            Type type = GetParent().property.type;
                            m_FieldInfoCache = type.GetField(name);
                        }

                        m_CachedValue = m_FieldInfoCache.GetValue(parentValue);
                    }

                    if (m_CachedValue == null && !typeof(UnityEngine.Object).IsAssignableFrom(property.type))
                    {
                        Debug.Log("null value in slot of type" + property.type.UserFriendlyName());
                    }
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Exception while getting value for slot {0} of type {1}: {2}\n{3}", name, GetType(), e, e.StackTrace);
                    // TODO Initialize to default value (try to call static default static method defaultValue from type)
                    m_CachedValue = null;
                }
                return m_CachedValue;
            }
            set
            {
                m_IsValueCached = false;
                try
                {
                    if (IsMasterSlot())
                        SetValueInternal(value, true);
                    else
                    {
                        object parentValue = GetParent().value;

                        if (m_FieldInfoCache == null)
                        {
                            Type type = GetParent().property.type;
                            m_FieldInfoCache = type.GetField(name);
                        }

                        m_FieldInfoCache.SetValue(parentValue, value);

                        GetParent().value = parentValue;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Exception while setting value for slot {0} of type {1}: {2}\n{3}", name, GetType(), e, e.StackTrace);
                }
            }
        }

        public bool spaceable
        {
            get
            {
                var masterSlot = GetMasterSlot();
                if (masterSlot.m_SlotsSpaceable == null)
                {
                    InitSpaceable(masterSlot);
                }
                return masterSlot.m_SlotsSpaceable.Any();
            }
        }

        public VFXCoordinateSpace space
        {
            get
            {
                if (spaceable)
                    return GetMasterData().m_Space;
                return (VFXCoordinateSpace)int.MaxValue;
            }

            set
            {
                if (spaceable)
                {
                    if (space != value)
                    {
                        GetMasterData().m_Space = value;
                        Invalidate(InvalidationCause.kSpaceChanged);
                    }
                }
                else
                {
                    Debug.LogError("Trying to modify space on a not spaceable slot : " + property.type);
                }
            }
        }

        public bool IsSpaceInherited()
        {
            if (!spaceable)
            {
                Debug.LogError("Unexpected call of IsSpaceInherited : " + ToString());
                return true;
            }

            if (!IsMasterSlot())
                return GetMasterSlot().IsSpaceInherited();

            if (!HasLink() || direction == Direction.kOutput)
                return false;

            var linkedSlot = m_LinkedSlots.First();
            return linkedSlot.spaceable && linkedSlot.space != (VFXCoordinateSpace)int.MaxValue;
        }

        public SpaceableType GetSpaceTransformationType()
        {
            if (!spaceable)
                Debug.LogError("Unexpected call of GetSpaceTransformationType : " + ToString());

            var masterSlot = GetMasterSlot();
            var findSlot = masterSlot.m_SlotsSpaceable.Where(o => o.slot == this).FirstOrDefault();
            return findSlot.slot == null ? SpaceableType.None : findSlot.type; //Explicitly return none (not really needed because is default of spaceableType)
        }

        private void SetValueInternal(object value, bool notify)
        {
            if (!IsMasterSlot()) // Must be a master node
                throw new InvalidOperationException();

            GetMasterData().m_Value.Set(value);
            UpdateDefaultExpressionValue();

            if (notify && owner != null)
                Invalidate(InvalidationCause.kParamChanged);
        }

        public string path
        {
            get
            {
                if (GetParent() != null)
                    return string.Format("{0}.{1}", GetParent().path, name);
                else
                    return name;
            }
        }

        public int depth
        {
            get
            {
                if (GetParent() == null)
                {
                    return 0;
                }
                else
                {
                    return GetParent().depth + 1;
                }
            }
        }

        public string fullName
        {
            get
            {
                string name = property.name;
                if (GetParent() != null)
                    name = GetParent().fullName + "_" + name;
                return name;
            }
        }

        public VFXExpression GetExpression()
        {
            if (!m_ExpressionTreeUpToDate)
                RecomputeExpressionTree();

            return m_OutExpression;
        }

        public VFXExpression GetInExpression()
        {
            if (!m_ExpressionTreeUpToDate)
                RecomputeExpressionTree();

            return m_InExpression;
        }

        public void SetExpression(VFXExpression expr)
        {
            if (!expr.Equals(m_LinkedInExpression))
            {
                PropagateToTree(s =>
                {
                    s.m_LinkedInExpression = null;
                    s.m_LinkedInSlot = null;
                });
                m_LinkedInExpression = expr;
                InvalidateExpressionTree();
            }
        }

        // Get relevant expressions in the slot hierarchy
        public void GetExpressions(HashSet<VFXExpression> expressions)
        {
            var exp = GetExpression();
            if (exp != null)
                expressions.Add(exp);
            else
                foreach (var child in children)
                    child.GetExpressions(expressions);
        }

        // Get relevant slot for UI & exposed expressions
        public IEnumerable<VFXSlot> GetVFXValueTypeSlots()
        {
            if (valueType != VFXValueType.None)
                yield return this;
            else
                foreach (var child in children)
                {
                    var slots = child.GetVFXValueTypeSlots();
                    foreach (var slot in slots)
                        yield return slot;
                }
        }

        // Get relevant slots
        public IEnumerable<VFXSlot> GetExpressionSlots()
        {
            var exp = GetExpression();
            if (exp != null)
                yield return this;
            else
                foreach (var child in children)
                {
                    var exps = child.GetExpressionSlots();
                    foreach (var e in exps)
                        yield return e;
                }
        }

        public VFXExpression DefaultExpr
        {
            get
            {
                if (!m_DefaultExpressionInitialized)
                {
                    InitDefaultExpression();
                }
                return m_DefaultExpression;
            }
        }

        public IEnumerable<VFXSlot> LinkedSlots
        {
            get
            {
                return m_LinkedSlots.AsReadOnly();
            }
        }

        public VFXSlot refSlot
        {
            get
            {
                if (direction == Direction.kOutput || !HasLink())
                    return this;
                return m_LinkedSlots[0];
            }
        }

        public IVFXSlotContainer owner { get { return GetMasterData().m_Owner as IVFXSlotContainer; } }

        public bool IsMasterSlot()          { return m_MasterSlot == this; }
        public VFXSlot GetMasterSlot()      { return m_MasterSlot; }
        private MasterData GetMasterData()  { return GetMasterSlot().m_MasterData; }

        // Never call this directly ! Called only by VFXSlotContainerModel
        public void SetOwner(VFXModel owner)
        {
            if (IsMasterSlot())
                m_MasterData.m_Owner = owner;
            else
                throw new InvalidOperationException();
        }

        public static VFXSlot Create(VFXPropertyWithValue property, Direction direction)
        {
            return Create(property.property, direction, property.value);
        }

        // Create and return a slot hierarchy from a property info
        public static VFXSlot Create(VFXProperty property, Direction direction, object value = null)
        {
            var slot = CreateSub(property, direction); // First create slot tree

            var masterData = new MasterData()
            {
                m_Owner = null,
                m_Value = new VFXSerializableObject(property.type, value),
            };

            slot.PropagateToChildren(s => {
                s.m_MasterSlot = slot;
                s.m_MasterData = null;
            });

            slot.m_MasterData = masterData;
            slot.UpdateDefaultExpressionValue();
            InitSpaceable(slot);
            return slot;
        }

        private static VFXSlot CreateSub(VFXProperty property, Direction direction)
        {
            var desc = VFXLibrary.GetSlot(property.type);
            if (desc != null)
            {
                var slot = desc.CreateInstance();
                slot.m_Direction = direction;
                slot.m_Property = property;

                foreach (var subInfo in property.SubProperties())
                {
                    var subSlot = CreateSub(subInfo, direction);
                    if (subSlot != null)
                    {
                        subSlot.Attach(slot, false);
                    }
                }

                return slot;
            }

            throw new InvalidOperationException(string.Format("Unable to create slot for property {0} of type {1}", property.name, property.type));
        }

        public VFXValueType valueType
        {
            get
            {
                return VFXExpression.GetVFXValueTypeFromType(property.type);
            }
        }

        public static void CopyLinksAndValue(VFXSlot dst, VFXSlot src, bool notify)
        {
            CopyValue(dst, src, notify);
            CopyLinks(dst, src, notify);
            CopySpace(dst, src, notify);
        }

        public static void CopySpace(VFXSlot dst, VFXSlot src, bool notify)
        {
            if (dst.IsMasterSlot() && dst.spaceable && src.spaceable)
            {
                if (dst.space != src.space)
                {
                    dst.GetMasterData().m_Space = src.space;
                    if (notify)
                        dst.Invalidate(InvalidationCause.kSpaceChanged);
                }
            }
        }

        public static void CopyValue(VFXSlot dst, VFXSlot src, bool notify)
        {
            // Transfer value only if dst can hold it (master slot)
            if (dst.IsMasterSlot())
            {
                if (src.property.type == dst.property.type)
                {
                    dst.SetValueInternal(src.value, notify);
                }
                else
                {
                    object newValue;
                    if (VFXConverter.TryConvertTo(src.value, dst.property.type, out newValue))
                    {
                        dst.SetValueInternal(newValue, notify);
                    }
                }
            }
        }

        public static void CopyLinks(VFXSlot dst, VFXSlot src, bool notify)
        {
            var links = src.LinkedSlots.ToArray();
            int index = 0;
            while (index < links.Count())
            {
                var link = links[index];
                if (dst.CanLink(link))
                {
                    dst.Link(link, notify);

                    // TODO Remove the callbacks after VFXParameter refactor
                    if (dst.owner != null)
                        dst.owner.OnCopyLinksMySlot(src, dst, link);
                    if (link.owner != null)
                        link.owner.OnCopyLinksOtherSlot(link, src, dst);
                }
                ++index;
            }

            var copySubLinks = (src.property.type == dst.property.type) ||
                (src.property.type == typeof(Transform) && dst.property.type == typeof(OrientedBox)); // This is bad but needed to keep sublinks when changing transform to orientedbox

            if (copySubLinks && src.GetNbChildren() == dst.GetNbChildren())
            {
                int nbSubSlots = src.GetNbChildren();
                for (int i = 0; i < nbSubSlots; ++i)
                    CopyLinks(dst[i], src[i], notify);
            }
        }

        public override void OnUnknownChange()
        {
            base.OnUnknownChange();

            m_ExpressionTreeUpToDate = false;
            m_DefaultExpressionInitialized = false;
            m_SlotsSpaceable = null;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_LinkedSlots == null)
                m_LinkedSlots = new List<VFXSlot>();

            int nbRemoved = m_LinkedSlots.RemoveAll(c => c == null);// Remove bad references if any
            if (nbRemoved > 0)
                Debug.LogFormat("Remove {0} linked slot(s) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType());

            m_ExpressionTreeUpToDate = false;
            m_DefaultExpressionInitialized = false;
            m_SlotsSpaceable = null;

            if (!IsMasterSlot())
                m_MasterData = null; // Non master slot will always have a null master data
        }

        public override void Sanitize(int version)
        {
            // Remove invalid links (without owners)
            if (owner == null)
                UnlinkAll();

            foreach (var link in LinkedSlots.ToArray())
                if (link.owner == null || ((VFXModel)(link.owner)).GetGraph() != ((VFXModel)owner).GetGraph())
                    Unlink(link);

            // Here we check if hierarchy of type match with slot hierarchy
            var subProperties = property.SubProperties().ToList();
            bool hierarchySane = subProperties.Count == GetNbChildren();
            if (hierarchySane)
                for (int i = 0; i < GetNbChildren(); ++i)
                    if (subProperties[i].type != this[i].property.type)
                    {
                        hierarchySane = false;
                        break;
                    }
                    else
                    {
                        // Just ensure potential renaming of property is taken into account
                        this[i].m_Property = subProperties[i];
                    }

            if (!hierarchySane)
            {
                Debug.LogWarningFormat("Slot {0} holding {1} didnt match the type layout. It is recreated and all links are lost.", property.name, property.type);

                // Try to retrieve the value
                object previousValue = null;
                try
                {
                    previousValue = this.value;
                }
                catch (Exception e)
                {
                    Debug.LogWarningFormat("Exception while trying to retrieve value: {0}: {1}", e, e.StackTrace);
                }

                // Recreate the slot
                var newSlot = Create(property, direction, previousValue);
                if (IsMasterSlot())
                {
                    var owner = this.owner;
                    if (owner != null)
                    {
                        int index = owner.GetSlotIndex(this);
                        owner.RemoveSlot(this);
                        owner.AddSlot(newSlot, index);
                    }
                }
                else
                {
                    var parent = GetParent();
                    var index = parent.GetIndex(this);
                    parent.RemoveChild(this, false);
                    parent.AddChild(newSlot, index);
                }

                CopyLinks(newSlot, this, true);
                UnlinkAll(true);
            }
        }

        private void SetDefaultExpressionValue()
        {
            var val = value;
            if (m_DefaultExpression is VFXValue)
                ((VFXValue)m_DefaultExpression).SetContent(val);
        }

        private void InitDefaultExpression()
        {
            if (GetNbChildren() == 0)
            {
                m_DefaultExpression = DefaultExpression(VFXValue.Mode.FoldableVariable);
            }
            else
            {
                // Depth first
                foreach (var child in children)
                    child.InitDefaultExpression();

                m_DefaultExpression = ExpressionFromChildren(children.Select(c => c.m_DefaultExpression).ToArray());
            }

            m_DefaultExpressionInitialized = true;
        }

        private static void InitSpaceable(VFXSlot masterSlot)
        {
            if (!masterSlot.IsMasterSlot())
                throw new InvalidOperationException("Trying to call init spaceable on a child slot");

            var spaceableCollection = new List<SpaceSlotConcerned>();
            masterSlot.PropagateToChildren(s =>
            {
                if (!s.property.IsExpandable())
                    return;

                var spaceAttributeOnType = s.property.type.GetCustomAttributes(typeof(VFXSpaceAttribute), true).FirstOrDefault();
                if (spaceAttributeOnType != null)
                {
                    spaceableCollection.Add(new SpaceSlotConcerned
                    {
                        slot = s,
                        type = (spaceAttributeOnType as VFXSpaceAttribute).type
                    });
                }

                var fields = s.property.type.GetFields(BindingFlags.Public | BindingFlags.Instance).ToArray();
                if (fields.Length != s.children.Count())
                    throw new InvalidOperationException("Unexpected slot count for : " + s.property.type.ToString());

                for (int fieldIndex = 0; fieldIndex < fields.Length; ++fieldIndex)
                {
                    var spaceAttribute = fields[fieldIndex].GetCustomAttributes(typeof(VFXSpaceAttribute), true).FirstOrDefault();
                    if (spaceAttribute != null)
                    {
                        var slot = s.children.ElementAt(fieldIndex);
                        if (slot.GetParent() is VFXSlotEncapsulated)
                        {
                            slot = slot.GetParent();
                        }

                        spaceableCollection.Add(new SpaceSlotConcerned
                        {
                            slot = slot,
                            type = (spaceAttribute as VFXSpaceAttribute).type
                        });
                    }
                }
            });

            if (spaceableCollection.GroupBy(s => s.slot).Any(g => g.Count() > 1))
            {
                Debug.LogErrorFormat("Unexpected space collection computed for {0}", masterSlot.property.type);
            }

            masterSlot.m_SlotsSpaceable = spaceableCollection.ToArray();
            if (masterSlot.m_SlotsSpaceable.Any())
            {
                if (masterSlot.m_MasterData.m_Space == (VFXCoordinateSpace)int.MaxValue)
                {
                    // By default set the same space as the data
                    // TODO This is useless at the moment because the InitSpaceable is called fat too soon before the slot is even attached to a container...
                    VFXData data = null;
                    if (masterSlot.owner is VFXBlock)
                        data = ((VFXBlock)(masterSlot.owner)).GetData();
                    else if (masterSlot.owner is VFXContext)
                        data = ((VFXContext)(masterSlot.owner)).GetData();

                    if (data is ISpaceable)
                        masterSlot.m_MasterData.m_Space = ((ISpaceable)data).space;
                    else
                        masterSlot.m_MasterData.m_Space = VFXCoordinateSpace.Local;
                }
            }
            else
            {
                masterSlot.m_MasterData.m_Space = (VFXCoordinateSpace)int.MaxValue;
            }
        }

        public void UpdateDefaultExpressionValue()
        {
            if (!m_DefaultExpressionInitialized)
                InitDefaultExpression();
            GetMasterSlot().PropagateToChildren(s => s.SetDefaultExpressionValue());
        }

        void InvalidateChildren(VFXModel model, InvalidationCause cause)
        {
            foreach (var child in children)
            {
                child.OnInvalidate(model, cause);
                child.InvalidateChildren(model, cause);
            }
        }

        protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);

            var owner = this.owner;
            if (owner != null)
                owner.Invalidate(this, cause);
        }

        public void UpdateAttributes(VFXPropertyAttribute[] attributes)
        {
            m_FieldInfoCache = null; // this is call by syncslot. at this point the type of our master slot might have changed.
            m_Property.attributes = attributes;
        }

        protected override void OnAdded()
        {
            base.OnAdded();

            var parent = GetParent();
            PropagateToChildren(s =>
            {
                s.m_MasterData = null;
                s.m_MasterSlot = parent.m_MasterSlot;
            });
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();

            var masterData = new MasterData()
            {
                m_Owner = null,
                m_Value = new VFXSerializableObject(property.type, value),
                m_Space = (VFXCoordinateSpace)int.MaxValue,
            };

            PropagateToChildren(s => {
                s.m_MasterData = null;
                s.m_MasterSlot = this;
            });
            m_MasterData = masterData;
        }

        public void CleanupLinkedSlots()
        {
            m_LinkedSlots = m_LinkedSlots.Where(t => t != null).ToList();
        }

        public int GetNbLinks() { return m_LinkedSlots.Count; }
        public bool HasLink(bool rescursive = false)
        {
            if (GetNbLinks() != 0)
            {
                return true;
            }

            if (rescursive)
            {
                foreach (var child in children)
                {
                    if (child.HasLink(rescursive))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool CanLink(VFXSlot other)
        {
            return direction != other.direction && !m_LinkedSlots.Contains(other) &&
                ((direction == Direction.kInput && CanConvertFrom(other.property.type)) || (other.CanConvertFrom(property.type)));
        }

        public bool Link(VFXSlot other, bool notify = true)
        {
            if (other == null)
                return false;

            if (!CanLink(other) || !other.CanLink(this)) // can link
                return false;

            if (direction == Direction.kOutput)
                InnerLink(this, other);
            else
                InnerLink(other, this);

            if (notify)
            {
                Invalidate(InvalidationCause.kConnectionChanged);
                other.Invalidate(InvalidationCause.kConnectionChanged);
            }

            return true;
        }

        public void Unlink(VFXSlot other, bool notify = true)
        {
            if (m_LinkedSlots.Contains(other))
            {
                if (direction == Direction.kOutput)
                    InnerUnlink(this, other);
                else
                    InnerUnlink(other, this);

                if (notify)
                {
                    Invalidate(InvalidationCause.kConnectionChanged);
                    other.Invalidate(InvalidationCause.kConnectionChanged);
                }
            }
        }

        protected void PropagateToParent(Action<VFXSlot> func)
        {
            var parent = GetParent();
            if (parent != null)
            {
                func(parent);
                parent.PropagateToParent(func);
            }
        }

        protected void PropagateToChildren(Action<VFXSlot> func)
        {
            func(this);
            foreach (var child in children)
                child.PropagateToChildren(func);
        }

        protected void PropagateToTree(Action<VFXSlot> func)
        {
            PropagateToParent(func);
            PropagateToChildren(func);
        }

        private static void UpdateLinkedInExpression(VFXSlot destSlot, VFXSlot refSlot)
        {
            var expression = refSlot.GetExpression();
            if (expression != null)
            {
                destSlot.m_LinkedInExpression = expression;
                destSlot.m_LinkedInSlot = refSlot;
            }
            else if (destSlot.GetType() == refSlot.GetType())
            {
                for (int i = 0; i < destSlot.GetNbChildren(); ++i)
                {
                    UpdateLinkedInExpression(destSlot.children.ElementAt(i), refSlot.children.ElementAt(i));
                }
            }
        }

        public IEnumerable<VFXSlot> AllChildrenWithLink(bool includeSelf = true)
        {
            if (includeSelf && HasLink())
                yield return this;

            foreach (var child in children)
            {
                var results = child.AllChildrenWithLink(true);
                foreach (var result in results)
                {
                    yield return result;
                }
            }
        }

        private void RecomputeExpressionTree()
        {
            // Start from the top most parent
            var masterSlot = GetMasterSlot();

            // When deserializing, default expression wont be initialized
            if (!m_DefaultExpressionInitialized)
                masterSlot.UpdateDefaultExpressionValue();

            // Mark all slots in tree as not up to date
            masterSlot.PropagateToChildren(s => { s.m_ExpressionTreeUpToDate = false; });

            if (direction == Direction.kInput) // For input slots, linked expression are directly taken from linked slots
            {
                masterSlot.PropagateToChildren(s =>
                {
                    s.m_LinkedInExpression = null;
                    s.m_LinkedInSlot = null;
                });

                var linkedChildren = masterSlot.AllChildrenWithLink();
                foreach (var slot in linkedChildren)
                {
                    UpdateLinkedInExpression(slot, slot.refSlot);// this will trigger recomputation of linked expressions if needed
                }
            }
            else
            {
                if (owner != null)
                {
                    owner.UpdateOutputExpressions();
                    // Update outputs can trigger an invalidate, it can be reentrant. Just check if we're up to date after that and early out
                    if (m_ExpressionTreeUpToDate)
                        return;
                }
                else
                    masterSlot.PropagateToChildren(s =>
                    {
                        s.m_LinkedInExpression = null;
                        s.m_LinkedInSlot = null;
                    });
            }

            List<VFXSlot> startSlots = new List<VFXSlot>();
            masterSlot.PropagateToChildren(s =>
            {
                if (s.m_LinkedInExpression != null)
                    startSlots.Add(s);

                // Initialize in expression to linked (will be overwritten later on for some slots)
                s.m_InExpression = s.m_DefaultExpression;
            });

            // First pass set in expression and propagate to children
            foreach (var startSlot in startSlots)
            {
                var inExpressionPatched = ApplySpaceConversion(startSlot.m_LinkedInExpression, startSlot, startSlot.m_LinkedInSlot);
                startSlot.m_InExpression = startSlot.ConvertExpression(inExpressionPatched, startSlot.m_LinkedInSlot); // TODO Handle structural modification
                startSlot.PropagateToChildren(s => {
                    var exp = s.ExpressionToChildren(s.m_InExpression);
                    for (int i = 0; i < s.GetNbChildren(); ++i)
                        s[i].m_InExpression = exp != null ? exp[i] : s.refSlot[i].GetExpression();     // Not sure about that
                });
            }

            // Then propagate to parent
            foreach (var startSlot in startSlots)
                startSlot.PropagateToParent(s => s.m_InExpression = s.ExpressionFromChildren(s.children.Select(c => c.m_InExpression).ToArray()));

            var toInvalidate = new HashSet<VFXSlot>();

            masterSlot.SetOutExpression(masterSlot.m_InExpression, toInvalidate, masterSlot.owner != null ? masterSlot.owner.GetOutputSpaceFromSlot(this) : (VFXCoordinateSpace)int.MaxValue);
            masterSlot.PropagateToChildren(s =>
            {
                var exp = s.ExpressionToChildren(s.m_OutExpression);
                for (int i = 0; i < s.GetNbChildren(); ++i)
                    s[i].SetOutExpression(exp != null ? exp[i] : s[i].m_InExpression, toInvalidate, masterSlot.owner != null ? masterSlot.owner.GetOutputSpaceFromSlot(s) : (VFXCoordinateSpace)int.MaxValue);
            });

            foreach (var slot in toInvalidate)
                slot.InvalidateExpressionTree();
        }

        private static VFXExpression ApplySpaceConversion(VFXExpression exp, VFXSlot destSlot, VFXSlot sourceSlot)
        {
            if (sourceSlot != null
                && destSlot.spaceable && sourceSlot.spaceable
                &&  destSlot.space != sourceSlot.space)
            {
                var destSpaceableType = destSlot.GetSpaceTransformationType();
                var sourceSpaceableType = sourceSlot.GetSpaceTransformationType();

                //Check if this slot is concerned by spaceable transform (e.g. radius of a sphere)
                //+ is same kind of space (but should not happen since SlotDirection isn't compatible to SlotPosition)
                if (destSpaceableType != SpaceableType.None && sourceSpaceableType != SpaceableType.None)
                {
                    if (destSpaceableType != sourceSpaceableType)
                    {
                        Debug.LogError("Unexpected spaceable type connection slot together, should be forbidden");
                    }
                    else
                    {
                        exp = ConvertSpace(exp, destSpaceableType, destSlot.space);
                    }
                }
            }
            return exp;
        }

        private void SetOutExpression(VFXExpression exp, HashSet<VFXSlot> toInvalidate, VFXCoordinateSpace convertToSpace = (VFXCoordinateSpace)int.MaxValue)
        {
            exp = VFXPropertyAttribute.ApplyToExpressionGraph(m_Property.attributes, exp);
            if (convertToSpace != (VFXCoordinateSpace)int.MaxValue)
            {
                exp = ConvertSpace(exp, this, convertToSpace);
            }

            if (m_OutExpression != exp)
            {
                m_OutExpression = exp;
                if (direction == Direction.kInput)
                {
                    if (owner != null)
                        toInvalidate.UnionWith(owner.outputSlots);
                }
                else
                    toInvalidate.UnionWith(LinkedSlots);
            }

            m_ExpressionTreeUpToDate = true;
        }

        private string GetOwnerType()
        {
            if (owner != null)
                return owner.GetType().Name;
            else
                return "No Owner";
        }

        public void InvalidateExpressionTree()
        {
            var masterSlot = GetMasterSlot();

            masterSlot.PropagateToChildren(s => {
                if (s.m_ExpressionTreeUpToDate)
                {
                    s.m_ExpressionTreeUpToDate = false;
                    if (s.direction == Direction.kOutput)
                        foreach (var linkedSlot in s.LinkedSlots.ToArray())     // To array as this can be reentrant...
                            linkedSlot.InvalidateExpressionTree();
                }
            });

            if (masterSlot.direction == Direction.kInput)
            {
                if (owner != null)
                {
                    foreach (var slot in owner.outputSlots.ToArray())
                        slot.InvalidateExpressionTree();
                }
            }

            if (owner != null && direction == Direction.kInput)
                owner.Invalidate(InvalidationCause.kExpressionInvalidated);
        }

        public void UnlinkAll(bool recursive = false, bool notify = true)
        {
            if (recursive)
            {
                PropagateToChildren(o => o.UnlinkAll(false, notify));
            }
            else
            {
                var currentSlots = new List<VFXSlot>(m_LinkedSlots);
                foreach (var slot in currentSlots)
                    Unlink(slot, notify);
            }
        }

        protected override sealed void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kConnectionChanged || cause == InvalidationCause.kSpaceChanged)
            {
                if (IsMasterSlot() && direction == Direction.kInput && spaceable && HasLink())
                {
                    var linkedSlot = m_LinkedSlots.First();
                    if (linkedSlot.spaceable && linkedSlot.space != (VFXCoordinateSpace)int.MaxValue)
                    {
                        space = linkedSlot.space;
                    }
                }
            }

            //Propagate space change to children
            if (cause == InvalidationCause.kSpaceChanged)
            {
                if (IsMasterSlot())
                {
                    InvalidateExpressionTree();
                }
                if (direction == Direction.kOutput)
                {
                    PropagateToChildren(s =>
                    {
                        foreach (var slot in s.m_LinkedSlots)
                            slot.Invalidate(cause);
                    });
                }
            }
            base.OnInvalidate(model, cause);
        }

        private static void InnerLink(VFXSlot output, VFXSlot input)
        {
            input.UnlinkAll(); // First disconnect any other linked slot
            input.PropagateToTree(s => s.UnlinkAll()); // Unlink other links in tree

            input.m_LinkedSlots.Add(output);
            output.m_LinkedSlots.Add(input);

            input.InvalidateExpressionTree();
        }

        private static void InnerUnlink(VFXSlot output, VFXSlot input)
        {
            output.m_LinkedSlots.Remove(input);
            if (input.m_LinkedSlots.Remove(output))
                input.InvalidateExpressionTree();
        }

        protected virtual bool CanConvertFrom(Type type)
        {
            return type == null || property.type == type;
        }

        protected virtual VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            return expression;
        }

        protected virtual VFXExpression[] ExpressionToChildren(VFXExpression exp)   { return null; }
        protected virtual VFXExpression ExpressionFromChildren(VFXExpression[] exp) { return null; }

        public virtual VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return null;
        }

        // Expression cache
        private VFXExpression m_DefaultExpression; // The default expression
        private VFXExpression m_LinkedInExpression; // The current linked expression to the slot
        private VFXSlot m_LinkedInSlot; // The origin of linked slot from linked expression (always null for output slot, and null if m_LinkedInExpression is null)
        private VFXExpression m_InExpression; // correctly converted expression
        private VFXExpression m_OutExpression; // output expression that can be fetched

        [NonSerialized] // This must not survive domain reload !
        private bool m_ExpressionTreeUpToDate = false;
        [NonSerialized]
        private bool m_DefaultExpressionInitialized = false;
        [NonSerialized]
        private SpaceSlotConcerned[] m_SlotsSpaceable;

        private struct SpaceSlotConcerned
        {
            public VFXSlot slot;
            public SpaceableType type;
        }

        [Serializable]
        private class MasterData
        {
            public MasterData()
            {
                m_Space = (VFXCoordinateSpace)int.MaxValue;
            }

            public VFXModel m_Owner;
            public VFXSerializableObject m_Value;
            public VFXCoordinateSpace m_Space; //can be undefined
        }

        [SerializeField]
        private VFXSlot m_MasterSlot;
        [SerializeField]
        private MasterData m_MasterData; // always null for none master slots

        [SerializeField]
        private VFXProperty m_Property;

        [SerializeField]
        private Direction m_Direction;

        [SerializeField]
        private List<VFXSlot> m_LinkedSlots;
    }
}
