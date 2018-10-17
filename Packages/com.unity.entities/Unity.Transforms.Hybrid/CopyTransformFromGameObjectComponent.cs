using Unity.Entities;

namespace Unity.Transforms
{
    /// <summary>
    /// Copy Transform from GameObject associated with Entity to TransformMatrix.
    /// </summary>
    public struct CopyTransformFromGameObject : IComponentData { }

    public class CopyTransformFromGameObjectComponent : ComponentDataWrapper<CopyTransformFromGameObject> { } 
}
