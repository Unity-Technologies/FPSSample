using UnityEngine;
using Unity.Entities;

public struct SpatialEffectRequest : IComponentData 
{
    public int effectTypeRegistryId;
    public Vector3 position;
    public Quaternion rotation;

    public SpatialEffectRequest(SpatialEffectTypeDefinition definition, Vector3 position, Quaternion rotation)
    {
        effectTypeRegistryId = definition.registryId;
        this.position = position;
        this.rotation = rotation;
    }

    public static void Create(EntityCommandBuffer commandBuffer, SpatialEffectTypeDefinition definition, Vector3 position, Quaternion rotation)
    {
        var request = new SpatialEffectRequest(definition, position, rotation);
        commandBuffer.CreateEntity();
        commandBuffer.AddComponent(request);
    }
}
