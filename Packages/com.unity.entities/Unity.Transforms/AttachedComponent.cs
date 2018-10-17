using Unity.Entities;

namespace Unity.Transforms
{
    /// <summary>
    /// Added by TransformSystem to child when Attach component is resolved.
    /// To detach from Parent, remove.
    /// </summary>
    public struct Attached : IComponentData
    {
    }
}
