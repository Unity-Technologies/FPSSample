using System;
using UnityEngine;
using UnityEngine.Experimental.Animations;

public struct CameraNoiseJob : IAnimationJob
{
    [Serializable]
    public struct JobSettings
    {
        [Range(0f, 1f)]
        public float dollyMagnitude;
        [Range(0f, 10f)]
        public float dollySpeed;
        [Range(0f, 1f)]
        public float panMagnitude;
        [Range(0f, 10f)]
        public float panSpeed;
        [Range(0f, 1f)]
        public float pedistalMagnitude;
        [Range(0f, 10f)]
        public float pedistalSpeed;
        [Range(0f, 1f)]
        public float bias;
    }
    
    [Serializable]
    public struct EditorSettings
    {
        // The non job reference types
        public Transform cameraBone;
        public Transform characterRootBone;

        // All the value types
        public JobSettings jobSettings;
    }
    
    TransformStreamHandle m_CameraHandle;
    TransformStreamHandle m_CharacterRoot;
    
    float m_DollyPos;
    float m_PanPos;
    float m_TiltPos;
    
    public JobSettings jobSettings;
    public Vector3 target;
//
    public bool Setup(Animator animator, EditorSettings editorSettings)
    {   
        jobSettings = editorSettings.jobSettings;
   
        m_CameraHandle = animator.BindStreamTransform(editorSettings.cameraBone);
        m_CharacterRoot = animator.BindStreamTransform(editorSettings.characterRootBone);
        return true;
    }
//    
    public void Update(Vector3 target, JobSettings jobSettings, AnimationScriptPlayable playable)
    {
        var job = playable.GetJobData<CameraNoiseJob>();
        job.target = target;
        job.jobSettings = jobSettings;
        playable.SetJobData(job);
    }
    
    public void Dispose() {}

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        var speedMult = 1f;
        
        var dollyPos = (Mathf.PerlinNoise(m_DollyPos, 0f) - 0.5f) * jobSettings.dollyMagnitude;
        m_DollyPos += jobSettings.dollySpeed * speedMult * stream.deltaTime;
            
        var panPos = (Mathf.PerlinNoise(m_PanPos, 0f) - 0.5f) * jobSettings.panMagnitude;
        m_PanPos -= jobSettings.panSpeed * speedMult * stream.deltaTime;
            
        var tiltPos = (Mathf.PerlinNoise(0f, m_TiltPos) - 0.5f) * jobSettings.pedistalMagnitude;
        m_TiltPos += jobSettings.pedistalSpeed * speedMult * stream.deltaTime;
        
        var offset = Quaternion.LookRotation(target) * new Vector3(panPos, tiltPos, dollyPos);    
//        var offset = new Vector3(panPos, tiltPos, dollyPos);    
       
        var cameraPos = m_CameraHandle.GetPosition(stream);
        var bias = jobSettings.bias;
        cameraPos = new Vector3(cameraPos.x + offset.x * bias, cameraPos.y + offset.y * bias, cameraPos.z + offset.z * bias);
        m_CameraHandle.SetPosition(stream, cameraPos);  
        
        var rootPos = m_CharacterRoot.GetPosition(stream);
        bias =  1 - jobSettings.bias;
        rootPos = new Vector3(rootPos.x + offset.x * bias, rootPos.y + offset.y * bias, rootPos.z + offset.z * bias);
        m_CharacterRoot.SetPosition(stream, rootPos);
    }
}