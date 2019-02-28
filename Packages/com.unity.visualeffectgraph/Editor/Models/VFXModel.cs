using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEngine.Profiling;

namespace UnityEditor.VFX
{
    class VFXObject : ScriptableObject
    {
        public Action<VFXObject> onModified;
        void OnValidate()
        {
            Modified();
        }

        public void Modified()
        {
            if (onModified != null)
                onModified(this);
        }
    }

    [Serializable]
    abstract class VFXModel : VFXObject
    {
        public enum InvalidationCause
        {
            kStructureChanged,      // Model structure (hierarchy) has changed
            kParamChanged,          // Some parameter values have changed
            kParamPropagated,       // Some parameter values have change and was propagated from the parents
            kSettingChanged,        // A setting value has changed
            kSpaceChanged,          // Space has been changed
            kConnectionChanged,     // Connection have changed
            kExpressionInvalidated, // No direct change to the model but a change in connection was propagated from the parents
            kExpressionGraphChanged,// Expression graph must be recomputed
            kUIChanged,             // UI stuff has changed
        }

        public new virtual string name  { get { return string.Empty; } }
        public virtual string libraryName  { get { return name; } }

        public delegate void InvalidateEvent(VFXModel model, InvalidationCause cause);

        public event InvalidateEvent onInvalidateDelegate;

        protected VFXModel()
        {
            m_UICollapsed = true;
        }

        public virtual void OnEnable()
        {
            if (m_Children == null)
                m_Children = new List<VFXModel>();
            else
            {
                int nbRemoved = m_Children.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} child(ren) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }
        }

        public virtual void Sanitize(int version) {}

        public virtual void OnUnknownChange()
        {
        }

        public virtual void CollectDependencies(HashSet<ScriptableObject> objs)
        {
            foreach (var child in children)
            {
                objs.Add(child);
                child.CollectDependencies(objs);
            }
        }

