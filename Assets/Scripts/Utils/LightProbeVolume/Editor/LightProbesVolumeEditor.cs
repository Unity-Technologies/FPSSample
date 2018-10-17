using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;

[CustomEditor(typeof(LightProbesVolumeSettings))]
public class LightProbesVolumeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        var volume = (LightProbesVolumeSettings)target;
        base.DrawDefaultInspector();
        if (GUILayout.Button("Create Light Probes in Selected Volume"))
        {
            volume.Populate();
        }
    }

    [MenuItem("GameObject/Light/Lightprobes Volume", false, 10)]
    static void CreateCustomGameObject(MenuCommand menuCommand)
    {
        // Create a custom game object
        GameObject volume = new GameObject("LightprobeVolume");
        // Ensure it gets reparented if this was a context click (otherwise does nothing)
        GameObjectUtility.SetParentAndAlign(volume, menuCommand.context as GameObject);
        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(volume, "Create " + volume.name);
        Selection.activeObject = volume;
        volume.AddComponent<LightProbesVolumeSettings>();
        volume.GetComponent<BoxCollider>().size = new Vector3(5, 2, 5);
    }
}