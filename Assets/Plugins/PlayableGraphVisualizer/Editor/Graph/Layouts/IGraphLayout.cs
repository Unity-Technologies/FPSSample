using System.Collections.Generic;
using UnityEngine;

namespace GraphVisualizer
{
    // Interface for a generic graph layout.
    public interface IGraphLayout
    {
        IEnumerable<Vertex> vertices { get; }
        IEnumerable<Edge> edges { get; }

        bool leftToRight { get; }

        void CalculateLayout(Graph graph);
    }

    public class Edge
    {
        // Indices in the vertex array of the layout algorithm.
        public Edge(Vertex src, Vertex dest)
        {
            source = src;
            destination = dest;
        }

        public Vertex source { get; private set; }

        public Vertex destination { get; private set; }
    }

    // One vertex is associated to each node in the graph.
    public class Vertex
    {
        // Center of the node in the graph layout.
        public Vector2 position { get; set; }

        // The Node represented by the vertex.
        public Node node { get; private set; }

        public Vertex(Node node)
        {
            this.node = node;
        }
    }
}
