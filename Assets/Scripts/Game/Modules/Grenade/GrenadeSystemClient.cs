using UnityEngine;
using Unity.Entities;




[DisableAutoCreation]
public class InterpolateGrenadePresentation : BaseComponentSystem
{
    public struct Grenades
    {
        public ComponentArray<GrenadePresentation> presentations;
    }

    [Inject] 
    public Grenades Group;   
    
    public InterpolateGrenadePresentation(GameWorld world) : base(world) { }

    protected override void OnUpdate()
    {
        var time = m_world.worldTime;
        for (var i = 0; i < Group.presentations.Length; i++)
        {
            var presentation = Group.presentations[i];
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

    [Inject] 
    public Grenades Group;   
    
    public ApplyGrenadePresentation(GameWorld world) : base(world) { }

    protected override void OnUpdate()
    {
        var time = m_world.worldTime;
        for (var i = 0; i < Group.presentations.Length; i++)
        {
            var presentation = Group.presentations[i];
            var grenadeClient = Group.grenadeClients[i];
            
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

