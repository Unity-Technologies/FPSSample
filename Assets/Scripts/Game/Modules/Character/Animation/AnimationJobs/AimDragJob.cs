using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Animations;

public struct AimDragJob : IAnimationJob
{
    [Serializable]
    public struct Settings
    {
        [Range(0.0f, 10f)]
        public float stiffness;
        [Range(0.0f, 180f)]
        public float maxAngle;
        [Range(-1.0f, 1.0f)]
        public float dragWeight;
        [Range(-1.0f, 1.0f)]
        public float rollWeight;
        [Range(0, 10)]
        public int rollDelay;
        public Vector3 dragPivot;
        public Vector3 rollPivot; 
    }
    
    [Serializable]
    public struct EditorSettings
    {
        // The non job reference types
        public Transform weaponHandBone;
        public Transform weaponBone;
        public Transform applyResultOn;
        
        // All the value types
        public Settings settings;

        public bool HasValidData()
        {
            return weaponHandBone != null & weaponBone != null & applyResultOn != null;
        }
    }
    
    TransformStreamHandle m_Effectorhandle;
    TransformStreamHandle m_WeaponPivot;
    TransformSceneHandle m_WeaponHandResult;
    
    bool m_AimDirectionInitialized;
    NativeQueue<Quaternion> m_DragHistory;
    float m_CurrentYaw;
    float m_CurrentPitch;
    
    public Settings settings;
    public CharacterInterpolatedData animState;

    public bool Setup(Animator animator, EditorSettings editorSettings, float deltaTime, NativeQueue<Quaternion> dragHistory)
    {
        if (!editorSettings.HasValidData())
        {
            return false;
        }
        
        settings = editorSettings.settings;   
        m_Effectorhandle = animator.BindStreamTransform(editorSettings.weaponHandBone);
        m_WeaponPivot = animator.BindStreamTransform(editorSettings.weaponBone);
        m_WeaponHandResult = animator.BindSceneTransform(editorSettings.applyResultOn);
        m_DragHistory = dragHistory;
        return true;
    }
    
    public void Update(Vector3 target, Settings settings, CharacterInterpolatedData animationState, AnimationScriptPlayable playable)
    {
        var job = playable.GetJobData<AimDragJob>();
        job.settings = settings;
        job.animState = animationState;
        playable.SetJobData(job);
    }
    

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {        
        if (!m_AimDirectionInitialized)
        {
            m_CurrentYaw = animState.aimYaw;
            m_CurrentPitch = animState.aimPitch;
            m_AimDirectionInitialized = true;
        }
        
        // TODO: (sunek) Get rid of stuttering
        var t = settings.stiffness * stream.deltaTime;
        m_CurrentYaw = Mathf.LerpAngle(m_CurrentYaw, animState.aimYaw, t);
        m_CurrentPitch = Mathf.LerpAngle(m_CurrentPitch, animState.aimPitch, t);
        
        var deltaYaw = Mathf.DeltaAngle(animState.aimYaw,  m_CurrentYaw);
        var deltaPitch = Mathf.DeltaAngle(animState.aimPitch, m_CurrentPitch);
        var deltaRotation = Quaternion.Euler(new Vector3(-deltaPitch, deltaYaw, 0));

        var angle = Quaternion.Angle(Quaternion.identity, deltaRotation);        
        if (angle > settings.maxAngle)
        {
            t = settings.maxAngle / angle;
            m_CurrentYaw = Mathf.LerpAngle(animState.aimYaw, m_CurrentYaw, t);
            m_CurrentPitch = Mathf.LerpAngle(animState.aimPitch, m_CurrentPitch, t);
            
            deltaYaw = Mathf.DeltaAngle(animState.aimYaw,  m_CurrentYaw);
            deltaPitch = Mathf.DeltaAngle(animState.aimPitch, m_CurrentPitch);
            deltaRotation = Quaternion.Euler(new Vector3(-deltaPitch, deltaYaw, 0));
        }
  
        if (m_DragHistory.Count <= settings.rollDelay)
        {
            m_DragHistory.Enqueue(deltaRotation);
        }

        var drag = Quaternion.SlerpUnclamped(Quaternion.identity, deltaRotation, settings.dragWeight);
        var roll = m_DragHistory.Count < settings.rollDelay + 1 ? m_DragHistory.Peek() : m_DragHistory.Dequeue();
        roll = Quaternion.SlerpUnclamped(Quaternion.identity, new Quaternion(0f, roll.y, 0f, roll.w), settings.rollWeight);
        
        var handPosition = m_Effectorhandle.GetPosition(stream);
        var handRotation = m_Effectorhandle.GetRotation(stream);
        var weaponPivotPosition = m_WeaponPivot.GetPosition(stream);
        var weaponPivotRotation = m_WeaponPivot.GetRotation(stream);

        var dragPivot = weaponPivotRotation * settings.dragPivot;
        var rollPivot = weaponPivotRotation * settings.rollPivot;   

        var dragPivotDelta = handPosition - (weaponPivotPosition + dragPivot);
        var rollPivotDelta = handPosition - (weaponPivotPosition + rollPivot);
        
        var handOffset = drag * dragPivotDelta;
        handOffset = handOffset - dragPivotDelta;
   
        var rollOffset = roll * rollPivotDelta;
        rollOffset = rollOffset - rollPivotDelta;
        
        handPosition = handPosition + handOffset + rollOffset;
        handRotation = drag * handRotation * roll;
        
        m_WeaponHandResult.SetPosition(stream, handPosition);
        m_WeaponHandResult.SetRotation(stream, handRotation);
    }
}