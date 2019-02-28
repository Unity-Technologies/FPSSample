using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "AimDrag", menuName = "FPS Sample/Animation/AnimGraph/AimDrag")]
public class AnimGraph_AimDrag : AnimGraphAsset
{
	public string weaponHandBone;
	public string weaponBone;
	public string applyResultOnBone;

	public AimDragJob.Settings aimDragSettings;
//    public NativeQueue<Quaternion> m_DragHistory;


	public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
	    Entity animStateOwner)
	{
		return new Instance(entityManager, owner, graph, animStateOwner, this);
	}
	
    class Instance : IAnimGraphInstance
    {
        public NativeQueue<Quaternion> m_DragHistory;

        
        
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_AimDrag settings)
        {
            m_settings = settings;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;
            m_graph = graph;
    
            GameDebug.Assert(entityManager.HasComponent<Animator>(owner),"Owner has no Animator component");
            var animator = entityManager.GetComponentObject<Animator>(owner);
            GameDebug.Assert(entityManager.HasComponent<Skeleton>(owner),"Owner has no Skeleton component");
            var skeleton = entityManager.GetComponentObject<Skeleton>(owner);
    
    
            var weaponHandBone = skeleton.bones[skeleton.GetBoneIndex(settings.weaponHandBone.GetHashCode())];
            var weaponBone = skeleton.bones[skeleton.GetBoneIndex(settings.weaponBone.GetHashCode())];
            var resultBone = animator.transform.Find(settings.applyResultOnBone);
    
            // Weapon sway
            var aimDragJobSettings = new AimDragJob.EditorSettings();
            aimDragJobSettings.weaponHandBone = weaponHandBone;
            aimDragJobSettings.weaponBone = weaponBone;
            aimDragJobSettings.applyResultOn = resultBone;
            aimDragJobSettings.settings = settings.aimDragSettings;
    
            var dragJob = new AimDragJob();
            m_DragHistory = new NativeQueue<Quaternion>(Allocator.Persistent);
            var initialized = dragJob.Setup(animator, aimDragJobSettings, 2312, m_DragHistory);
            GameDebug.Assert(initialized, "Failed to initialize AimDragJob");
            m_AimDragPlayable = AnimationScriptPlayable.Create(graph, dragJob, 1); 
            m_AimDragPlayable.SetInputWeight(0,1);
    
            // Hand IK
            var ikSettings = new TwoBoneIKJob.IkChain();
            ikSettings.target.target = resultBone;
            ikSettings.target.readFrom = TwoBoneIKJob.TargetType.Scene;
            ikSettings.driven.type = TwoBoneIKJob.IkType.Generic;
            ikSettings.driven.genericEndJoint = weaponHandBone;
            ikSettings.driven.humanoidLimb = AvatarIKGoal.LeftFoot;
    
            var rightArmIkJob = new TwoBoneIKJob();
            initialized = rightArmIkJob.Setup(animator, ikSettings, typeof(AnimStateController), "rightArmIK.weight.value",
                "rightArmIK.weight.propertyOffset", "rightArmIK.target.offset");
            GameDebug.Assert(initialized, "Failed to initialize TwoBoneIKJob");
            
            m_IKPlayable = AnimationScriptPlayable.Create(graph, rightArmIkJob);
            m_IKPlayable.AddInput(m_AimDragPlayable, 0, 1f);
        }
    
        public void Shutdown()
        {
            m_DragHistory.Dispose();
        }
    
        public void SetPlayableInput(int portId, Playable playable, int playablePort)
        {
            m_graph.Connect(playable, playablePort, m_AimDragPlayable, 0);
        }
    
        public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
        {
            playable = m_IKPlayable;
            playablePort = 0;
        }
    
        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("AimDrag.Apply");

            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            var lookDir = Quaternion.Euler(new Vector3(-animState.aimPitch, animState.aimYaw, 0)) * Vector3.down;
            var job = m_AimDragPlayable.GetJobData<AimDragJob>();            
            job.Update(lookDir, m_settings.aimDragSettings, animState, m_AimDragPlayable);

            Profiler.EndSample();
        }
    
        AnimGraph_AimDrag m_settings;
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;
        PlayableGraph m_graph;
        AnimationScriptPlayable m_AimDragPlayable;
        AnimationScriptPlayable m_IKPlayable;
    }
}
