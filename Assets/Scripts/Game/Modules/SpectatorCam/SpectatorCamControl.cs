using Unity.Entities;
using UnityEngine;

public class SpectatorCamControl : MonoBehaviour 
{
}

[DisableAutoCreation]
public class UpdateSpectatorCamControl : BaseComponentSystem
{
    struct GroupType
    {
        public ComponentArray<LocalPlayer> localPlayers;
        public ComponentArray<PlayerCameraSettings> cameraSettings;
        public ComponentArray<SpectatorCamControl> spectatorCamCtrls;
    }

    [Inject]
    GroupType Group;
    
    public UpdateSpectatorCamControl(GameWorld world) : base(world)
    {
    }

    protected override void OnUpdate()
    {
        for (var i = 0; i < Group.localPlayers.Length; i++)
        {
            var controlledEntity = Group.localPlayers[i].controlledEntity;
            
            if (controlledEntity == Entity.Null || !EntityManager.HasComponent<SpectatorCam>(controlledEntity))
                continue;

            var spectatorCam = EntityManager.GetComponentObject<SpectatorCam>(controlledEntity);
            var cameraSettings = Group.cameraSettings[i];
            cameraSettings.isEnabled = true;
            cameraSettings.position = spectatorCam.position;
            cameraSettings.rotation = spectatorCam.rotation;
        }            
    }
}