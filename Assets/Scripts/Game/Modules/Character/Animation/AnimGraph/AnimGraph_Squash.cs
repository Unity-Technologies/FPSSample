using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "Squash", menuName = "FPS Sample/Animation/AnimGraph/Squash")]
public class AnimGraph_Squash : AnimGraphAsset
{
    public AnimationClip animSquash;
    [Range(0f, 1f)]
    public float minTimeBetweenSquashing;

    [Serializable]
    public struct PlaySettings
    {
        [Range(0f,2f)]
        public float weight;
        public float playSpeed;
    }

    public PlaySettings stop;
    public PlaySettings start;
    public PlaySettings doubleJump;
    
    public PlaySettings landMin;
    public float landMinFallSpeed = 2;
    public PlaySettings landMax;
    public float landMaxFallSpeed = 5;
    
	public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph)
	{
		return new Instance(entityManager, owner, graph, this);
	}
	
    class Instance : IAnimGraphInstance, IGraphLogic
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, AnimGraph_Squash settings)
        {
            m_Settings = settings;
            m_graph = graph;
            m_EntityManager = entityManager;
            m_Owner = owner;
            
            GameDebug.Assert(entityManager.HasComponent<CharacterPredictedState>(owner),"Owner has no Character component");
            m_character = entityManager.GetComponentObject<CharacterPredictedState>(owner);

            m_mixer = AnimationLayerMixerPlayable.Create(graph,2);
            m_mixer.SetInputWeight(0, 1.0f);
            
            m_animSquash = AnimationClipPlayable.Create(graph, settings.animSquash);
            m_animSquash.SetApplyFootIK(false);
            m_animSquash.SetDuration(settings.animSquash.length);
            m_animSquash.Pause();
            m_graph.Connect(m_animSquash, 0, m_mixer, 1);
            m_mixer.SetInputWeight(1, 0.0f);
            m_mixer.SetLayerAdditive(1, true);
        }
    
        public void Shutdown()
        {
            
        }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
            m_graph.Connect(playable, playablePort, m_mixer, 0);
        }
        
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_mixer;
            playablePort = 0;
        }

        
        public void UpdateGraphLogic(GameTime time, float deltaTime)
        {            
            var animState = m_EntityManager.GetComponentData<CharAnimState>(m_Owner);

            if (TimeToSquash(time))
            {
                if (m_prevLocoState != animState.charLocoState)
                {
                    
                    // Double jump
                    if (animState.charLocoState == CharacterPredictedState.StateData.LocoState.DoubleJump)
                    {
                        m_lastSquashTick = time.tick;
                        animState.squashTime = 0;
                        animState.squashWeight = m_Settings.doubleJump.weight;
                        playSpeed = m_Settings.doubleJump.playSpeed; 
                    }
                    
                    // Landing
                    else if (m_prevLocoState == CharacterPredictedState.StateData.LocoState.InAir)
                    {
                        m_lastSquashTick = time.tick;
                        animState.squashTime = 0;
    
                        var vel = - m_character.State.velocity.y;    
                        var t = vel < m_Settings.landMinFallSpeed ? 0 :
                            vel > m_Settings.landMaxFallSpeed ? 1 :
                            (vel - m_Settings.landMinFallSpeed) / (m_Settings.landMaxFallSpeed - m_Settings.landMinFallSpeed);
            
                        animState.squashWeight = Mathf.Lerp(m_Settings.landMin.weight, m_Settings.landMax.weight, t);
                        playSpeed = Mathf.Lerp(m_Settings.landMin.playSpeed, m_Settings.landMax.playSpeed, t);
                    }
                    // Stopping
                    else if (animState.charLocoState == CharacterPredictedState.StateData.LocoState.Stand)
                    {
                        m_lastSquashTick = time.tick;
                        animState.squashTime = 0;
                        animState.squashWeight = m_Settings.stop.weight;
                        playSpeed = m_Settings.stop.playSpeed;
                    }     
                    // Start Moving
                    else if (animState.charLocoState == CharacterPredictedState.StateData.LocoState.GroundMove)
                    {
                        m_lastSquashTick = time.tick;
                        animState.squashTime = 0;
                        animState.squashWeight = m_Settings.start.weight;
                        playSpeed = m_Settings.start.playSpeed; 
                    } 
                }
                
                // Change Direction
                else if (animState.charLocoState == CharacterPredictedState.StateData.LocoState.GroundMove &&
                    Vector2.Angle(animState.locomotionVector, m_prevLocoVector) > 90f)
                {
                    m_lastSquashTick = time.tick;
                    animState.squashTime = 0;
                    animState.squashWeight = m_Settings.stop.weight;
                    playSpeed = m_Settings.stop.playSpeed; 
                }   
            }
            
            if (animState.squashWeight > 0)
            {
                animState.squashTime += playSpeed*deltaTime;
                if (animState.squashTime > m_animSquash.GetDuration())
                    animState.squashWeight = 0.0f;
            }

            m_prevLocoState = animState.charLocoState;
            m_prevLocoVector = animState.locomotionVector;
            m_EntityManager.SetComponentData(m_Owner, animState);
        }
        
        public void ResetNetwork(float timeOffset)
        {
        }
    
        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            var animState = m_EntityManager.GetComponentData<CharAnimState>(m_Owner);
            m_mixer.SetInputWeight(1, animState.squashWeight);
            m_animSquash.SetTime(animState.squashTime);
        }

        bool TimeToSquash(GameTime time)
        {
            return (time.tick - m_lastSquashTick) / time.tickRate > m_Settings.minTimeBetweenSquashing;
        }
        
        AnimGraph_Squash m_Settings;
        EntityManager m_EntityManager;
        Entity m_Owner;

        CharacterPredictedState m_character;

        AnimationClipPlayable m_animSquash;
        AnimationLayerMixerPlayable m_mixer;
        PlayableGraph m_graph;

        CharacterPredictedState.StateData.LocoState m_prevLocoState;
        Vector2 m_prevLocoVector;
        float playSpeed;

        float m_lastSquashTick;
    }
}
