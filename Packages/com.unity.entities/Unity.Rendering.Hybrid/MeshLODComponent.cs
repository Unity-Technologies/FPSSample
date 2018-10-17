using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Rendering
{
    public struct ActiveLODGroupMask : IComponentData
    {
        public int LODMask;
    }

    public struct MeshLODGroupComponent : IComponentData
    {
        public Entity    ParentGroup;
        public int       ParentMask;
        
        public float4    LODDistances;
        
        public float3    WorldReferencePoint;
    }
    

    public struct HLODComponent : IComponentData
    {
    }
    
    public struct MeshLODComponent : IComponentData
    {
        public Entity   Group;
        public int      LODMask;
    }
}
