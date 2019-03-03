using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "Banking", menuName = "FPS Sample/Animation/AnimGraph/Banking")]
public class AnimGraph_Banking : AnimGraphAsset
{
    public string bankTransform;
    public BankingJob.Settings bankingSettings;

    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        return new Instance(entityManager, owner, graph, animStateOwner, this);
    }
    
    class Instance : IAnimGraphInstance, IGraphLogic
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_Banking settings)
        {
            m_Settings = settings;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;
            
            GameDebug.Assert(entityManager.HasComponent<Animator>(owner), "Owner has no Animator component");
            var animator = entityManager.GetComponentObject<Animator>(owner);
            GameDebug.Assert(entityManager.HasComponent<Skeleton>(owner), "Owner has no Skeleton component");
            var skeleton = entityManager.GetComponentObject<Skeleton>(owner);
            GameDebug.Assert(entityManager.HasComponent<CharacterPredictedData>(m_AnimStateOwner),"Owner has no CharPredictedState component");
            
            var bankTransform = skeleton.bones[skeleton.GetBoneIndex(settings.bankTransform.GetHashCode())];
    
            var bankingSettings = new BankingJob.EditorSettings();
            bankingSettings.bankTransform = bankTransform;
            bankingSettings.settings = settings.bankingSettings;
    
            var bankingJob = new BankingJob();
            m_HeadLeftRightMuscles = new NativeArray<MuscleHandle>(2, Allocator.Persistent);
            m_SpineLeftRightMuscles = new NativeArray<MuscleHandle>(3, Allocator.Persistent);
            var initialized = bankingJob.Setup(animator, bankingSettings, 2312, m_HeadLeftRightMuscles, m_SpineLeftRightMuscles);
            GameDebug.Assert(initialized, "Failed to initialize BankingJob");
            m_Playable = AnimationScriptPlayable.Create(graph, bankingJob);
        }
    
        public void Shutdown()
        {
            var job = m_Playable.GetJobData<BankingJob>();
            job.Dispose();
            m_HeadLeftRightMuscles.Dispose();
            m_SpineLeftRightMuscles.Dispose();
        }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
            m_Playable.AddInput(playable, playablePort, 1f);
        }
    
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_Playable;
            playablePort = 0;
        }

        public void UpdateGraphLogic(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Banking.Update");

            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            var predictedState = m_EntityManager.GetComponentData<CharacterPredictedData>(m_AnimStateOwner);

            if (animState.charLocoState != CharacterPredictedData.LocoState.GroundMove)
            {   
                var groundMoveVec = Vector3.ProjectOnPlane(predictedState.velocity, Vector3.up);
                if (animState.charLocoState == CharacterPredictedData.LocoState.InAir || Vector3.Magnitude(groundMoveVec) < 0.1f)
                {
                    m_PreviousPosition = animState.position;
                }
            }
                
            animState.banking = Mathf.MoveTowards(animState.banking, 0f, m_Settings.bankingSettings.bankDamp * deltaTime);
    
            if (animState.charLocoState == CharacterPredictedData.LocoState.GroundMove)
            {
                var movement = new Vector3(animState.position.x, 0f, animState.position.z) - new Vector3(m_PreviousPosition.x, 0f, m_PreviousPosition.z);
                var delta = -Vector3.SignedAngle(m_PreviousMovement, movement, Vector3.up) * m_Settings.bankingSettings.bankContribution * deltaTime;                
                
                // - Multiply the delta by the movement direction: Forward = 1, Strafe = 0, Backwards = -1
                delta *= (Mathf.Abs(Mathf.DeltaAngle(animState.rotation, animState.moveYaw)) - 90f) / -90f;
        
                // - Define max contribution 
                delta = Mathf.Clamp(delta, -m_Settings.bankingSettings.maxBankContribution * deltaTime, m_Settings.bankingSettings.maxBankContribution * deltaTime);
        
                // TODO: (sunek) Make it be multiplied by velocity
                animState.banking = Mathf.Clamp(animState.banking + delta, -m_Settings.bankingSettings.bankMagnitude, m_Settings.bankingSettings.bankMagnitude);
        
                m_PreviousPosition = animState.position;
                m_PreviousMovement = movement;            
            }
            
            m_EntityManager.SetComponentData(m_AnimStateOwner, animState);

            Profiler.EndSample();
        }
    
        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Banking.Apply");

            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            var job = m_Playable.GetJobData<BankingJob>();
            job.Update(animState, m_Settings.bankingSettings, m_Playable);
            
            Profiler.EndSample();
        }
    
        AnimGraph_Banking m_Settings;
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;

        AnimationScriptPlayable m_Playable;
        Vector3 m_PreviousPosition;
        Vector3 m_PreviousMovement;
        
        NativeArray<MuscleHandle> m_HeadLeftRightMuscles;
        NativeArray<MuscleHandle> m_SpineLeftRightMuscles;
    }

}
