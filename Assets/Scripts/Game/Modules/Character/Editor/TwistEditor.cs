using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Twist))]
public class TwistEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        Twist twistScript = (Twist) target;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Bindpose"))
        {
            twistScript.SetBindpose();
        }

        if (GUILayout.Button("Goto Bindpose"))
        {
            twistScript.GotoBindpose();
        }
        GUILayout.EndHorizontal();
        // EditorGUILayout.HelpBox("This is a help box", MessageType.Info);
    }
}

