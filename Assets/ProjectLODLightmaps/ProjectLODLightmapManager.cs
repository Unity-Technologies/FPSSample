using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

#if UNITY_EDITOR

using UnityEditor;

[ExecuteInEditMode]
public class ProjectLODLightmapManager : MonoBehaviour {

    void OnEnable()
    {
        EditorApplication.playModeStateChanged += PlayModeChange;
        Lightmapping.completed += SetupRenderers;
    }

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= PlayModeChange;
        Lightmapping.completed -= SetupRenderers;
    }

    void Start ()
    {
        SetupRenderers();
    }

    static void PlayModeChange(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            SetupRenderers();
        }
    }

    static void SetupRenderers()
    {
        Profiler.BeginSample("ProjectLODLightmapManager.SetupRenderers");
        ProjectLODLightmaps[] projectors = FindObjectsOfType<ProjectLODLightmaps>();

        foreach (var projector in projectors)
        {
            projector.SetupRenderer();
        }
        Profiler.EndSample();
    }
}

#endif