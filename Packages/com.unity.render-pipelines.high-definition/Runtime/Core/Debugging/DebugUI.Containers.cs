namespace UnityEngine.Experimental.Rendering
{
    public partial class DebugUI
    {
        // Base class for "container" type widgets, although it can be used on its own (if a display
        // name is set then it'll behave as a group with a header)
        public class Container : Widget, IContainer
        {
            public ObservableList<Widget> children { get; private set; }

            public override Panel panel
            {
                get { return m_Panel; }
                internal set
                {
                    m_Panel = value;

                    // Bubble down
                    foreach (var child in children)
                        child.panel = value;
                }
            }

            public Container()
            {
                displayName = "";
                children = new ObservableList<Widget>();
                children.ItemAdded += OnItemAdded;
                children.ItemRemoved += OnItemRemoved;
            }

            internal override void GenerateQueryPath()
            {
                base.GenerateQueryPath();

                foreach (var child in children)
                    child.GenerateQueryPath();
            }

            protected virtual void OnItemAdded(ObservableList<Widget> sender, ListChangedEventArgs<Widget> e)
            {
                if (e.item != null)
                {
                    e.item.panel = m_Panel;
                    e.item.parent = this;
                }

                if (m_Panel != null)
                    m_Panel.SetDirty();
            }

            protected virtual void OnItemRemoved(ObservableList<Widget> sender, ListChangedEventArgs<Widget> e)
            {
                if (e.item != null)
                {
                    e.item.panel = null;
                    e.item.parent = null;
                }

                if (m_Panel != null)
                    m_Panel.SetDirty();
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 23 + queryPath.GetHashCode();

                foreach (var child in children)
                    hash = hash * 23 + child.GetHashCode();

                return hash;
            }
        }

        // Unity-like foldout that can be collapsed
        public class Foldout : Container, IValueField
        {
            public bool isReadOnly { get { return false; } }

            public bool opened;

            public bool GetValue()
            {
                return opened;
            }

            object IValueField.GetValue()
            {
                return GetValue();
            }

            public void SetValue(object value)
            {
                SetValue((bool)value);
            }

            public object ValidateValue(object value)
            {
                return value;
            }

            public void SetValue(bool value)
            {
                opened = value;
            }
        }

        // Horizontal layout
        public class HBox : Container
        {
            public HBox()
            {
                displayName = "HBox";
            }
        }

        // Vertical layout
        public class VBox : Container
        {
            public VBox()
            {
                displayName = "VBox";
            }
        }
    }
}
