using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Squash", menuName = "FPS Sample/Animation/AnimGraph/Squash")]
public class AnimGraph_Squash : AnimGraphAsset
{
    public AnimationClip animSquash;
    [Range(0f, 180f)]
    public float dirChangeMinAngle;
    public float dirChangeTimePenalty;
    
    [Serializable]
    public struct PlaySettings
    {
        [Range(0f,2f)]
        public float weight;
        public float playSpeed;
    }

    public PlaySettings stop;
    public PlaySettings start;
    public PlaySettings changeDir;
    public PlaySettings doubleJump;
    
    public PlaySettings landMin;
    public float landMinFallSpeed = 2;
    public PlaySettings landMax;
    public float landMaxFallSpeed = 5;

    
	public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
	    Entity animStateOwner)
	{
		return new Instance(entityManager, owner, graph, animStateOwner, this);
	}
	
    class Instance : IAnimGraphInstance, IGraphLogic
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_Squash settings)
        {
            m_Settings = settings;
            m_graph = graph;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;
            
            GameDebug.Assert(entityManager.HasComponent<CharacterPredictedData>(m_AnimStateOwner),"Owner has no CharPredictedState component");

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
            Profiler.BeginSample("Squash.Update");
            
            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            var predictedState = m_EntityManager.GetComponentData<CharacterPredictedData>(m_AnimStateOwner);
            var timeToSquash = TimeToSquash(animState);
            
            if (m_prevLocoState != animState.charLocoState)
            {
                // Double jump
                if (animState.charLocoState == CharacterPredictedData.LocoState.DoubleJump)
                {
                    animState.squashTime = 0;
                    animState.squashWeight = m_Settings.doubleJump.weight;
                    m_playSpeed = m_Settings.doubleJump.playSpeed; 
                }
                    
                // Landing
                else if (m_prevLocoState == CharacterPredictedData.LocoState.InAir)
                {
                    animState.squashTime = 0;
    
                    var vel = - predictedState.velocity.y;    
                    var t = vel < m_Settings.landMinFallSpeed ? 0 :
                        vel > m_Settings.landMaxFallSpeed ? 1 :
                        (vel - m_Settings.landMinFallSpeed) / (m_Settings.landMaxFallSpeed - m_Settings.landMinFallSpeed);
            
                    animState.squashWeight = Mathf.Lerp(m_Settings.landMin.weight, m_Settings.landMax.weight, t);
                    m_playSpeed = Mathf.Lerp(m_Settings.landMin.playSpeed, m_Settings.landMax.playSpeed, t);
                }                
                
                // Stopping
                else if (timeToSquash && animState.charLocoState == CharacterPredictedData.LocoState.Stand)
                {
                    animState.squashTime = 0;
                    animState.squashWeight = m_Settings.stop.weight;
                    m_playSpeed = m_Settings.stop.playSpeed;
                }     
                // Start Moving
                else if (timeToSquash && animState.charLocoState == CharacterPredictedData.LocoState.GroundMove)
                {
                    animState.squashTime = 0;
                    animState.squashWeight = m_Settings.start.weight;
                    m_playSpeed = m_Settings.start.playSpeed; 
                }
            }
            
            // Direction change
            else if (animState.charLocoState == CharacterPredictedData.LocoState.GroundMove && 
                Mathf.Abs(Mathf.DeltaAngle(animState.moveAngleLocal, m_prevMoveAngle)) > m_Settings.dirChangeMinAngle)
            {                
                if (timeToSquash && time.DurationSinceTick(m_lastDirChangeTick)> m_Settings.dirChangeTimePenalty)
                {
                    animState.squashTime = 0;
                    animState.squashWeight = m_Settings.changeDir.weight;
                    m_playSpeed = m_Settings.changeDir.playSpeed; 
                }

                m_lastDirChangeTick = time.tick;
            }
                        
            if (animState.squashWeight > 0)
            {
                animState.squashTime += m_playSpeed*deltaTime;
                if (animState.squashTime > m_animSquash.GetDuration())
                    animState.squashWeight = 0.0f;
            }

            m_prevLocoState = animState.charLocoState;
            m_prevMoveAngle = animState.moveAngleLocal;
            m_EntityManager.SetComponentData(m_AnimStateOwner, animState);
            
            Profiler.EndSample();
        }
        
        public void ResetNetwork(float timeOffset)
        {
        }
    
        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Squash.Apply");

            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            m_mixer.SetInputWeight(1, animState.squashWeight);
            m_animSquash.SetTime(animState.squashTime);
            
            Profiler.EndSample();
        }

        bool TimeToSquash(CharacterInterpolatedData animState)
        {               
            return Math.Abs(animState.squashTime) < 0.001f || animState.squashTime >= m_animSquash.GetDuration();
        }
        
        AnimGraph_Squash m_Settings;
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;

        AnimationClipPlayable m_animSquash;
        AnimationLayerMixerPlayable m_mixer;
        PlayableGraph m_graph;

        CharacterPredictedData.LocoState m_prevLocoState;
        
        float m_prevMoveAngle;
        float m_playSpeed;
        int m_lastDirChangeTick;

        Vector3 lastFramePos;
    }
}
