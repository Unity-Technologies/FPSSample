
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "TwoBoneIk", menuName = "FPS Sample/Animation/AnimGraph/TwoBoneIk")]
public class AnimGraph_TwoBoneIk : AnimGraphAsset
{
    public string targetBone;
    public string drivenBone;

    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        var animator = entityManager.GetComponentObject<Animator>(owner);
        var skeleton = entityManager.GetComponentObject<Skeleton>(owner);
        return new TwoBoneIkAnimNode(animator, skeleton, graph, this);
    }
    
    public class TwoBoneIkAnimNode : IAnimGraphInstance 
    {
        public TwoBoneIkAnimNode(Animator animator, Skeleton skeleton, PlayableGraph graph, AnimGraph_TwoBoneIk settings)
        {
            m_graph = graph;
    
            var targetBone = skeleton.bones[skeleton.GetBoneIndex(settings.targetBone.GetHashCode())];
            var drivenBone = skeleton.bones[skeleton.GetBoneIndex(settings.drivenBone.GetHashCode())];
    
    
            // Setup settings
            var ikSettings = new TwoBoneIKJob.IkChain();
            ikSettings.target.target = targetBone;
            ikSettings.target.readFrom = TwoBoneIKJob.TargetType.Stream;
            ikSettings.driven.type = TwoBoneIKJob.IkType.Generic;
            ikSettings.driven.genericEndJoint = drivenBone;
//            ikSettings.driven.humanoidLimb = AvatarIKGoal.LeftFoot;
            
            // Create job
            var leftArmIkJob = new TwoBoneIKJob();
            var initialized = leftArmIkJob.Setup(animator, ikSettings, typeof(AnimStateController),
                "leftArmIK.weight.value", "leftArmIK.weight.propertyOffset", "leftArmIK.target.offset");
            GameDebug.Assert(initialized,"Failed to initialize TwoBoneIKJob");
            m_ikPlayable = AnimationScriptPlayable.Create(graph, leftArmIkJob, 1);
            m_ikPlayable.SetInputWeight(0,1);
        }
    
        public void Shutdown()
        {
        }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
            m_graph.Connect(playable, playablePort, m_ikPlayable, 0);
        }
    
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_ikPlayable;
            playablePort = 0;
        }
    
        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
        }
    
        PlayableGraph m_graph;
        AnimationScriptPlayable m_ikPlayable;
    }

}
