using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [Serializable]
    public abstract class DebugState : ScriptableObject
    {
        [SerializeField]
        protected string m_QueryPath;

        // We need this to keep track of the state modified in the current frame.
        // This helps reduces the cost of re-applying states to original widgets and is also needed
        // when two states point to the same value (e.g. when using split enums like HDRP does for
        // the `fullscreenDebugMode`.
        internal static DebugState m_CurrentDirtyState;

        public string queryPath
        {
            get { return m_QueryPath; }
            internal set { m_QueryPath = value; }
        }

        public abstract object GetValue();

        public abstract void SetValue(object value, DebugUI.IValueField field);

        public virtual void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }
    }

    [Serializable]
    public class DebugState<T> : DebugState
    {
        [SerializeField]
        protected T m_Value;

        public virtual T value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override object GetValue()
        {
            return value;
        }

        public override void SetValue(object value, DebugUI.IValueField field)
        {
            this.value = (T)field.ValidateValue(value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;
                hash = hash * 23 + m_QueryPath.GetHashCode();
                hash = hash * 23 + m_Value.GetHashCode();
                return hash;
            }
        }
    }

    public sealed class DebugStateAttribute : Attribute
    {
        public readonly Type[] types;

        public DebugStateAttribute(params Type[] types)
        {
            this.types = types;
        }
    }

    // Builtins
    [Serializable, DebugState(typeof(DebugUI.BoolField), typeof(DebugUI.Foldout))]
    public sealed class DebugStateBool : DebugState<bool> {}

    [Serializable, DebugState(typeof(DebugUI.IntField), typeof(DebugUI.EnumField))]
    public sealed class DebugStateInt : DebugState<int> {}

    [Serializable, DebugState(typeof(DebugUI.UIntField))]
    public sealed class DebugStateUInt : DebugState<uint> {}

    [Serializable, DebugState(typeof(DebugUI.FloatField))]
    public sealed class DebugStateFloat : DebugState<float> {}

    [Serializable, DebugState(typeof(DebugUI.ColorField))]
    public sealed class DebugStateColor : DebugState<Color> {}

    [Serializable, DebugState(typeof(DebugUI.Vector2Field))]
    public sealed class DebugStateVector2 : DebugState<Vector2> {}

    [Serializable, DebugState(typeof(DebugUI.Vector3Field))]
    public sealed class DebugStateVector3 : DebugState<Vector3> {}

    [Serializable, DebugState(typeof(DebugUI.Vector4Field))]
    public sealed class DebugStateVector4 : DebugState<Vector4> {}
}
