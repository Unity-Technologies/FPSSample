using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "CameraNoise", menuName = "FPS Sample/Animation/AnimGraph/CameraNoise")]
public class AnimGraph_CameraNoise : AnimGraphAsset
{
	public string cameraBone;
	public string characterRootBone;
	public CameraNoiseJob.JobSettings cameraNoiseJobSettings;

	public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
	    Entity animStateOwner)
	{
		return new Instance(entityManager, owner, graph, animStateOwner, this);
	}
	
    class Instance : IAnimGraphInstance
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_CameraNoise settings)
        {
            m_Settings = settings;
            m_graph = graph;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;

            GameDebug.Assert(entityManager.HasComponent<Animator>(owner),"Owner has no Animator component");
            var animator = entityManager.GetComponentObject<Animator>(owner);
            GameDebug.Assert(entityManager.HasComponent<Skeleton>(owner),"Owner has no Skeleton component");
            var skeleton = entityManager.GetComponentObject<Skeleton>(owner);
    
            var cameraBone = skeleton.bones[skeleton.GetBoneIndex(settings.cameraBone.GetHashCode())];
            var characterRootBone = skeleton.bones[skeleton.GetBoneIndex(settings.characterRootBone.GetHashCode())];
    
            var cameraNoiseSettings = new CameraNoiseJob.EditorSettings();
            cameraNoiseSettings.cameraBone = cameraBone;
            cameraNoiseSettings.characterRootBone = characterRootBone;
            cameraNoiseSettings.jobSettings = settings.cameraNoiseJobSettings;
    
            var noiseJob = new CameraNoiseJob();
            var initialized = noiseJob.Setup(animator, cameraNoiseSettings);
            GameDebug.Assert(initialized, "Failed to initialize AimDragJob");
            m_NoisePlayable = AnimationScriptPlayable.Create(graph, noiseJob, 1);
            m_NoisePlayable.SetInputWeight(0, 1);
        }
    
        public void Shutdown()
        {
            m_NoisePlayable.GetJobData<CameraNoiseJob>().Dispose();
        }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
            m_graph.Connect(playable, playablePort, m_NoisePlayable, 0);
        }
        
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_NoisePlayable;
            playablePort = 0;
        }
    
        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("CameraNoise.Apply");

            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            var lookDir = Quaternion.Euler(new Vector3(-animState.aimPitch, animState.aimYaw, 0)) * Vector3.down;                        
            var job = m_NoisePlayable.GetJobData<CameraNoiseJob>();
            job.Update(lookDir, m_Settings.cameraNoiseJobSettings, m_NoisePlayable);
    
    //        Debug.Log(job.debugValue.x + " : " + job.debugValue.y + " : " + job.debugValue.z);
            Profiler.EndSample();
        }

        
        AnimGraph_CameraNoise m_Settings;
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;
        AnimationScriptPlayable m_NoisePlayable;
        
        PlayableGraph m_graph;
    }
}
