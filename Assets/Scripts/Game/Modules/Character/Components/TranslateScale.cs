using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

// TODO: (sunek) With the introduction of prefabs, evaluating procedural bones in edit mode will dirty the prefab.
// Find a way to not evaluate the script in prefab isolation mode or better yet address the bones in a way that does
// not change their serialized values (e.g. with a playable graph)
[ExecuteAlways]
public class TranslateScale : MonoBehaviour, ISkeletonTypeComponent
{
    // todo: (sunek) Consider making function distance based (rather than on single axis)?
    public List<TranslateScaleChain> chains = new List<TranslateScaleChain>();
    
    // TODO: (sunek) Add support for variable axis
//    public AimAxis aimAxis;
//
//    public enum AimAxis
//    {
//        X, Y, Z
//    }
//    
    [Serializable]
    public struct Driven
    {
        public Transform joint;
        [HideInInspector]
        public float3 bindpose;
        [Range(0f, 1)]
        public float strectchFactor;
        [Range(0f, 1)]
        public float scaleFactor;

        public bool IsValid()
        {
            return joint != null;
        }
    }

    [Serializable]
    public struct TranslateScaleChain
    {
        public Transform driver;
        [HideInInspector]
        public float3 bindpose;
        public List<Driven> drivenJoints;

        public bool HasValidData()
        {
            for (var i = 0; i < drivenJoints.Count; i++)
            {
                if (drivenJoints[i].joint == null)
                    return false;
            }

            return driver != null;
        }
    }

    public void SetBindpose()
    {
        for (var i = 0; i < chains.Count; i++)
        {
            var chain = chains[i];
            chain.bindpose = chain.driver.localPosition;

            for (var j = 0; j < chain.drivenJoints.Count; j++)
            {
                var driven = chain.drivenJoints[j];
                driven.bindpose = driven.joint.localPosition;
                chain.drivenJoints[j] = driven;
            }
            
            chains[i] = chain;
        }       
    }
    
    public void GotoBindpose()
    {
        for (var i = 0; i < chains.Count; i++)
        {
            chains[i].driver.localPosition = chains[i].bindpose;
            for (var j = 0; j < chains[i].drivenJoints.Count; j++)
            {
                chains[i].drivenJoints[j].joint.localPosition = chains[i].drivenJoints[j].bindpose;
            }
            
        }
    }    
    
#if UNITY_EDITOR
    void LateUpdate()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPaused)
            return;

        for (var i = 0; i < chains.Count; i++)
        {
            var chain = chains[i];

            if (!chain.HasValidData())
                continue;
            
            var stretchOffset = chain.driver.transform.localPosition.y - chain.bindpose.y;
            var stretchFactor = chain.driver.transform.localPosition.y / chain.bindpose.y ;

            for (var j = 0; j < chain.drivenJoints.Count; j++)
            {
                var driven = chain.drivenJoints[j];
                if (driven.IsValid())
                {
                    driven.joint.localPosition = driven.bindpose + new float3(0f, stretchOffset * driven.strectchFactor, 0f);
                    var volumeScale = -driven.scaleFactor * stretchFactor + 1 + driven.scaleFactor;
                    driven.joint.localScale = new float3(volumeScale, 1f, volumeScale);
                }
            }
        }       
    }
#endif
}
