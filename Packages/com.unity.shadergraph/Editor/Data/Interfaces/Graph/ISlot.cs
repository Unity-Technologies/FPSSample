using System;

namespace UnityEditor.Graphing
{
    public interface ISlot : IEquatable<ISlot>
    {
        int id { get; }
        string displayName { get; set; }
        bool isInputSlot { get; }
        bool isOutputSlot { get; }
        int priority { get; set; }
        SlotReference slotReference { get; }
        INode owner { get; set; }
        bool hidden { get; set; }
    }
}
