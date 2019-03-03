using UnityEngine;
using Unity.Entities;



[DisableAutoCreation]
public class ApplyGrenadePresentation : BaseComponentSystem
{
    ComponentGroup Group;   
    
    public ApplyGrenadePresentation(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(GrenadeClient),typeof(PresentationEntity),ComponentType.Subtractive<DespawningEntity>());
    }

    protected override void OnUpdate()
    {
        var grenadeClientArray = Group.GetComponentArray<GrenadeClient>();
        var presentationArray = Group.GetComponentArray<PresentationEntity>();
        
        for (var i = 0; i < grenadeClientArray.Length; i++)
        {
            var grenadeClient = grenadeClientArray[i];
            var presentation = presentationArray[i];
            if (!EntityManager.Exists(presentation.ownerEntity))
            {
                GameDebug.LogError("ApplyGrenadePresentation. Entity does not exist;" + presentation.ownerEntity);
                continue;
            }
            
            var interpolatedState = EntityManager.GetComponentData<Grenade.InterpolatedState>(presentation.ownerEntity);
            
            grenadeClient.transform.position = interpolatedState.position;
            
            if(interpolatedState.bouncetick > grenadeClient.bounceTick)
            {
                grenadeClient.bounceTick = interpolatedState.bouncetick;
                Game.SoundSystem.Play(grenadeClient.bounceSound, interpolatedState.position);
            }
            
            if (interpolatedState.exploded == 1 && !grenadeClient.exploded)
            {
                grenadeClient.exploded = true;
                
                grenadeClient.geometry.SetActive(false);
                
                if (grenadeClient.explodeEffect != null)
                {
                    World.GetExistingManager<HandleSpatialEffectRequests>().Request(grenadeClient.explodeEffect, 
                        interpolatedState.position,Quaternion.identity);
                }
            }
        }
    }
}

