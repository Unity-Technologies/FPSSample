using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;

public class RefreshLightProbesVolumes : EditorWindow
{
    [MenuItem("Lighting/Refresh lightprobes volumes")]
    static void Refresh()
    {
        var volumes = GameObject.FindObjectsOfType<LightProbesVolumeSettings>();
        foreach (var volume in volumes)
        {
            volume.Populate();
        }
    }
}