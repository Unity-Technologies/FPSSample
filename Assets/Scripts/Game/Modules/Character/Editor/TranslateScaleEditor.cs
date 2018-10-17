using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TranslateScale))]
public class TranslateScaleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        TranslateScale translateScaleScript = (TranslateScale) target;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Bindpose"))
        {
            translateScaleScript.SetBindpose();
        }

        if (GUILayout.Button("Goto Bindpose"))
        {
            translateScaleScript.GotoBindpose();
        }
        GUILayout.EndHorizontal();
        // EditorGUILayout.HelpBox("This is a help box", MessageType.Info);
    }
}

