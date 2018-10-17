using System;
using UnityEngine;
using UnityEditor;
using static UnityEditor.EditorUtility;

[CustomEditor(typeof(Skeleton))]
public class SkeletonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var skeletonComponent = (Skeleton) target;

        if (GUILayout.Button("Goto Import Pose"))
        {   
            // TODO: Is this safe?
            Undo.RecordObjects(skeletonComponent.bones, "Skeleton Component: Goto Bindpose");
            var result = skeletonComponent.GotoBindpose();

            if (!result)
            {
                var componentName = skeletonComponent.GetType();
                var message = "Unable to go to bindpose..";
                DisplayDialog($"{componentName} : Goto Bindpose Error", message, "OK'");
                Debug.Log($"{componentName} : {message}");
            }
        }
    }
}