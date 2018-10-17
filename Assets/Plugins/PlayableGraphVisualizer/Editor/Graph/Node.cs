using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GraphVisualizer
{
    public class Node
    {
        public object content { get; private set; }
        public float weight { get; set; }
        public bool active { get; private set; }
        public Node parent { get; private set; }
        public IList<Node> children { get; private set; }

        public Node(object content, float weight = 1.0f, bool active = false)
        {
            this.content = content;
            this.weight = weight;
            this.active = active;
            children = new List<Node>();
        }

        public void AddChild(Node child)
        {
            if (child == this) throw new Exception("Circular graphs not supported.");
            if (child.parent == this) return;

            children.Add(child);
            child.parent = this;
        }

        public int depth
        {
            get { return GetDepthRecursive(this); }
        }

        private static int GetDepthRecursive(Node node)
        {
            if (node.parent == null) return 1;
            return 1 + GetDepthRecursive(node.parent);
        }

        public virtual Type GetContentType()
        {
            return content == null ? null : content.GetType();
        }

        public virtual string GetContentTypeName()
        {
            Type type = GetContentType();
            return type == null ? "Null" : type.ToString();
        }

        public virtual string GetContentTypeShortName()
        {
            return GetContentTypeName().Split('.').Last();
        }

        public override string ToString()
        {
            return "Node content: " + GetContentTypeName();
        }

        public virtual Color GetColor()
        {
            Type type = GetContentType();
            if (type == null)
                return Color.red;

            string shortName = type.ToString().Split('.').Last();
            float h = (float)Math.Abs(shortName.GetHashCode()) / int.MaxValue;
            return Color.HSVToRGB(h, 0.6f, 1.0f);
        }
    }
}
