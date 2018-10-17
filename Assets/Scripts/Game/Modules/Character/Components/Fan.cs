using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

// TODO: (sunek) With the introduction of prefabs, evaluating procedural bones in edit mode will dirty the prefab.
// Find a way to not evaluate the script in prefab isolation mode or better yet address the bones in a way that does
// not change their serialized values (e.g. with a playable graph)
[ExecuteAlways]
public class Fan : MonoBehaviour, ISkeletonTypeComponent
{
    public List<FanData> fanDatas = new List<FanData>();

    [Serializable]
    public struct FanData
    {
        public Transform driven;
        public Transform driverA;
        public Transform driverB;

        public bool HasValidData()
        {
            return driven != null && driverA != null && driverB != null;
        }
    }

#if UNITY_EDITOR
    void LateUpdate()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPaused)
            return;

        for (var i = 0; i < fanDatas.Count; i++)
        {
            if (fanDatas[i].HasValidData())
            {
                fanDatas[i].driven.rotation = math.slerp(fanDatas[i].driverA.rotation, fanDatas[i].driverB.rotation, 0.5f);
            }
        }
    }
#endif
}