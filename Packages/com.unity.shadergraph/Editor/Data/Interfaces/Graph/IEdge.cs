using System;

namespace UnityEditor.Graphing
{
    public interface IEdge : IEquatable<IEdge>
    {
        SlotReference outputSlot { get; }
        SlotReference inputSlot { get; }
    }
}
