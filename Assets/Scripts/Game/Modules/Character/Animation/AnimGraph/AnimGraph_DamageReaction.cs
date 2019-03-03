using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "DamageReaction", menuName = "FPS Sample/Animation/AnimGraph/DamageReaction")]
public class AnimGraph_DamageReaction : AnimGraphAsset
{
	[Tooltip("Reaction animations starting from S (damage comming from front) going clockwise")]
	public AnimationClip[] clips;

	public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
	    Entity animStateOwner)
	{
		return new Instance(entityManager, owner, graph, animStateOwner, this);
	}
	
    public class Instance : IAnimGraphInstance
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_DamageReaction settings)
        {
            m_settings = settings;
            m_graph = graph;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;
    
            m_rootMixer = AnimationLayerMixerPlayable.Create(graph,2);
            m_rootMixer.SetInputWeight(0, 1f);
    
            // Setup blend mixer
            m_blendMixer = AnimationMixerPlayable.Create(graph,4);
            GameDebug.Assert(m_settings.clips != null &&m_settings.clips.Length > 0,"No animation clips added to damagereaction settings:{0}",settings.name);
            for (var i = 0; i < m_settings.clips.Length; i++)
            {
                var clipPlayable = AnimationClipPlayable.Create(graph, m_settings.clips[i]);
                clipPlayable.SetApplyFootIK(false);
                clipPlayable.Pause();
                graph.Connect(clipPlayable, 0, m_blendMixer, i);

                if (m_settings.clips[i].length > m_reactionAnimDuration)
                {
                    m_reactionAnimDuration = m_settings.clips[i].length;
                }
            }
            m_reactionAnimAngleSpan = 360 / m_blendMixer.GetInputCount();
            graph.Connect(m_blendMixer, 0, m_rootMixer, m_blendMixerPort);
            m_rootMixer.SetLayerAdditive(m_blendMixerPort,true);
        }
    
        public void Shutdown()
        {
        }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
            m_graph.Connect(playable, playablePort, m_rootMixer, m_inputPort);
        }
    
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_rootMixer;
            playablePort = 0;
        }
    
        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("DamageReaction.Apply");

            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            if (animState.damageTick > m_lastReactionTick)
            {
                // Handle first update
                if (m_lastReactionTick == -1)
                {
                    m_lastReactionTick = animState.damageTick;
                    return;
                }

                m_reactionAnimPlaying = true;
                m_lastReactionTick = animState.damageTick;
                m_rootMixer.SetInputWeight(m_blendMixerPort, 1.0f);

                var animCount = m_blendMixer.GetInputCount();
                var angle = MathHelper.SignedAngle(animState.aimYaw, animState.damageDirection);

                var index = Mathf.FloorToInt(angle + 180 + m_reactionAnimAngleSpan * 0.5f) / m_reactionAnimAngleSpan;
                if (index >= animCount)
                    index = 0;

                //            GameDebug.Log("yaw: " + presentationState.aimYaw + "dam:" + presentationState.damageDirection + " angle:" + angle + " index:" + index);

                m_blendMixer.GetInput(index).SetTime(0);
                m_blendMixer.GetInput(index).Play();

                for (var i = 0; i < animCount; i++)
                {
                    m_blendMixer.SetInputWeight(i,i==index ? 1 : 0);
                }     
            }

            else if (m_reactionAnimPlaying)
            {
                var timeSinceLastDamage = (time.tick - m_lastReactionTick) / (float)time.tickRate;
                if (timeSinceLastDamage > m_reactionAnimDuration)
                {
                    m_reactionAnimPlaying = false;
                    m_rootMixer.SetInputWeight(m_blendMixerPort, 0.0f); 
                }

            }
            
            Profiler.EndSample();
        }
    
        AnimGraph_DamageReaction m_settings;
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;
        
        PlayableGraph m_graph;
        AnimationLayerMixerPlayable m_rootMixer;
        AnimationMixerPlayable m_blendMixer;

        float m_reactionAnimDuration;
        bool m_reactionAnimPlaying;
        
        int m_lastReactionTick = -1;
        int m_reactionAnimAngleSpan;
        const int m_inputPort = 0;
        const int m_blendMixerPort = 1;
    
        
    }
}
