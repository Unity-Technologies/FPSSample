using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Graphing
{
    public enum ModificationScope
    {
        Nothing = 0,
        Node = 1,
        Graph = 2,
        Topological = 3
    }

    public delegate void OnNodeModified(INode node, ModificationScope scope);

    public interface INode
    {
        void RegisterCallback(OnNodeModified callback);
        void UnregisterCallback(OnNodeModified callback);
        void Dirty(ModificationScope scope);
        IGraph owner { get; set; }
        Guid guid { get; }
        Identifier tempId { get; set; }
        Guid RewriteGuid();
        string name { get; set; }
        bool canDeleteNode { get; }
        void GetInputSlots<T>(List<T> foundSlots) where T : ISlot;
        void GetOutputSlots<T>(List<T> foundSlots) where T : ISlot;
        void GetSlots<T>(List<T> foundSlots) where T : ISlot;
        void AddSlot(ISlot slot);
        void RemoveSlot(int slotId);
        SlotReference GetSlotReference(int slotId);
        T FindSlot<T>(int slotId) where T : ISlot;
        T FindInputSlot<T>(int slotId) where T : ISlot;
        T FindOutputSlot<T>(int slotId) where T : ISlot;
        IEnumerable<ISlot> GetInputsWithNoConnection();
        DrawState drawState { get; set; }
        bool hasError { get; }
        void ValidateNode();
        void UpdateNodeAfterDeserialization();
    }

    public static class NodeExtensions
    {
        public static IEnumerable<T> GetSlots<T>(this INode node) where T : ISlot
        {
            var slots = new List<T>();
            node.GetSlots(slots);
            return slots;
        }

        public static IEnumerable<T> GetInputSlots<T>(this INode node) where T : ISlot
        {
            var slots = new List<T>();
            node.GetInputSlots(slots);
            return slots;
        }

        public static IEnumerable<T> GetOutputSlots<T>(this INode node) where T : ISlot
        {
            var slots = new List<T>();
            node.GetOutputSlots(slots);
            return slots;
        }
    }
}
