using UnityEngine;
using Unity.Entities;




[DisableAutoCreation]
public class InterpolateGrenadePresentation : BaseComponentSystem
{
    ComponentGroup Group;   
    
    public InterpolateGrenadePresentation(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(GrenadePresentation));
    }

    protected override void OnUpdate()
    {
        var grenadeArray = Group.GetComponentArray<GrenadePresentation>();
        var time = m_world.worldTime;
        for (var i = 0; i < grenadeArray.Length; i++)
        {
            var presentation = grenadeArray[i];
            int lowIndex = 0, highIndex = 0;
            float interpVal = 0;
            var interpValid = presentation.stateHistory.GetStates(time.tick, time.TickDurationAsFraction, ref lowIndex, ref highIndex, ref interpVal);
            if (interpValid)
            {
                var prevState = presentation.stateHistory[lowIndex];
                var nextState = presentation.stateHistory[highIndex];
                presentation.state.Interpolate(ref prevState, ref nextState, interpVal);
            }
        }
    }
}


[DisableAutoCreation]
public class ApplyGrenadePresentation : BaseComponentSystem
{
    public struct Grenades
    {
        public ComponentArray<GrenadePresentation> presentations;
        public ComponentArray<GrenadeClient> grenadeClients;
        
    }

    ComponentGroup Group;   
    
    public ApplyGrenadePresentation(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(GrenadePresentation), typeof(GrenadeClient));
    }

    protected override void OnUpdate()
    {
        var grenadePresentArray = Group.GetComponentArray<GrenadePresentation>();
        var grenadeClientArray = Group.GetComponentArray<GrenadeClient>();
        var time = m_world.worldTime;
        for (var i = 0; i < grenadePresentArray.Length; i++)
        {
            var presentation = grenadePresentArray[i];
            var grenadeClient = grenadeClientArray[i];
            
            presentation.transform.position = presentation.state.position;
            
            if(presentation.state.bouncetick > grenadeClient.bounceTick)
            {
                grenadeClient.bounceTick = presentation.state.bouncetick;
                Game.SoundSystem.Play(grenadeClient.bounceSound, presentation.state.position);
            }
            
            if (presentation.state.exploded && !grenadeClient.exploded)
            {
                grenadeClient.exploded = true;
                
                grenadeClient.geometry.SetActive(false);
                
                if (grenadeClient.explodeEffect != null)
                {
                    SpatialEffectRequest.Create(PostUpdateCommands, grenadeClient.explodeEffect, presentation.state.position,
                        Quaternion.identity);
                }
            }
        }
    }
}

