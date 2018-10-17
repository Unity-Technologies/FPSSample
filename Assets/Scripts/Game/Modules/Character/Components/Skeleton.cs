using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class Skeleton : MonoBehaviour, ISkeletonTypeComponent//
{    
    public static event Action<Skeleton> SkeletonEnabled;
    public static event Action<Skeleton> SkeletonDisabled;

    public bool drawSkeleton;
    public Color skeletonColor = Color.green;
    [Range(0.01f, 5.0f)]
    public float boneSize = 1.0f;
    public bool drawTripods;


    // TODO: Make bone arrays show up in editor as read only @sunek
    [FormerlySerializedAs("m_Bones")]
    public Transform[] bones;
    [FormerlySerializedAs("m_BoneNames")]
    public int[] nameHashes;
    [FormerlySerializedAs("m_ParentIndex")]
    public int[] parentIndex;
    [FormerlySerializedAs("m_Bindpose")]
    public Bonepose[] importPose;

    [Serializable]
    public struct Bonepose
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

#if UNITY_EDITOR
    void Start()
    {
        skeletonColor = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f, 1f, 1f);        
    }
    
    void OnEnable()
    {
        if (SkeletonEnabled != null)
        {
            SkeletonEnabled(this);
        }
    }
    
    void OnDisable()
    {
        if (SkeletonDisabled != null)
        {
            SkeletonDisabled(this);
        }
    }
#endif
    
    public bool StoreBoneData(Transform skeletonRoot)
    {
        if (!skeletonRoot)
        {
            bones = new Transform[0];
            nameHashes = new int[0];
            parentIndex = new int[0];
            importPose = new Bonepose[0];
            return false; 
        }

        var boneList = new List<Transform>();
        GetBones(skeletonRoot, skeletonRoot, ref boneList);

        var numBones = boneList.Count;
        bones = boneList.ToArray();

        nameHashes = new int[numBones];
        importPose = new Bonepose[numBones];

        for (var i = 0; i < numBones; i++)
        {
            string boneName = bones[i].gameObject.name;
            int hashCode = boneName.GetHashCode();
            nameHashes[i] = hashCode;
            var bindpose = new Bonepose
            {
                localPosition = bones[i].localPosition,
                localRotation = bones[i].localRotation,
                localScale = bones[i].localScale
            };

            importPose[i] = bindpose;
        }

        parentIndex = new int[numBones];
        for (var i = 0; i < numBones; i++)
        {
            parentIndex[i] = GetBoneIndex(bones[i].parent.gameObject.name.GetHashCode());
        }
        return true;
    }

    public int GetBoneIndex(int stringHash)
    {
        // TODO: (sunek) Consider lazily building a persisted hashmap or do binary search on a sorted hash sequence
        var numBones = bones.Length;
        for (var i = 0; i < numBones; i++)
        {
            if (nameHashes[i] == stringHash)
                return i;
        }

        return -1;
    }

    static void GetBones(Transform t, Transform skeletonRoot, ref List<Transform> boneList)
    {
        var bonesToProcess = new Queue<Transform>();
        bonesToProcess.Enqueue(t);

        while (bonesToProcess.Count > 0)
        {
            var currentBone = bonesToProcess.Dequeue();
            boneList.Add(currentBone);

            var numChildren = currentBone.childCount;
            for (var i = 0; i < numChildren; i++)
            {
                bonesToProcess.Enqueue(currentBone.GetChild(i));
            }
        }
    }

    public bool GotoBindpose()
    {
        var numBones = bones.Length;
        if (numBones == 0 || numBones != importPose.Length)
        {
            return false;
        }

        for (var i = 0; i < numBones; i++)
        {
            if (bones[i] != null)
            {
                bones[i].localPosition = importPose[i].localPosition;
                bones[i].localRotation = importPose[i].localRotation;
                bones[i].localScale = importPose[i].localScale;
            }
        }

        return true;
    }
}
