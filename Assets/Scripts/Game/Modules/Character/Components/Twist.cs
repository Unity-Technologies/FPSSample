using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

// TODO: (sunek) With the introduction of prefabs, evaluating procedural bones in edit mode will dirty the prefab.
// Find a way to not evaluate the script in prefab isolation mode or better yet address the bones in a way that does
// not change their serialized values (e.g. with a playable graph)
[ExecuteAlways]
public class Twist : MonoBehaviour, ISkeletonTypeComponent
{
    public List<TwistChain> twistChains = new List<TwistChain>();
    
//    public AimAxis aimAxis;

//    public enum AimAxis
//    {
//        X, Y, Z
//    }

    [Serializable]
    public struct TwistJoint
    {
        public Transform joint;
        [Range(-1.0f, 1.0f)]
        public float factor;
    }

    [Serializable]
    public struct TwistChain
    {
        public Transform driver;
        public List<TwistJoint> twistJoints;

        [HideInInspector]
        public quaternion bindpose;

        public bool HasValidData()
        {
            for (var i = 0; i < twistJoints.Count; i++)
            {
                if (twistJoints[i].joint == null)
                    return false;
            }

            return driver != null;
        }
    }

    public void SetBindpose()
    {
        for (var i = 0; i < twistChains.Count; i++)
        {
            var twistChain = twistChains[i];
            twistChain.bindpose = twistChain.driver.localRotation;
            twistChains[i] = twistChain;
        }
    }

    public void GotoBindpose()
    {
        for (var i = 0; i < twistChains.Count; i++)
        {
            twistChains[i].driver.localRotation = twistChains[i].bindpose;
        }
    }

#if UNITY_EDITOR
    void LateUpdate()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPaused)
            return;

        for (var i = 0; i < twistChains.Count; i++)
        {
            var twistChain = twistChains[i];

            if (!twistChain.HasValidData())
                continue;

             var delta = math.inverse(twistChain.bindpose) * twistChain.driver.localRotation;

            var twist = new quaternion(0.0f, delta.y, 0.0f, delta.w);
            
            // TODO: Add support for variable axis
//            quaternion twist;
//            switch (aimAxis)
//            {
//                case AimAxis.X:
//                    twist = new quaternion(delta.x, 0.0f, 0.0f, delta.w);
//                    break;
//                case AimAxis.Y:
//                    twist = new quaternion(0.0f, delta.y, 0.0f, delta.w);
//                    break;
//                case AimAxis.Z:
//                    twist = new quaternion(0.0f, 0.0f, delta.z, delta.w);
//                    break;
//                default:
//                    twist = quaternion.identity;
//                    break;
//            }

            // Apply rotation to twist joints
            int numIterations = twistChain.twistJoints.Count;
            for (int j = 0; j < numIterations; ++j)
            {
                if (twistChain.twistJoints[j].joint == null)
                    continue;

                var twistRotation = math.slerp(quaternion.identity, twist, twistChain.twistJoints[j].factor);
                twistChain.twistJoints[j].joint.localRotation = twistRotation;
            }
        }       
    }
#endif

}
