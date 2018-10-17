using System;
using System.Collections.Generic;

namespace UnityEditor.Graphing
{
    public interface IGraph : IOnAssetEnabled
    {
        IEnumerable<T> GetNodes<T>() where T : INode;
        IEnumerable<IEdge> edges { get; }
        void AddNode(INode node);
        void RemoveNode(INode node);
        IEdge Connect(SlotReference fromSlotRef, SlotReference toSlotRef);
        void RemoveEdge(IEdge e);
        void RemoveElements(IEnumerable<INode> nodes, IEnumerable<IEdge> edges);
        INode GetNodeFromGuid(Guid guid);
        bool ContainsNodeGuid(Guid guid);
        T GetNodeFromGuid<T>(Guid guid) where T : INode;
        void GetEdges(SlotReference s, List<IEdge> foundEdges);
        void ValidateGraph();
        void ReplaceWith(IGraph other);
        IGraphObject owner { get; set; }
        IEnumerable<INode> addedNodes { get; }
        IEnumerable<INode> removedNodes { get; }
        IEnumerable<IEdge> addedEdges { get; }
        IEnumerable<IEdge> removedEdges { get; }
        void ClearChanges();
    }

    public static class GraphExtensions
    {
        public static IEnumerable<IEdge> GetEdges(this IGraph graph, SlotReference s)
        {
            var edges = new List<IEdge>();
            graph.GetEdges(s, edges);
            return edges;
        }
    }
}
