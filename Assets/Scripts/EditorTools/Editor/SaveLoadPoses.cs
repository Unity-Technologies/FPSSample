using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;


public static class PoseType
{
    public static string tpose = "tpose";
    public static string bindpose = "bindpose";
    public static string humanpose = "humanpose";
}


[Serializable]
public class Bone : ISerializationCallbackReceiver
{
    public string name;

    [NonSerialized] public Vector3 position;
    [NonSerialized] public Quaternion rotation;

    [SerializeField] private float[] pos = new float[3];
    [SerializeField] private float[] rot = new float[4];

    Bone() {}

    public Bone(string name, Vector3 position, Quaternion rotation)
    {
        this.name = name;
        this.position = position;
        this.rotation = rotation;
    }

    public void OnBeforeSerialize()
    {
        var serializedPosition = position;
        var serializedRotation = rotation;
        changeHandedness(ref serializedPosition, ref serializedRotation);

        pos = new[] { serializedPosition.x, serializedPosition.y, serializedPosition.z };
        rot = new[] { serializedRotation.x, serializedRotation.y, serializedRotation.z, serializedRotation.w };
    }

    public void OnAfterDeserialize()
    {
        position = new Vector3(pos[0], pos[1], pos[2]);
        rotation = new Quaternion(rot[0], rot[1], rot[2], rot[3]);
        changeHandedness(ref position, ref rotation);
    }

    static void changeHandedness(ref Vector3 position, ref Quaternion rotation)
    {
        var spaceConvertMatrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, new Vector3(-1, 1, 1));
        var origMatrix = Matrix4x4.TRS(position, rotation, new Vector3(1, 1, 1));
        var flippedMatrix = spaceConvertMatrix * origMatrix;

        position = new Vector3(flippedMatrix[0, 3], flippedMatrix[1, 3], flippedMatrix[2, 3]);
        rotation = QuaternionFromMatrix(flippedMatrix);
    }

    static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }
}


[Serializable]
public class Pose
{
    public string name = "";
    public List<Bone> bones = new List<Bone>();
    Pose() {}

    public Pose(string name, List<Bone> bones)
    {
        this.name = name;
        this.bones = bones;
    }
}

[Serializable]
public class Poses
{
    public List<Pose> poses = new List<Pose>();
}

public static class SaveLoadPoses
{
    static string filepath => $"{Path.GetTempPath()}/Skeleton_Pose.json";

    [MenuItem("FPS Sample/Animation/Skeleton Pose Interop/Load from tmp folder: Bind Pose (Selected)")]
    static void LoadBindPose()
    {
        LoadPose(PoseType.bindpose);
    }

    [MenuItem("FPS Sample/Animation/Skeleton Pose Interop/Load from tmp folder: T-Pose (Selected)")]
    static void LoadRigPose()
    {
        LoadPose(PoseType.tpose);
    }
    
    [MenuItem("FPS Sample/Animation/Skeleton Pose Interop/Load from tmp folder: Human Pose (Selected)")]
    static void LoadHumanPose()
    {
        LoadPose(PoseType.humanpose);
    }

    [MenuItem("FPS Sample/Animation/Skeleton Pose Interop/")]
    static void SeperateMe()
    {
        EditorGUILayout.Separator();
    }

    [MenuItem("FPS Sample/Animation/Skeleton Pose Interop/Save to tmp folder: Bind Pose (Selected)")]
    static void SaveBindPose()
    {
        SavePose(PoseType.bindpose);
    }

    [MenuItem("FPS Sample/Animation/Skeleton Pose Interop/Save to tmp folder: T-Pose (Selected)")]
    static void SaveRigPose()
    {
        SavePose(PoseType.tpose);
    }
   
    [MenuItem("FPS Sample/Animation/Skeleton Pose Interop/Save to tmp folder: Human Pose (Selected)")]
    static void SaveHumanPose()
    {
        SavePose(PoseType.humanpose);
    }

    static Poses ReadFromJson()
    {
        var poses = new Poses();

        if (File.Exists(filepath))
        {
            var json = File.ReadAllText(filepath);
            poses = JsonUtility.FromJson<Poses>(json);
        }
       
        if (poses == null)
        {
            poses = new Poses();
        }

        return poses;
    }

    static void WriteToJson(Poses poses)
    {
        var json = JsonUtility.ToJson(poses, true);
        var writer = File.CreateText(filepath);
        writer.Close();

        File.WriteAllText(filepath, json);
    }

    static bool LoadPose(string poseType)
    {
        var poses = ReadFromJson();
        foreach (var pose in poses.poses)
        {
            if (pose.name == poseType)
            {
                if (SetPose(pose))
                {
                    Debug.Log("Successfully loaded pose: " + poseType);
                    return true; 
                }
            }
        }

        Debug.LogWarning("Failed to load pose..: " + poseType);
        return false;
    }
    
    static void SavePose(string poseType)
    {
        var poses = ReadFromJson();
        var newPose = GetPose(poseType);

        if (newPose.bones.Count < 1)
        {
            Debug.LogWarning("No bones found. Aborting save of pose: " + poseType);
            return;
        }

        var poseWritten = false;
        for (var i = 0; i < poses.poses.Count; i++)
        {
            if (poses.poses[i].name == poseType)
            {
                poses.poses[i] = newPose;
                poseWritten = true;
            }
        }

        if (!poseWritten)
            poses.poses.Add(newPose);

        WriteToJson(poses);
        Debug.Log("Successfully saved pose: " + poseType);
    }

    static Pose GetPose(string poseType)
    {
        var bones = new List<Bone>();
        var root = Selection.activeGameObject.transform;

        var hierarchy = root.GetComponentsInChildren<Transform>(true);

        foreach (var bone in hierarchy)
        {
            bones.Add(new Bone(bone.name, bone.localPosition, bone.localRotation));
        }

        return new Pose(poseType, bones);
    }

    static bool SetPose(Pose pose)
    {
        var selected = Selection.activeGameObject;
        if (selected == null) { return false; }

        var root = selected.transform;

        var hierarchy = root.GetComponentsInChildren<Transform>(true);
        var boneMap = new Dictionary<string, Transform>();

        foreach (var bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (!boneMap.ContainsKey(bone.name))
            {
                boneMap.Add(bone.name, bone);
            }
            else
            {
                Debug.Log("Bone already in map: " + bone.name);
            }
        }

        Undo.RecordObjects(hierarchy, "Set Skeleton Pose");
        foreach (var bonePose in pose.bones)
        {
            Transform bone;
            boneMap.TryGetValue(bonePose.name, out bone);

            if (bone != null)
            {
                bone.localPosition = bonePose.position;
                bone.localRotation = bonePose.rotation;
            }
        }

        return true;
    }
}
