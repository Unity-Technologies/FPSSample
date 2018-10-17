namespace Unity.Entities
{   
    /// <summary>
    /// Any associated components are ignored by the TSystem.
    /// </summary>
    public struct VoidSystem<TSystem> : IComponentData
    where TSystem : ComponentSystemBase
    {
    }
}
