using System.Collections.Generic;

namespace UnityEditor.Dot
{
    public abstract class DotElement
    {
        public abstract string Name { get; }

        public string Label
        {
            get
            {
                if (attributes.ContainsKey(DotAttribute.Label))
                    return attributes[DotAttribute.Label];
                return string.Empty;
            }
            set
            {
                attributes[DotAttribute.Label] = value;
            }
        }

        public Dictionary<string, string> attributes = new Dictionary<string, string>();

        public bool HasAttributes()
        {
            return attributes.Count > 0;
        }
    }

    public class DotNode : DotElement
    {
        public DotNode() {}
        public DotNode(string name)
        {
            Label = name;
        }

        public override string Name { get { return "node"; } }
    }

    public class DotEdge : DotElement
    {
        public DotEdge(DotNode from, DotNode to)
        {
            m_From = from;
            m_To = to;
        }

        public override string Name { get { return "edge"; } }

        public DotNode From { get { return m_From; } }
        public DotNode To   { get { return m_To; } }

        private DotNode m_From;
        private DotNode m_To;
    }
}
