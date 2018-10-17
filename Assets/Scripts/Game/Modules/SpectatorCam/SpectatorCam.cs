using Unity.Entities;
using UnityEngine;

public class SpectatorCam : MonoBehaviour, INetworkSerializable
{
    public Vector3 position;
    public Quaternion rotation;


    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteVector3Q("pos",position,1);
        writer.WriteQuaternionQ("rot",rotation,1);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        position = reader.ReadVector3Q();
        rotation = reader.ReadQuaternionQ();
    }
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
    struct GroupType
    {
        public ComponentArray<UserCommandComponent> userCommands;
        public ComponentArray<SpectatorCam> spectatorCams;
    }

    [Inject]
    GroupType Group;
    
    public UpdateSpectatorCam(GameWorld world) : base(world)
    {
    }

    protected override void OnUpdate()
    {
        for (var i = 0; i < Group.spectatorCams.Length; i++)
        {
            var command = Group.userCommands[i].command;
            var spectatorCam = Group.spectatorCams[i];

            spectatorCam.rotation = Quaternion.Euler(new Vector3(90 - command.lookPitch, command.lookYaw, 0));

            var forward = spectatorCam.rotation * Vector3.forward;
            var right = spectatorCam.rotation * Vector3.right;
            var maxVel = 3 * m_world.worldTime.tickInterval;
            var moveDir = forward * Mathf.Cos(command.moveYaw*Mathf.Deg2Rad)  + right * Mathf.Sin(command.moveYaw*Mathf.Deg2Rad);
            spectatorCam.position += moveDir * maxVel * command.moveMagnitude;
        }            
    }
}


[DisableAutoCreation]
public class HandleSpectatorCamRequests : BaseComponentSystem
{
    public struct SpawnRequests
    {
        public EntityArray entities;
        public ComponentDataArray<SpectatorCamSpawnRequest> requests;
    }

    [Inject] 
    public SpawnRequests SpawnGroup;   


    public HandleSpectatorCamRequests(GameWorld world, BundledResourceManager resourceManager) : base(world)
    {
        m_ResourceManager = resourceManager;
        m_Settings = Resources.Load<SpectatorCamSettings>("SpectatorCamSettings");
    }


    protected override void OnUpdate()
    {
        if (SpawnGroup.requests.Length == 0)
            return;

        // Copy requests as spawning will invalidate Group
        var spawnRequests = new SpectatorCamSpawnRequest[SpawnGroup.requests.Length];
        for (var i = 0; i < SpawnGroup.requests.Length; i++)
        {
            spawnRequests[i] = SpawnGroup.requests[i];
            PostUpdateCommands.DestroyEntity(SpawnGroup.entities[i]);
        }

        for(var i =0;i<spawnRequests.Length;i++)
        {
            var request = spawnRequests[i];
            var playerState = EntityManager.GetComponentObject<PlayerState>(request.playerEntity);
            
            
            
            var resource = m_ResourceManager.LoadSingleAssetResource(m_Settings.spectatorCamPrefab.guid);

            GameDebug.Assert(resource != null);


            var prefab = (GameObject)resource;
            GameDebug.Log("Spawning spectatorcam");
            var spectatorCam = m_world.Spawn<SpectatorCam>(prefab);
            spectatorCam.name = prefab.name;
            spectatorCam.position = request.position;
            spectatorCam.rotation = request.rotation;
            
            playerState.controlledEntity = spectatorCam.gameObject.GetComponent<GameObjectEntity>().Entity; 
        }
    }

    readonly SpectatorCamSettings m_Settings;
    readonly BundledResourceManager m_ResourceManager;
}

