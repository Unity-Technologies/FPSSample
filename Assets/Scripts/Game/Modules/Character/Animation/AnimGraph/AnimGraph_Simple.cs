using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "Simple", menuName = "FPS Sample/Animation/AnimGraph/Simple")]
public class AnimGraph_Simple : AnimGraphAsset
{
    public AnimationClip animIdle;
    public bool idleFootIKActive = true;

    public AnimationClip animAimDownToUp;

    public ActionAnimationDefinition[] actionAnimations;

    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        return new Instance(entityManager, owner, graph, animStateOwner, this);
    }
        
    class Instance : IAnimGraphInstance, IGraphState
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_Simple settings)
        {
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;
            
            m_layerMixer = AnimationLayerMixerPlayable.Create(graph);
            int port;
    
            // Idle
            m_animIdle = AnimationClipPlayable.Create(graph, settings.animIdle);
            m_animIdle.SetApplyFootIK(settings.idleFootIKActive);
            port = m_layerMixer.AddInput(m_animIdle, 0);
            m_layerMixer.SetInputWeight(port, 1.0f);
    
            // Aim
            if(settings.animAimDownToUp != null)
                m_aimHandler = new AimVerticalHandler(m_layerMixer, settings.animAimDownToUp);
    
            // Actions
            m_actionMixer = AnimationLayerMixerPlayable.Create(graph);
            port = m_actionMixer.AddInput(m_layerMixer, 0);
            m_actionMixer.SetInputWeight(port, 1);
            m_actionAnimationHandler = new ActionAnimationHandler(m_actionMixer, settings.actionAnimations);
        }
    
        public void Shutdown()
        {
        }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
        }
    
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_actionMixer;
            playablePort = 0;
        }
    
        public void UpdatePresentationState(bool firstUpdate, GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Simple.Update");
            
            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            animState.rotation = animState.aimYaw;

            if (firstUpdate)
                animState.simpleTime = 0;
            else
                animState.simpleTime += deltaTime;
            m_EntityManager.SetComponentData(m_AnimStateOwner, animState);
            
            Profiler.EndSample();
        }

        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Simple.Apply");

            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            m_animIdle.SetTime(animState.simpleTime);
            
            var characterActionDuration = time.DurationSinceTick(animState.charActionTick);
            m_actionAnimationHandler.UpdateAction(animState.charAction, characterActionDuration);
            if(m_aimHandler != null)
                m_aimHandler.SetAngle(animState.aimPitch);
            
            Profiler.EndSample();
        }
    
        
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;
        AnimationLayerMixerPlayable m_layerMixer;
        AnimationClipPlayable m_animIdle;
        AnimationLayerMixerPlayable m_actionMixer;
    
        ActionAnimationHandler m_actionAnimationHandler;
        AimVerticalHandler m_aimHandler;
    }
}
