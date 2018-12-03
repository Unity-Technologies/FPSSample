using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Animations;

// TODO: Blending foot ik on and off?
//[BurstCompile(CompileSynchronously = true)]
public struct VelBasedBlendJob : IAnimationJob
{
    NativeArray<TransformStreamHandle> m_Bones;
    NativeArray<TransformStreamHandle> m_IkHandles;

    // Output history
    NativeArray<float3> m_OutputPositions;
    NativeArray<quaternion> m_OutputRotations;
    NativeArray<float3> m_OutputScales;

    NativeArray<float3> m_OutputIkPositions;
    NativeArray<quaternion> m_OutputIkRotations;

    // Position blend data
    NativeArray<float3> m_PosDirArray;
    NativeArray<float> m_PosDeltaArray;
    NativeArray<float> m_PosDeltaVelArray;

    // Rotation blend data
    NativeArray<float3> m_RotAxisArray;
    NativeArray<float> m_RotDeltaArray;
    NativeArray<float> m_RotDeltaVelArray;
    
    // Scale blend data
    NativeArray<float3> m_StarteScaleArray;

    NativeArray<float3> m_FromIkPositions;
    NativeArray<quaternion> m_FromIkRotations;

    const int k_OutputBufferCount = 2;
    const int k_NumIkHandles = 4;
    int m_CurrentBufferIndex;
    int m_StoredOutputCount;

    public bool doTransition;
    public bool inTransition;
    public float transitionDuration;
    public float transitionTimeRemaining;

    public void Setup(Animator animator, Skeleton skeleton, AvatarMask boneMask)
    {
        int numBones;
        var activeBones = new List<Transform>();        
        if (boneMask != null)
        {
            for (var i = 0; i < boneMask.transformCount; i++)
            {
                if (boneMask.GetTransformActive(i))
                {   
                    var tokens = boneMask.GetTransformPath(i).Split('/');
                    var name = tokens[tokens.Length - 1];
                    var skeletonIndex = skeleton.GetBoneIndex(name.GetHashCode());

                    if (skeletonIndex != -1)
                    {
                        activeBones.Add(skeleton.bones[skeletonIndex]);
                    }
                }
            }  
            
            numBones = activeBones.Count;
        }

        else
        {
            numBones = skeleton.bones.Length;   
        }
        
        m_CurrentBufferIndex = -1;
        
        m_Bones = new NativeArray<TransformStreamHandle>(numBones, Allocator.Persistent);

        m_OutputPositions = new NativeArray<float3>(numBones * k_OutputBufferCount, Allocator.Persistent);
        m_OutputRotations = new NativeArray<quaternion>(numBones * k_OutputBufferCount, Allocator.Persistent);
        m_OutputScales = new NativeArray<float3>(numBones * k_OutputBufferCount, Allocator.Persistent);

        m_OutputIkPositions = new NativeArray<float3>(k_NumIkHandles * k_OutputBufferCount, Allocator.Persistent);
        m_OutputIkRotations = new NativeArray<quaternion>(k_NumIkHandles * k_OutputBufferCount, Allocator.Persistent);

        m_PosDirArray = new NativeArray<float3>(numBones, Allocator.Persistent);
        m_PosDeltaArray = new NativeArray<float>(numBones, Allocator.Persistent);
        m_PosDeltaVelArray = new NativeArray<float>(numBones, Allocator.Persistent);
        
        m_RotAxisArray = new NativeArray<float3>(numBones, Allocator.Persistent);
        m_RotDeltaArray = new NativeArray<float>(numBones, Allocator.Persistent);
        m_RotDeltaVelArray = new NativeArray<float>(numBones, Allocator.Persistent); 
        
        m_StarteScaleArray = new NativeArray<float3>(numBones, Allocator.Persistent);

        m_FromIkPositions = new NativeArray<float3>(k_NumIkHandles, Allocator.Persistent);
        m_FromIkRotations = new NativeArray<quaternion>(k_NumIkHandles, Allocator.Persistent);
        
        
        
        for (var i = 0; i < numBones; i ++)
        {
            if (boneMask != null)
            {
                m_Bones[i] = animator.BindStreamTransform(activeBones[i]);                
            }
            else
            {
                m_Bones[i] = animator.BindStreamTransform(skeleton.bones[i]);                
            }
            
        }     
    }

    public void Dispose()
    {
        m_Bones.Dispose();
        m_OutputPositions.Dispose();
        m_OutputRotations.Dispose();
        m_OutputIkPositions.Dispose();
        m_OutputIkRotations.Dispose();

        m_PosDirArray.Dispose();
        m_PosDeltaArray.Dispose();
        m_PosDeltaVelArray.Dispose();
        
        m_RotAxisArray.Dispose();
        m_RotDeltaArray.Dispose();
        m_RotDeltaVelArray.Dispose(); 
        
        m_StarteScaleArray.Dispose();

        m_FromIkPositions.Dispose();
        m_FromIkRotations.Dispose();        
    }

