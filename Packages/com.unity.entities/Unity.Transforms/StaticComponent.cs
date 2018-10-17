using Unity.Entities;

namespace Unity.Transforms
{
    /// <summary>
    /// When added, TransformSystem will process transform compomnnts once
    /// to update LocalToWorld. Once that is resolved, the Frozen
    /// component will be added and LocalToWorld will no longer be updated.
    /// </summary>
    public struct Static : IComponentData
    {
    }

    public class StaticComponent : ComponentDataWrapper<Static>
    {
    }
}
