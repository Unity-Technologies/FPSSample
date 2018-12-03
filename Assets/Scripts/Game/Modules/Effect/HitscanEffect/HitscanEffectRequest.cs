using UnityEngine;
using Unity.Entities;

public struct HitscanEffectRequest : IComponentData 
{
    public int effectTypeRegistryId;
    public Vector3 startPos;
    public Vector3 endPos;

    public static void Create(EntityCommandBuffer commandBuffer, HitscanEffectTypeDefinition definition, Vector3 startPos, Vector3 endPos)
    {
        var request = new HitscanEffectRequest();
        request.effectTypeRegistryId = definition.registryId;
        request.startPos = startPos;
        request.endPos = endPos;

        commandBuffer.CreateEntity();
        commandBuffer.AddComponent(request);
    }
}