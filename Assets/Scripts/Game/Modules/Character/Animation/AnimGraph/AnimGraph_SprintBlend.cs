using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "SprintBlend", menuName = "FPS Sample/Animation/AnimGraph/SprintBlend")]
public class AnimGraph_SprintBlend : AnimGraphAsset
{
    public AnimGraphAsset runTemplate;
    public AnimGraphAsset sprintTemplate;
    [Range(0f, 1f)]
    public float sprintTransitionSpeed;
    // TODO: (sunek) Check if this is needed or we can do without first update when changing between run and sprint
    [Tooltip("Always reset child controllers on sprint state change")]
    public bool resetControllerOnChange;

    struct AnimationControllerEntry
    {
        public IAnimGraphInstance controller;
        public IGraphState animStateUpdater;
        public int port;
    }
    
    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        var animState = new Instance(entityManager, owner, graph, animStateOwner, this);
        return animState;
    }

    class Instance : IAnimGraphInstance, IGraphState
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_SprintBlend settings)
        {
            // TODO: Remove the members that are not needed
            m_Settings = settings;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;
       
            m_RunController = new AnimationControllerEntry();
            m_RunController.controller = settings.runTemplate.Instatiate(entityManager, owner, graph, animStateOwner);
            m_RunController.animStateUpdater = m_RunController.controller as IGraphState;
            m_RunController.port = 0;
            
            m_SprintController = new AnimationControllerEntry();
            m_SprintController.controller = settings.sprintTemplate.Instatiate(entityManager, owner, graph, animStateOwner);
            m_SprintController.animStateUpdater = m_SprintController.controller as IGraphState;
            m_SprintController.port = 1;
            
            m_RootMixer = AnimationMixerPlayable.Create(graph, 2);
            
            // TODO: Put into function?
            var outputPlayable = Playable.Null;
            var outputPort = 0;
            
            m_RunController.controller.GetPlayableOutput(m_RunController.port, ref outputPlayable, ref outputPort);
            graph.Connect(outputPlayable, outputPort, m_RootMixer, 0);
            
            m_SprintController.controller.GetPlayableOutput(m_SprintController.port, ref outputPlayable, ref outputPort);
            graph.Connect(outputPlayable, outputPort, m_RootMixer, 1);
            
            m_RootMixer.SetInputWeight(0, 1f);
        }
        
        public void Shutdown() {}

        public void SetPlayableInput(int portId, Playable playable, int playablePort) { }

        public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
        {
            playable = m_RootMixer;
            playablePort = 0;
        }

        public void UpdatePresentationState(bool firstUpdate, GameTime time, float deltaTime)
        {
            Profiler.BeginSample("SprintBlend.Update");
            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            if (firstUpdate && animState.previousCharLocoState != CharacterPredictedData.LocoState.Jump && 
                animState.previousCharLocoState != CharacterPredictedData.LocoState.DoubleJump && 
                animState.previousCharLocoState != CharacterPredictedData.LocoState.InAir)
            {
                animState.sprintWeight = animState.sprinting;
            }
            
            var transitionSpeed = m_Settings.sprintTransitionSpeed * 60 * deltaTime;
            if (animState.sprinting == 1)
            {    
                animState.sprintWeight = math.clamp(animState.sprintWeight + transitionSpeed, 0f, 1f);
            }
            else
            {
                animState.sprintWeight = math.clamp(animState.sprintWeight - transitionSpeed, 0f, 1f); 
            }
            
            m_EntityManager.SetComponentData(m_AnimStateOwner,animState);
                        
            if (animState.sprinting == 0)
            {
                var resetController = m_WasSprinting && m_Settings.resetControllerOnChange || firstUpdate;
                m_RunController.animStateUpdater.UpdatePresentationState(resetController, time, deltaTime);                 
            }
            else
            {
                var resetController = !m_WasSprinting && m_Settings.resetControllerOnChange || firstUpdate;
                m_SprintController.animStateUpdater.UpdatePresentationState(resetController, time, deltaTime);
            }

            m_WasSprinting = animState.sprinting == 1;
                        
            Profiler.EndSample();
        }
        
        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("SprintBlend.Apply");
            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);

            var smoothedWeight = math.smoothstep(0f, 1f, animState.sprintWeight);
            m_RootMixer.SetInputWeight(0, 1f - smoothedWeight);
            m_RootMixer.SetInputWeight(1, smoothedWeight);

            if (animState.sprintWeight < 1.0f)
            {
                m_RunController.controller.ApplyPresentationState(time, deltaTime);
            }
            
            if (animState.sprintWeight > 0.0f)
            {
                m_SprintController.controller.ApplyPresentationState(time, deltaTime);
            }
            
            Profiler.EndSample();
        }

        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;
        AnimGraph_SprintBlend m_Settings;
        
        AnimationMixerPlayable m_RootMixer;        
        AnimationControllerEntry m_RunController;
        AnimationControllerEntry m_SprintController;
        
        bool m_WasSprinting;
    }
}
