using System;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering
{
    public partial class DebugUI
    {
        [Flags]
        public enum Flags
        {
            None        = 0,
            EditorOnly  = 1 << 1,
            RuntimeOnly = 1 << 2,
            EditorForceUpdate = 1 << 3
        }

        // Base class for all debug UI widgets
        public abstract class Widget
        {
            // Set to null until it's added to a panel, be careful
            protected Panel m_Panel;
            public virtual Panel panel
            {
                get { return m_Panel; }
                internal set { m_Panel = value; }
            }

            protected IContainer m_Parent;
            public virtual IContainer parent
            {
                get { return m_Parent; }
                internal set { m_Parent = value; }
            }

            public Flags flags { get; set; }
            public string displayName { get; set; }

            public string queryPath { get; private set; }

            public bool isEditorOnly { get { return (flags & Flags.EditorOnly) != 0; } }
            public bool isRuntimeOnly { get { return (flags & Flags.RuntimeOnly) != 0; } }

            internal virtual void GenerateQueryPath()
            {
                queryPath = displayName.Trim();

                if (m_Parent != null)
                    queryPath = m_Parent.queryPath + " -> " + queryPath;
            }

            public override int GetHashCode()
            {
                return queryPath.GetHashCode();
            }

            public void RemoveSelf()
            {
                if (parent != null)
                    parent.children.Remove(this);
            }
        }

        // Any widget that can holds other widgets must implement this interface
        public interface IContainer
        {
            ObservableList<Widget> children { get; }
            string displayName { get; set; }
            string queryPath { get; }
        }

        // Any widget that implements this will be considered for serialization (only if the setter
        // is set and thus is not read-only)
        public interface IValueField
        {
            object GetValue();
            void SetValue(object value);
            object ValidateValue(object value);
        }

        // Miscellaneous
        public class Button : Widget
        {
            public Action action { get; set; }
        }

        public class Value : Widget
        {
            public Func<object> getter { get; set; }

            // Runtime-only
            public float refreshRate = 0.1f;

            public object GetValue()
            {
                Assert.IsNotNull(getter);
                return getter();
            }
        }
    }
}
