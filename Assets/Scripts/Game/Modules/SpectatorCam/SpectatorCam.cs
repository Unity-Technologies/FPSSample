using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SpectatorCamData : IComponentData, IReplicatedComponent
{
    public float3 position;
    public quaternion rotation;
    
    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<SpectatorCamData>();
    }

    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        writer.WriteVector3Q("pos",position,1);
        writer.WriteQuaternionQ("rot",rotation,1);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        position = reader.ReadVector3Q();
        rotation = reader.ReadQuaternionQ();
    }
}



public class SpectatorCam : ComponentDataProxy<SpectatorCamData>
{
    
}




public struct SpectatorCamSpawnRequest : IComponentData
{
    public Entity playerEntity;
    public Vector3 position;
    public Quaternion rotation;
    
    public static void Create(EntityCommandBuffer commandBuffer, Vector3 position, Quaternion rotation, Entity playerEntity)
    {
        var data = new SpectatorCamSpawnRequest()
        {
            playerEntity = playerEntity,
            position = position,
            rotation = rotation,
        };
        commandBuffer.CreateEntity();
        commandBuffer.AddComponent(data);
    }
}

[DisableAutoCreation]
public class UpdateSpectatorCam : BaseComponentSystem
{
    ComponentGroup Group;
    
    public UpdateSpectatorCam(GameWorld world) : base(world)
    {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(UserCommandComponentData), typeof(SpectatorCamData));
    }

    protected override void OnUpdate()
    {
        var spectatorCamEntityArray = Group.GetEntityArray();
        var spectatorCamArray = Group.GetComponentDataArray<SpectatorCamData>();
        var userCommandArray = Group.GetComponentDataArray<UserCommandComponentData>();
        for (var i = 0; i < spectatorCamArray.Length; i++)
        {
            var command = userCommandArray[i].command;
            var spectatorCam = spectatorCamArray[i];

            spectatorCam.rotation = Quaternion.Euler(new Vector3(90 - command.lookPitch, command.lookYaw, 0));

            var forward = math.mul(spectatorCam.rotation,Vector3.forward);
            var right = math.mul(spectatorCam.rotation,Vector3.right);
            var maxVel = 3 * m_world.worldTime.tickInterval;
            var moveDir = forward * Mathf.Cos(command.moveYaw*Mathf.Deg2Rad)  + right * Mathf.Sin(command.moveYaw*Mathf.Deg2Rad);
            spectatorCam.position += moveDir * maxVel * command.moveMagnitude;

            EntityManager.SetComponentData(spectatorCamEntityArray[i], spectatorCam);
        }            
    }
}


[DisableAutoCreation]
public class HandleSpectatorCamRequests : BaseComponentSystem
{
    ComponentGroup Group;   

    public HandleSpectatorCamRequests(GameWorld world, BundledResourceManager resourceManager) : base(world)
    {
        m_ResourceManager = resourceManager;
        m_Settings = Resources.Load<SpectatorCamSettings>("SpectatorCamSettings");
    }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(SpectatorCamSpawnRequest));
    }

    protected override void OnUpdate()
    {
        var requestArray = Group.GetComponentDataArray<SpectatorCamSpawnRequest>();
        if (requestArray.Length == 0)
            return;

        var entityArray = Group.GetEntityArray();

        
        // Copy requests as spawning will invalidate Group
        var spawnRequests = new SpectatorCamSpawnRequest[requestArray.Length];
        for (var i = 0; i < requestArray.Length; i++)
        {
            spawnRequests[i] = requestArray[i];
            PostUpdateCommands.DestroyEntity(entityArray[i]);
        }

        for(var i =0;i<spawnRequests.Length;i++)
        {
            var request = spawnRequests[i];
            var playerState = EntityManager.GetComponentObject<PlayerState>(request.playerEntity);
            
            
            
            var resource = m_ResourceManager.GetSingleAssetResource(m_Settings.spectatorCamPrefab);

            GameDebug.Assert(resource != null);


            var prefab = (GameObject)resource;
            GameDebug.Log("Spawning spectatorcam");
            
            
            var goe = m_world.Spawn<GameObjectEntity>(prefab);
            goe.name = prefab.name;
            var entity = goe.Entity;

            var spectatorCam = EntityManager.GetComponentData<SpectatorCamData>(entity);
            spectatorCam.position = request.position;
            spectatorCam.rotation = request.rotation;
            EntityManager.SetComponentData(entity,spectatorCam);
            
            playerState.controlledEntity = entity; 
        }
    }

    readonly SpectatorCamSettings m_Settings;
    readonly BundledResourceManager m_ResourceManager;
}