        protected virtual void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (onInvalidateDelegate != null)
            {
                Profiler.BeginSample("VFXEditor.OnInvalidateDelegate");
                try
                {
                    onInvalidateDelegate(model, cause);
                }
                finally
                {
                    Profiler.EndSample();
                }
            }
        }

        protected virtual void OnAdded() {}
        protected virtual void OnRemoved() {}

        public virtual bool AcceptChild(VFXModel model, int index = -1)
        {
            return false;
        }

        public void AddChild(VFXModel model, int index = -1, bool notify = true)
        {
            int realIndex = index == -1 ? m_Children.Count : index;
            if (model.m_Parent != this || realIndex != GetIndex(model))
            {
                if (!AcceptChild(model, index))
                    throw new ArgumentException("Cannot attach " + model + " to " + this);

                model.Detach(notify && model.m_Parent != this); // Dont notify if the owner is already this to avoid double invalidation
                realIndex = index == -1 ? m_Children.Count : index; // Recompute as the child may have been removed

                m_Children.Insert(realIndex, model);
                model.m_Parent = this;
                model.OnAdded();

                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        public void RemoveChild(VFXModel model, bool notify = true)
        {
            if (model.m_Parent != this)
                return;

            model.OnRemoved();
            m_Children.Remove(model);
            model.m_Parent = null;

            if (notify)
                Invalidate(InvalidationCause.kStructureChanged);
        }

        public void RemoveAllChildren(bool notify = true)
        {
            while (m_Children.Count > 0)
                RemoveChild(m_Children[m_Children.Count - 1], notify);
        }

        public VFXModel GetParent()
        {
            return m_Parent;
        }

        public T GetFirstOfType<T>() where T : VFXModel
        {
            if (this is T)
                return this as T;

            var parent = GetParent();

            if (parent == null)
                return null;

            return parent.GetFirstOfType<T>();
        }

        public void Attach(VFXModel parent, bool notify = true)
        {
            parent.AddChild(this, -1, notify);
        }

        public void Detach(bool notify = true)
        {
            if (m_Parent == null)
                return;

            m_Parent.RemoveChild(this, notify);
        }

        public IEnumerable<VFXModel> children
        {
            get { return m_Children; }
        }

        public VFXModel this[int index]
        {
            get { return m_Children[index]; }
        }

        public Vector2 position
        {
            get { return m_UIPosition; }
            set
            {
                if (m_UIPosition != value)
                {
                    m_UIPosition = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public bool collapsed
        {
            get { return m_UICollapsed; }
            set
            {
                if (m_UICollapsed != value)
                {
                    m_UICollapsed = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public bool superCollapsed
        {
            get { return m_UISuperCollapsed; }
            set
            {
                if (m_UISuperCollapsed != value)
                {
                    m_UISuperCollapsed = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public int GetNbChildren()
        {
            return m_Children.Count;
        }

        public int GetIndex(VFXModel child)
        {
            return m_Children.IndexOf(child);
        }

        public void SetSettingValue(string name, object value)
        {
            SetSettingValue(name, value, true);
        }

        protected void SetSettingValue(string name, object value, bool notify)
        {
            var field = GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                throw new ArgumentException(string.Format("Unable to find field {0} in {1}", name, GetType().ToString()));
            }

            var currentValue = field.GetValue(this);
            if (currentValue != value)
            {
                field.SetValue(this, value);
                if (notify)
                {
                    Invalidate(InvalidationCause.kSettingChanged);
                }
            }
        }

        public void Invalidate(InvalidationCause cause)
        {
            Modified();
            string sampleName = GetType().Name + "-" + name + "-" + cause;
            Profiler.BeginSample("VFXEditor.Invalidate" + sampleName);
            try
            {
                Invalidate(this, cause);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        protected virtual void Invalidate(VFXModel model, InvalidationCause cause)
        {
            OnInvalidate(model, cause);
            if (m_Parent != null)
                m_Parent.Invalidate(model, cause);
        }

        public IEnumerable<FieldInfo> GetSettings(bool listHidden, VFXSettingAttribute.VisibleFlags flags = VFXSettingAttribute.VisibleFlags.All)
        {
            return GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(f =>
            {
                var attrArray = f.GetCustomAttributes(typeof(VFXSettingAttribute), true);
                if (attrArray.Length == 1)
                {
                    var attr = attrArray[0] as VFXSettingAttribute;
                    if (listHidden)
                        return true;

                    return (attr.visibleFlags & flags) != 0 && !filteredOutSettings.Contains(f.Name);
                }
                return false;
            });
        }

        static public VFXExpression ConvertSpace(VFXExpression input, VFXSlot targetSlot, VFXCoordinateSpace space)
        {
            if (targetSlot.spaceable)
            {
                if (targetSlot.space != space)
                {
                    var spaceType = targetSlot.GetSpaceTransformationType();
                    input = ConvertSpace(input, spaceType, space);
                }
            }
            return input;
        }

        static protected VFXExpression ConvertSpace(VFXExpression input, SpaceableType spaceType, VFXCoordinateSpace space)
        {
            VFXExpression matrix = null;
            if (space == VFXCoordinateSpace.Local)
            {
                matrix = VFXBuiltInExpression.WorldToLocal;
            }
            else if (space == VFXCoordinateSpace.World)
            {
                matrix = VFXBuiltInExpression.LocalToWorld;
            }
            else
            {
                throw new InvalidOperationException("Cannot Convert to unknown space");
            }

            if (spaceType == SpaceableType.Position)
            {
                input = new VFXExpressionTransformPosition(matrix, input);
            }
            else if (spaceType == SpaceableType.Direction)
            {
                input = new VFXExpressionTransformDirection(matrix, input);
            }
            else if (spaceType == SpaceableType.Matrix)
            {
                input = new VFXExpressionTransformMatrix(matrix, input);
            }
            else if (spaceType == SpaceableType.Vector)
            {
                input = new VFXExpressionTransformVector(matrix, input);
            }
            else
            {
                //Not a transformable subSlot
            }
            return input;
        }

        protected virtual IEnumerable<string> filteredOutSettings
        {
            get
            {
                return Enumerable.Empty<string>();
            }
        }

        public VisualEffectResource GetResource()
        {
            var graph = GetGraph();
            if (graph != null)
                return graph.visualEffectResource;
            return null;
        }

        public VFXGraph GetGraph()
        {
            var graph = this as VFXGraph;
            if (graph != null)
                return graph;
            var parent = GetParent();
            if (parent != null)
                return parent.GetGraph();
            return null;
        }

        public static void UnlinkModel(VFXModel model, bool notify = true)
        {
            if (model is IVFXSlotContainer)
            {
                var slotContainer = (IVFXSlotContainer)model;
                VFXSlot slotToClean = null;
                do
                {
                    slotToClean = slotContainer.inputSlots.Concat(slotContainer.outputSlots).FirstOrDefault(o => o.HasLink(true));
                    if (slotToClean)
                        slotToClean.UnlinkAll(true, notify);
                }
                while (slotToClean != null);
            }
        }

        public static void RemoveModel(VFXModel model, bool notify = true)
        {
            VFXGraph graph = model.GetGraph();
            if (graph != null)        
                graph.UIInfos.Sanitize(graph); // Remove reference from groupInfos
            UnlinkModel(model);
            model.Detach(notify);
        }

        public static void ReplaceModel(VFXModel dst, VFXModel src, bool notify = true)
        {
            // UI
            dst.m_UIPosition = src.m_UIPosition;
            dst.m_UICollapsed = src.m_UICollapsed;
            dst.m_UISuperCollapsed = src.m_UISuperCollapsed;

            if (notify)
                dst.Invalidate(InvalidationCause.kUIChanged);

            VFXGraph graph = src.GetGraph();
            if (graph != null && graph.UIInfos != null && graph.UIInfos.groupInfos != null)
            {
                // Update group nodes
                foreach (var groupInfo in graph.UIInfos.groupInfos)
                    if (groupInfo.contents != null)
                        for (int i = 0; i < groupInfo.contents.Length; ++i)
                            if (groupInfo.contents[i].model == src)
                                groupInfo.contents[i].model = dst;
            }

            if (dst is VFXBlock && src is VFXBlock)
            {
                ((VFXBlock)dst).enabled = ((VFXBlock)src).enabled;
            }

            // Unlink everything
            UnlinkModel(src);

            // Replace model
            var parent = src.GetParent();
            int index = parent.GetIndex(src);
            src.Detach(notify);

            if (parent)
                parent.AddChild(dst, index, notify);
        }

        [SerializeField]
        protected VFXModel m_Parent;

        [SerializeField]
        protected List<VFXModel> m_Children;

        [SerializeField]
        protected Vector2 m_UIPosition;

        [SerializeField]
        protected bool m_UICollapsed;
        [SerializeField]
        protected bool m_UISuperCollapsed;
    }

    abstract class VFXModel<ParentType, ChildrenType> : VFXModel
        where ParentType : VFXModel
        where ChildrenType : VFXModel
    {
        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return index >= -1 && index <= m_Children.Count && model is ChildrenType;
        }

        public new ParentType GetParent()
        {
            return (ParentType)m_Parent;
        }

        public new int GetNbChildren()
        {
            return m_Children.Count;
        }

        public new ChildrenType this[int index]
        {
            get { return m_Children[index] as ChildrenType; }
        }

        public new IEnumerable<ChildrenType> children
        {
            get { return m_Children.Cast<ChildrenType>(); }
        }
    }
}
