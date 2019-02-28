using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Animations;

public struct BankingJob : IAnimationJob
{
    [Serializable]
    public struct Settings
    {
        public Vector3 position;
        public Vector3 rotation;

        public float bankContribution;
        public float maxBankContribution;
        public float bankDamp;
        public float bankMagnitude;
        [Range(0f, 1f)]
        public float footMultiplier;
        [Range(-1f, 1f)]
        public float headMultiplier;
        [Range(-1f, 1f)]
        public float spineMultiplier;
    }

    [Serializable]
    public struct EditorSettings
    {
        // The non job reference types
        public Transform bankTransform;

        // All the value types
        public Settings settings;

        public bool HasValidData()
        {
            return bankTransform != null;
        }
    }

    TransformStreamHandle m_SkeletonHandle;    
    NativeArray<MuscleHandle> m_HeadLeftRightMuscles;
    NativeArray<MuscleHandle> m_SpineLeftRightMuscles;

    public Settings settings;
    public float bankAmount;
    public CharacterInterpolatedData animState;

    public bool Setup(Animator animator, EditorSettings editorSettings, float deltaTime, 
        NativeArray<MuscleHandle> headMuscles, NativeArray<MuscleHandle> spineMuscles)
    {
        if (!editorSettings.HasValidData())
        {
            return false;
        }
        
        m_SkeletonHandle = animator.BindStreamTransform(editorSettings.bankTransform);

        m_HeadLeftRightMuscles = headMuscles;
        m_SpineLeftRightMuscles = spineMuscles;
        
        m_HeadLeftRightMuscles[0] = new MuscleHandle(HeadDof.NeckLeftRight);
        m_HeadLeftRightMuscles[1] = new MuscleHandle(HeadDof.HeadLeftRight);
        
        m_SpineLeftRightMuscles[0] = new MuscleHandle(BodyDof.SpineLeftRight);
        m_SpineLeftRightMuscles[1] = new MuscleHandle(BodyDof.ChestLeftRight);
        m_SpineLeftRightMuscles[2] = new MuscleHandle(BodyDof.UpperChestLeftRight);


        return true;
    }

    public void Dispose()
    {
    }

    public void Update(CharacterInterpolatedData animState, Settings settings, AnimationScriptPlayable playable)
    {
        var job = playable.GetJobData<BankingJob>();
        job.animState = animState;
        job.bankAmount = animState.banking;
        job.settings = settings;
        playable.SetJobData(job);
    }

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        if (math.abs(bankAmount) < 0.001f)
        {
            return; 
        }
        
        var bankPosition = new Vector3(
            settings.position.x * bankAmount * 0.01f, 
            settings.position.y * bankAmount * 0.01f, 
            settings.position.z * bankAmount * 0.01f);
        
        var weightedBankRotation = Quaternion.Euler(new Vector3(
            settings.rotation.x * bankAmount * (1 - settings.spineMultiplier),
            settings.rotation.y * bankAmount * (1 - settings.spineMultiplier),
            settings.rotation.z * bankAmount) * (1 - settings.spineMultiplier));
        
        var bankRotation = Quaternion.Euler(new Vector3(
            settings.rotation.x * bankAmount,
            settings.rotation.y * bankAmount,
            settings.rotation.z * bankAmount));
        
        var footPosition = new Vector3(
            settings.position.x * bankAmount * 0.01f *settings.footMultiplier, 
            settings.position.y * bankAmount * 0.01f * settings.footMultiplier, 
            settings.position.z * bankAmount * 0.01f * settings.footMultiplier);
        
        //TODO: A multiplier here??
        var footRotation = Quaternion.Euler(new Vector3(
            settings.rotation.x * bankAmount * settings.footMultiplier,
            settings.rotation.y * bankAmount * settings.footMultiplier,
            settings.rotation.z * bankAmount * settings.footMultiplier));

        // Humanoid
        if (stream.isHumanStream)
        {         
            var humanStream = stream.AsHuman();

            var position = humanStream.bodyLocalPosition;
            humanStream.bodyLocalRotation = weightedBankRotation * humanStream.bodyLocalRotation;
            humanStream.bodyLocalPosition = bankRotation * position + bankPosition;
			
            var numHandles = m_HeadLeftRightMuscles.Length;
            var multiplier = bankAmount * 0.075f * settings.headMultiplier / numHandles;
            for (var i = 0; i < numHandles; i++)
            {
                var headLeftRight = humanStream.GetMuscle(m_HeadLeftRightMuscles[i]);
                humanStream.SetMuscle(m_HeadLeftRightMuscles[i], headLeftRight + multiplier);               
            }

            numHandles = m_SpineLeftRightMuscles.Length;
            multiplier = bankAmount * 0.075f * settings.spineMultiplier / numHandles;
            for (var i = 0; i < numHandles; i++)
            {
                var spineLeftRight = humanStream.GetMuscle(m_SpineLeftRightMuscles[i]);
                humanStream.SetMuscle(m_SpineLeftRightMuscles[i], spineLeftRight + multiplier);
            }
                        
            humanStream.SetGoalLocalPosition(AvatarIKGoal.LeftFoot, humanStream.GetGoalLocalPosition(AvatarIKGoal.LeftFoot) + footPosition);
            humanStream.SetGoalRotation(AvatarIKGoal.LeftFoot, humanStream.GetGoalRotation(AvatarIKGoal.LeftFoot) * footRotation);
            humanStream.SetGoalLocalPosition(AvatarIKGoal.RightFoot, humanStream.GetGoalLocalPosition(AvatarIKGoal.RightFoot) + footPosition);
            humanStream.SetGoalRotation(AvatarIKGoal.RightFoot, humanStream.GetGoalRotation(AvatarIKGoal.RightFoot) * footRotation);
        }

        // Generic
        else         // TODO: Flesh this path out or consider loosing it
        {
            m_SkeletonHandle.SetLocalPosition(stream, m_SkeletonHandle.GetLocalPosition(stream) + bankPosition);
            m_SkeletonHandle.SetLocalRotation(stream, m_SkeletonHandle.GetLocalRotation(stream) * bankRotation);
        }
    }
}