    public static void Transition(AnimationScriptPlayable jobPlayable, float duration)
    {
        var job = jobPlayable.GetJobData<VelBasedBlendJob>();

        // Ignore transition when output buffer isnt full
        if (job.m_StoredOutputCount < k_OutputBufferCount)
            return;

        job.transitionDuration = duration;
        job.transitionTimeRemaining = duration;
        job.doTransition = true;
        job.inTransition = true;
        jobPlayable.SetJobData(job);
    }

    public void ProcessRootMotion(AnimationStream stream) { }

    
    const int k_debugBone = 15;
    
    public void ProcessAnimation(AnimationStream stream)
    {
        var invDeltaTime = 1.0f / stream.deltaTime;
            
        // Store from pose
        if (doTransition)
        {

            var row = 0;
            var prevIndexOffset = (m_CurrentBufferIndex + 1) % k_OutputBufferCount; 
            for (var i = 0; i < m_Bones.Length; i++)
            {
                var lastOutputBufferIndex = row + m_CurrentBufferIndex;
                var prevOutputBufferIndex  = row + prevIndexOffset;

                // Position
                {
                    var currentPos = (float3)m_Bones[i].GetLocalPosition(stream);
                    var vLast = m_OutputPositions[lastOutputBufferIndex];
                    var vPrev = m_OutputPositions[prevOutputBufferIndex];

                    var vDelta = vLast - currentPos;
                    var delta = math.length(vDelta);
                    var dir = math.normalize(vDelta);

                    var vDeltaPrev = vPrev - currentPos;
                    var deltaPrev = math.dot(vDeltaPrev, dir);
                    
                    var vel = (delta - deltaPrev)* invDeltaTime;

//                    if(i == k_debugBone)
//                        Debug.Log("start delta:" + delta + " vel:" + vel);

                    
                    // If delta is negative we invert properties to keep delta positive
                    if (delta < 0)
                    {
                        delta = -delta;
                        vel = -vel;
                        dir = -dir;
//                        
//                        if(i == k_debugBone)
//                            Debug.Log("   ... negated to delta:" + delta + " vel:" + vel);
                    }

//                    if(i == k_debugBone && vel > 0)
//                        Debug.Log("   ... vel positive so its clamped to 0");
                    
                    vel = vel < 0 ? vel : 0; 

                    
                    
                    m_PosDirArray[i] = dir;
                    m_PosDeltaArray[i] = delta;
                    m_PosDeltaVelArray[i] = vel;
                }
                
                // Rotation
                {
                    var currentRot = m_Bones[i].GetLocalRotation(stream);
                    var qLast = m_OutputRotations[lastOutputBufferIndex];
                    var qPrev = m_OutputRotations[prevOutputBufferIndex];

                    var qInvCurrentRot = math.inverse(currentRot);
                    var qDeltaRot = math.mul(qLast,qInvCurrentRot);               
                    var qDeltaPrevRot = math.mul(qPrev,qInvCurrentRot);

                    float3 rotAxis;
                    float delta;
                    GetAxisAngle(qDeltaRot, out rotAxis, out delta);

                    // TODO (mogensh) use this !!
                    var prevRot = TwistAroundAxis(qDeltaPrevRot, rotAxis);
                    
                    float3 rotAxis2;
                    float rot2;
                    GetAxisAngle(qDeltaPrevRot, out rotAxis2, out rot2);

                    var vel = (delta - rot2) * invDeltaTime;
                    
                    m_RotAxisArray[i] = rotAxis;
                    m_RotDeltaArray[i] = delta;
                    m_RotDeltaVelArray[i] = vel;
                }
                
                // Scale
                m_StarteScaleArray[i] = m_OutputScales[lastOutputBufferIndex];

                row += k_OutputBufferCount;
            }

            for (var i = 0; i < k_NumIkHandles; i++)
            {
                var index = i * k_OutputBufferCount + m_CurrentBufferIndex;   
                m_FromIkPositions[i] = m_OutputIkPositions[index];
                m_FromIkRotations[i] = m_OutputIkRotations[index];
            }

            doTransition = false;
        }

        // Decrement delta
        transitionTimeRemaining -= stream.deltaTime; // TODO: Find meaningful way of decrementing this
        if (transitionTimeRemaining <= 0f)
        {
            inTransition = false;
        }

        if (inTransition)
        {
            var transitionTime = transitionDuration - transitionTimeRemaining;
            
            var factor = transitionTimeRemaining / transitionDuration;
            for (var i = 0; i < m_Bones.Length; i++)
            {
                // Position
                {
                    var startDelta = m_PosDeltaArray[i];
                    var deltaVel = m_PosDeltaVelArray[i];
                    
//                    var delta = startDelta + deltaVel * transitionTime;

                    var delta = Approach(startDelta, deltaVel, transitionDuration, transitionTime);        
                        
//                    if(i == k_debugBone)
//                        Debug.Log("startDelta:" + startDelta + " deltaVel:" + deltaVel + " time:" + transitionTime + " delta:" + delta);
                    
                    var dir = m_PosDirArray[i];
                    var vDelta = delta * dir;                     
                    var streamPos = (float3)m_Bones[i].GetLocalPosition(stream);
                        
                    var pos = streamPos + vDelta;
                    m_Bones[i].SetLocalPosition(stream,pos);
                }
                
                // Rotation
                {
                    var startDelta = m_RotDeltaArray[i];
                    var deltaVel = m_RotDeltaVelArray[i];
                   // var delta = startDelta + deltaVel * transitionTime;
                    var delta = Approach(startDelta, deltaVel, transitionDuration, transitionTime);

                    var rotAxis = m_RotAxisArray[i];
                    var qDeltaRot = quaternion.AxisAngle(rotAxis, delta);
                    var qStreamRot = m_Bones[i].GetLocalRotation(stream);
                
                    var qRot = qDeltaRot * qStreamRot;            
                   
                    
                    m_Bones[i].SetLocalRotation(stream,qStreamRot);
                }
                
                
                // Scale
                m_Bones[i].SetLocalScale(stream, m_Bones[i].GetLocalScale(stream));
            }


            if (stream.isHumanStream)
            {
                var humanStream = stream.AsHuman();
                for (var i = 0; i < k_NumIkHandles; i++)
                {                
                    var position = math.lerp(humanStream.GetGoalLocalPosition((AvatarIKGoal)i), m_FromIkPositions[i], factor);
                    humanStream.SetGoalLocalPosition((AvatarIKGoal)i, position);
                    var rotation = math.slerp(humanStream.GetGoalLocalRotation((AvatarIKGoal)i), m_FromIkRotations[i], factor);
                    humanStream.SetGoalLocalRotation((AvatarIKGoal)i, rotation);
                }                
            }
        }

        // Store output values
        StoreOutput(stream);
    }

