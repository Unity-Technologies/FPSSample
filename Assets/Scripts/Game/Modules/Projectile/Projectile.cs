using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Component set on projectiles that should be updated. Client only puts this on predicted projectiles
public struct UpdateProjectileFlag : IComponentData    
{
    public int foo;
}

public struct ProjectileData : IComponentData, INetSerialized
{
    public int test;        // TODO remove this test no longer needed  
    public int projectileTypeRegistryId;
    public Entity projectileOwner;
    public int startTick;
    public float3 startPos;
    public float3 endPos;  
    public int impacted;
    public float3 impactPos;
    public float3 impactNormal;
    
    public void Serialize(ref NetworkWriter networkWriter, IEntityReferenceSerializer refSerializer)
    {
        refSerializer.SerializeReference(ref networkWriter, "owner", projectileOwner);
        networkWriter.WriteUInt16("typeId", (ushort)projectileTypeRegistryId);
        networkWriter.WriteInt32("startTick", startTick);
        networkWriter.WriteVector3Q("startPosition", startPos,2);
        networkWriter.WriteVector3Q("endPosition", endPos,2);
        networkWriter.WriteBoolean("impacted", impacted == 1);
        networkWriter.WriteVector3Q("impactPosition", impactPos,2);
        networkWriter.WriteVector3Q("impactNormal", impactNormal,2);
    }

    public void Deserialize(ref NetworkReader networkReader, IEntityReferenceSerializer refSerializer, int tick)
    {
        refSerializer.DeserializeReference(ref networkReader, ref projectileOwner);
        projectileTypeRegistryId = networkReader.ReadUInt16();
        startTick = networkReader.ReadInt32();
        startPos = networkReader.ReadVector3Q();
        endPos = networkReader.ReadVector3Q();
        impacted = networkReader.ReadBoolean() ? 1 : 0;
        impactPos = networkReader.ReadVector3Q();
        impactNormal = networkReader.ReadVector3Q();
    }
    
    // State properties  
    public int rayQueryId;
    public int teamId;
    public int collisionCheckTickDelay;    
    public float3 position;
    public ProjectileSettings settings;
    public float maxAge;
    public int impactTick;
    
    public void SetupFromRequest(ProjectileRequest request)
    {
        rayQueryId = -1;
        projectileOwner = request.owner;
        projectileTypeRegistryId = request.projectileTypeRegistryId;
        startTick = request.startTick;
        startPos = request.startPosition;
        endPos = request.endPosition;
        teamId = request.teamId;
        collisionCheckTickDelay = request.collisionTestTickDelay;
    }

    public void Initialize(ProjectileRegistry registry) 
    {
        settings = registry.GetEntryById(projectileTypeRegistryId).properties;
        
        maxAge = Vector3.Magnitude(endPos - startPos) / settings.velocity;
        position = startPos;
    }
}    
