using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "InAir", menuName = "FPS Sample/Animation/AnimGraph/InAir")]
public class AnimGraph_InAir : AnimGraphAsset
{
    public AnimationClip animInAir;
    public AnimationClip animLandAntic;
    public float landAnticStartHeight = 0.3f;
    public float blendDuration = 0.1f;

    public AnimationClip animAimDownToUp;

    public ActionAnimationDefinition[] actionAnimations;

    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        return new Instance(entityManager, owner, graph, animStateOwner, this);
    }
    
    class Instance : IAnimGraphInstance, IGraphState
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_InAir settings)
        {
            m_settings = settings;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;
            
            GameDebug.Assert(entityManager.HasComponent<Character>(m_AnimStateOwner),"Owner has no Character component");
            m_character = entityManager.GetComponentObject<Character>(m_AnimStateOwner);

            m_mainMixer = AnimationMixerPlayable.Create(graph);
    
            m_animInAir = AnimationClipPlayable.Create(graph, settings.animInAir);
            m_animInAir.Play();
            m_animInAir.SetApplyFootIK(false);
            inAirPort = m_mainMixer.AddInput(m_animInAir, 0);
    
            m_animLandAntic = AnimationClipPlayable.Create(graph, settings.animLandAntic);
            m_animInAir.Play();
            m_animLandAntic.SetApplyFootIK(false);
            landAnticPort = m_mainMixer.AddInput(m_animLandAntic, 0);
    
            m_layerMixer = AnimationLayerMixerPlayable.Create(graph);
            var port = m_layerMixer.AddInput(m_mainMixer, 0);
            m_layerMixer.SetInputWeight(port, 1);
    
            // Aim
            if (settings.animAimDownToUp != null)
                m_aimHandler = new AimVerticalHandler(m_layerMixer, settings.animAimDownToUp);
    
            // Actions
            m_actionAnimationHandler = new ActionAnimationHandler(m_layerMixer, settings.actionAnimations);
        }
    
        public void Shutdown()
        {
        }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
        }
    
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_layerMixer;
            playablePort = 0;
        }
    
        public void UpdatePresentationState(bool firstUpdate, GameTime time, float deltaTime)
        {
            Profiler.BeginSample("InAir.Update");
            
            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            if (firstUpdate)
            {
                animState.inAirTime = 0;
            }
            else
            {
                animState.inAirTime += deltaTime;
            }
            
            animState.rotation = animState.aimYaw;
    
            // Blend in land anticipation when close to ground // TODO only do this test when moving downwards
            var nearGround = m_character.altitude < m_settings.landAnticStartHeight;
            var deltaWeight = deltaTime / m_settings.blendDuration;
            animState.landAnticWeight += nearGround ? deltaWeight : -deltaWeight;
            animState.landAnticWeight = Mathf.Clamp(animState.landAnticWeight, 0, 1);
            m_EntityManager.SetComponentData(m_AnimStateOwner, animState);
            
            Profiler.EndSample();
        }

        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("InAir.Apply");
            
            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            m_animInAir.SetTime(animState.inAirTime);
            m_animLandAntic.SetTime(animState.inAirTime);
            
            
            var landAnticWeight = animState.landAnticWeight;
    
            m_mainMixer.SetInputWeight(inAirPort, 1.0f - landAnticWeight);
            m_mainMixer.SetInputWeight(landAnticPort, landAnticWeight);
    
            var characterActionDuration = time.DurationSinceTick(animState.charActionTick);
            m_actionAnimationHandler.UpdateAction(animState.charAction, characterActionDuration);
            if(m_aimHandler != null)
                m_aimHandler.SetAngle(animState.aimPitch);
            
            Profiler.EndSample();
        }
    
        AnimGraph_InAir m_settings;
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;

        Character m_character;
        
    
        
        AnimationMixerPlayable m_mainMixer;
        
        AnimationClipPlayable m_animInAir;
        AnimationClipPlayable m_animLandAntic;
    
        int inAirPort;
        int landAnticPort;
    
        AnimationLayerMixerPlayable m_layerMixer;
    
        ActionAnimationHandler m_actionAnimationHandler;
        AimVerticalHandler m_aimHandler;
    }    
}