    void GetAxisAngle(quaternion q, out float3 axis, out float angle)
    {
        angle = 2 * math.acos(q.value.w);
        axis = math.normalize(q.value.xyz);
    }

    float TwistAroundAxis(quaternion q, float3 axis)
    {
        var a = math.dot(math.normalize(q.value.xyz), axis) / q.value.w;
        return 2*math.atan(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float Approach(float x, float v, float dur, float t)
    {
        //dur = math.min(dur, -5 * x / v);
        
        var dur2 = math.pow(dur, 2);
        var dur3 = math.pow(dur, 3);
        var dur4 = math.pow(dur, 4);
        var dur5 = math.pow(dur, 5);
        
        var a = (-8 * v * dur - 20 * x) / dur2;
        a = a > 0 ? a : 0;

        var A = (a * dur + 6 * v * dur + 12 * x) / 2 * dur5;
        var B = (3 * a * dur2 + 16 * v * dur + 30 * x) / 2 * dur4;
        var C = (3 * a * dur2 + 12 * v * dur * 20 * x) / 2 * dur3;

        var result = A * math.pow(t, 5) + B * math.pow(t, 4) + C * math.pow(t, 3) + (a / 2) * math.pow(t, 2) + v * t + x;        
        
        return result;
    }
    

    void StoreOutput(AnimationStream stream)
    {       
        m_CurrentBufferIndex = (m_CurrentBufferIndex + 1) % k_OutputBufferCount;

        {
            var index = m_CurrentBufferIndex;
            for (var i = 0; i < m_Bones.Length; i++)
            {
                m_OutputPositions[index] = m_Bones[i].GetLocalPosition(stream);
                m_OutputRotations[index] = m_Bones[i].GetLocalRotation(stream);
                m_OutputScales[index] = m_Bones[i].GetLocalScale(stream);
                index += k_OutputBufferCount;
            }
        }

        if (stream.isHumanStream)
        {
            var humanStream = stream.AsHuman();
            for (var i = 0; i < k_NumIkHandles; i++)
            {
                var index = i * k_OutputBufferCount + m_CurrentBufferIndex;
                m_OutputIkPositions[index] = humanStream.GetGoalLocalPosition((AvatarIKGoal)i);
                m_OutputIkRotations[index] = humanStream.GetGoalLocalRotation((AvatarIKGoal)i);
            }
        }

        m_StoredOutputCount++;
    }
}
