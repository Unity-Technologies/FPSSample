using System;

namespace UnityEngine.Experimental.Rendering
{
    public partial class DebugUI
    {
        // Root panel class - we don't want to extend Container here because we need a clear
        // separation between debug panels and actual widgets
        public class Panel : IContainer
        {
            public Flags flags { get; set; }
            public string displayName { get; set; }
            public string queryPath { get { return displayName; } }

            public bool isEditorOnly { get { return (flags & Flags.EditorOnly) != 0; } }
            public bool isRuntimeOnly { get { return (flags & Flags.RuntimeOnly) != 0; } }
            public bool editorForceUpdate { get { return (flags & Flags.EditorForceUpdate) != 0; } }

            public ObservableList<Widget> children { get; private set; }
            public event Action<Panel> onSetDirty = delegate {};

            public Panel()
            {
                children = new ObservableList<Widget>();
                children.ItemAdded += OnItemAdded;
                children.ItemRemoved += OnItemRemoved;
            }

            protected virtual void OnItemAdded(ObservableList<Widget> sender, ListChangedEventArgs<Widget> e)
            {
                if (e.item != null)
                {
                    e.item.panel = this;
                    e.item.parent = this;
                }

                SetDirty();
            }

            protected virtual void OnItemRemoved(ObservableList<Widget> sender, ListChangedEventArgs<Widget> e)
            {
                if (e.item != null)
                {
                    e.item.panel = null;
                    e.item.parent = null;
                }

                SetDirty();
            }

            public void SetDirty()
            {
                foreach (var child in children)
                    child.GenerateQueryPath();

                onSetDirty(this);
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 23 + displayName.GetHashCode();

                foreach (var child in children)
                    hash = hash * 23 + child.GetHashCode();

                return hash;
            }
        }
    }
}
