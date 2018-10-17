using Unity.Mathematics;
using Unity.Entities;

namespace Unity.Rendering
{
    public struct MeshRenderBounds : IComponentData
    {
        public float3 Center;
        public float Radius;
    }
    
    public struct WorldMeshRenderBounds : IComponentData
    {
        public float3 Center;
        public float Radius;
    }
}
