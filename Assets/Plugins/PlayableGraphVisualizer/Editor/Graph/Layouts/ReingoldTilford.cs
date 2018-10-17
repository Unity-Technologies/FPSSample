using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GraphVisualizer
{
    // Implementation of Reingold and Tilford algorithm for graph layout
    // "Tidier Drawings of Trees", IEEE Transactions on Software Engineering Vol SE-7 No.2, March 1981
    // The implementation has been customized to support graphs with multiple roots and unattached nodes.
    public class ReingoldTilford : IGraphLayout
    {
        // By convention, all graph layout algorithms should have a minimum distance of 1 unit between nodes
        private static readonly float s_DistanceBetweenNodes = 1.0f;

        // Used to specify the vertical distance two non-attached trees in the graph.
        private static readonly float s_VerticalDistanceBetweenTrees = 3.0f;

        // Used to lengthen the wire when lots of children are connected. If 1, all levels will be evenly separated
        private static readonly float s_WireLengthFactorForLargeSpanningTrees = 3.0f;

        private static readonly float s_MaxChildrenThreshold = 6.0f;

        // Helper structure to easily find the vertex associated to a given Node.
        private readonly Dictionary<Node, Vertex> m_NodeVertexLookup = new Dictionary<Node, Vertex>();

        public ReingoldTilford(bool leftToRight = true)
        {
            this.leftToRight = leftToRight;
        }

        public IEnumerable<Vertex> vertices
        {
            get { return m_NodeVertexLookup.Values; }
        }

        public IEnumerable<Edge> edges
        {
            get
            {
                var edgesList = new List<Edge>();
                foreach (var node in m_NodeVertexLookup)
                {
                    Vertex v = node.Value;
                    foreach (Node child in v.node.children)
                    {
                        edgesList.Add(new Edge(m_NodeVertexLookup[child], v));
                    }
                }
                return edgesList;
            }
        }

        public bool leftToRight { get; private set; }

        // Main entry point of the algorithm
        public void CalculateLayout(Graph graph)
        {
            m_NodeVertexLookup.Clear();
            foreach (Node node in graph)
            {
                m_NodeVertexLookup.Add(node, new Vertex(node));
            }

            if (m_NodeVertexLookup.Count == 0) return;

            IList<float> horizontalPositions = ComputeHorizontalPositionForEachLevel();

            List<Node> roots = m_NodeVertexLookup.Keys.Where(n => n.parent == null).ToList();

            for (int i = 0; i < roots.Count; ++i)
            {
                RecursiveLayout(roots[i], 0, horizontalPositions);

                if (i > 0)
                {
                    Vector2 previousRootRange = ComputeRangeRecursive(roots[i - 1]);
                    RecursiveMoveSubtree(roots[i], previousRootRange.y + s_VerticalDistanceBetweenTrees + s_DistanceBetweenNodes);
                }
            }
        }

        // Precompute the horizontal position for each level.
        // Levels with few wires (as measured by the maximum number of children for one node) are placed closer
        // apart; very cluttered levels are placed further apart.
        private float[] ComputeHorizontalPositionForEachLevel()
        {
            // Gather information about depths.
            var maxDepth = int.MinValue;
            var nodeDepths = new Dictionary<int, List<Node>>();
            foreach (Node node in m_NodeVertexLookup.Keys)
            {
                int d = node.depth;
                List<Node> nodes;
                if (!nodeDepths.TryGetValue(d, out nodes))
                {
                    nodeDepths[d] = nodes = new List<Node>();
                }
                nodes.Add(node);
                maxDepth = Mathf.Max(d, maxDepth);
            }

            // Bake the left to right horizontal positions.
            var horizontalPositionForDepth = new float[maxDepth];
            horizontalPositionForDepth[0] = 0;
            for (int d = 1; d < maxDepth; ++d)
            {
                IEnumerable<Node> nodesOnThisLevel = nodeDepths[d + 1];

                int maxChildren = nodesOnThisLevel.Max(x => x.children.Count);

                float wireLengthHeuristic = Mathf.Lerp(1, s_WireLengthFactorForLargeSpanningTrees,
                        Mathf.Min(1, maxChildren / s_MaxChildrenThreshold));

                horizontalPositionForDepth[d] = horizontalPositionForDepth[d - 1] +
                    s_DistanceBetweenNodes * wireLengthHeuristic;
            }

            return leftToRight ? horizontalPositionForDepth : horizontalPositionForDepth.Reverse().ToArray();
        }

        // Traverse the graph and place all nodes according to the algorithm
        private void RecursiveLayout(Node node, int depth, IList<float> horizontalPositions)
        {
            IList<Node> children = node.children;
            foreach (Node child in children)
            {
                RecursiveLayout(child, depth + 1, horizontalPositions);
            }

            var yPos = 0.0f;
            if (children.Count > 0)
            {
                SeparateSubtrees(children);
                yPos = GetAveragePosition(children).y;
            }

            var pos = new Vector2(horizontalPositions[depth], yPos);
            m_NodeVertexLookup[node].position = pos;
        }

        private Vector2 ComputeRangeRecursive(Node node)
        {
            var range = Vector2.one * m_NodeVertexLookup[node].position.y;
            foreach (Node child in node.children)
            {
                Vector2 childRange =  ComputeRangeRecursive(child);
                range.x = Mathf.Min(range.x, childRange.x);
                range.y = Mathf.Max(range.y, childRange.y);
            }
            return range;
        }

        // Determine parent's vertical position based on its children
        private Vector2 GetAveragePosition(ICollection<Node> children)
        {
            Vector2 centroid = new Vector2();

            centroid = children.Aggregate(centroid, (current, n) => current + m_NodeVertexLookup[n].position);

            if (children.Count > 0)
                centroid /= children.Count;

            return centroid;
        }

        // Separate the given subtrees so they do not overlap
        private void SeparateSubtrees(IList<Node> subroots)
        {
            if (subroots.Count < 2)
                return;

            Node upperNode = subroots[0];

            Dictionary<int, Vector2> upperTreeBoundaries = GetBoundaryPositions(upperNode);
            for (int s = 0; s < subroots.Count - 1; s++)
            {
                Node lowerNode = subroots[s + 1];
                Dictionary<int, Vector2> lowerTreeBoundaries = GetBoundaryPositions(lowerNode);

                int minDepth = upperTreeBoundaries.Keys.Min();
                if (minDepth != lowerTreeBoundaries.Keys.Min())
                    Debug.LogError("Cannot separate subtrees which do not start at the same root depth");

                int lowerMaxDepth = lowerTreeBoundaries.Keys.Max();
                int upperMaxDepth = upperTreeBoundaries.Keys.Max();
                int maxDepth = System.Math.Min(upperMaxDepth, lowerMaxDepth);

                for (int depth = minDepth; depth <= maxDepth; depth++)
                {
                    float delta = s_DistanceBetweenNodes - (lowerTreeBoundaries[depth].x - upperTreeBoundaries[depth].y);
                    delta = System.Math.Max(delta, 0);
                    RecursiveMoveSubtree(lowerNode, delta);
                    for (int i = minDepth; i <= lowerMaxDepth; i++)
                        lowerTreeBoundaries[i] += new Vector2(delta, delta);
                }
                upperTreeBoundaries = CombineBoundaryPositions(upperTreeBoundaries, lowerTreeBoundaries);
            }
        }

        // Using a Vector2 at each depth to hold the extrema vertical positions
        private Dictionary<int, Vector2> GetBoundaryPositions(Node subTreeRoot)
        {
            var extremePositions = new Dictionary<int, Vector2>();

            IEnumerable<Node> descendants = GetSubtreeNodes(subTreeRoot);

            foreach (var node in descendants)
            {
                int depth = m_NodeVertexLookup[node].node.depth;
                float pos =  m_NodeVertexLookup[node].position.y;
                if (extremePositions.ContainsKey(depth))
                    extremePositions[depth] = new Vector2(Mathf.Min(extremePositions[depth].x, pos),
                            Mathf.Max(extremePositions[depth].y, pos));
                else
                    extremePositions[depth] = new Vector2(pos, pos);
            }

            return extremePositions;
        }

        // Includes all descendants and the subtree root itself
        private IEnumerable<Node> GetSubtreeNodes(Node root)
        {
            var allDescendants = new List<Node> { root };
            foreach (Node child in root.children)
            {
                allDescendants.AddRange(GetSubtreeNodes(child));
            }
            return allDescendants;
        }

        // After adjusting a subtree, compute its new boundary positions
        private Dictionary<int, Vector2> CombineBoundaryPositions(Dictionary<int, Vector2> upperTree, Dictionary<int, Vector2> lowerTree)
        {
            var combined = new Dictionary<int, Vector2>();
            int minDepth = upperTree.Keys.Min();
            int maxDepth = System.Math.Max(upperTree.Keys.Max(), lowerTree.Keys.Max());

            for (int d = minDepth; d <= maxDepth; d++)
            {
                float upperBoundary = upperTree.ContainsKey(d) ? upperTree[d].x : lowerTree[d].x;
                float lowerBoundary = lowerTree.ContainsKey(d) ? lowerTree[d].y : upperTree[d].y;
                combined[d] = new Vector2(upperBoundary, lowerBoundary);
            }
            return combined;
        }

        // Apply a vertical delta to all nodes in a subtree
        private void RecursiveMoveSubtree(Node subtreeRoot, float yDelta)
        {
            Vector2 pos = m_NodeVertexLookup[subtreeRoot].position;
            m_NodeVertexLookup[subtreeRoot].position = new Vector2(pos.x, pos.y + yDelta);

            foreach (Node child in subtreeRoot.children)
            {
                RecursiveMoveSubtree(child, yDelta);
            }
        }
    }
}
