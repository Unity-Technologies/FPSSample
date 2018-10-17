using UnityEngine;
using UnityEngine.Experimental.Animations;


public struct FollowBoneJob : IAnimationJob
{
    TransformStreamHandle m_BoneToFollow;
    TransformSceneHandle m_SlavedTransform;
    
    public void Setup(Animator animator, Transform boneToFollow, Transform slavedTransform)
    {
        m_BoneToFollow = animator.BindStreamTransform(boneToFollow);
        m_SlavedTransform = animator.BindSceneTransform(slavedTransform);
    }
    
    public void ProcessAnimation(AnimationStream stream)
    {
        m_SlavedTransform.SetPosition(stream, m_BoneToFollow.GetPosition(stream));
        m_SlavedTransform.SetRotation(stream, m_BoneToFollow.GetRotation(stream));
    }

    public void ProcessRootMotion(AnimationStream stream)
    {
    }
}

