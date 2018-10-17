using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Transforms
{
    /// <summary>
    /// If is not previously added, LocalToWorld is added by system if Rotation +/- Position +/- Scale exist.
    /// Updated by system.
    /// Read-only from external systems.
    /// User responsible for removing.
    /// </summary>
    [Serializable]
    public struct LocalToWorld : IComponentData
    {
        public float4x4 Value;
    }

    public class LocalToWorldComponent : ComponentDataWrapper<LocalToWorld>
    {
    }
}
