using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GraphVisualizer
{
    public abstract class Graph : IEnumerable<Node>
    {
        private readonly List<Node> m_Nodes = new List<Node>();

        public ReadOnlyCollection<Node> nodes
        {
            get { return m_Nodes.AsReadOnly(); }
        }

        protected class NodeWeight
        {
            public object node { get; set; }
            public float weight { get; set; }
        }

        // Derived class should specify the children of a given node.
        protected abstract IEnumerable<Node> GetChildren(Node node);

        // Derived class should implement how to populate this graph (usually by calling AddNodeHierarchy()).
        protected abstract void Populate();

        public void AddNodeHierarchy(Node root)
        {
            AddNode(root);

            IEnumerable<Node> children = GetChildren(root);
            if (children == null)
                return;

            foreach (Node child in children)
            {
                root.AddChild(child);
                AddNodeHierarchy(child);
            }
        }

        public void AddNode(Node node)
        {
            m_Nodes.Add(node);
        }

        public void Clear()
        {
            m_Nodes.Clear();
        }

        public void Refresh()
        {
            // TODO optimize?
            Clear();
            Populate();
        }

        public IEnumerator<Node> GetEnumerator()
        {
            return m_Nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Nodes.GetEnumerator();
        }

        public bool IsEmpty()
        {
            return m_Nodes.Count == 0;
        }
    }
}
