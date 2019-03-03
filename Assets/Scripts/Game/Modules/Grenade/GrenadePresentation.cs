#if UNITY_EDITOR
using System;
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(PresentationEntity))]
public class GrenadePresentation : MonoBehaviour
{
}


// Project specific
enum PlatformFlag
{
    Server = 1 << 0,
    ClientPC = 1 << 1,
}


#if UNITY_EDITOR
[CustomEditor(typeof(GrenadePresentation))]
public class GrenadePresentationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var grenadePresentation = target as GrenadePresentation;

        var presentation = grenadePresentation.GetComponent<PresentationEntity>();
        
        var serializedPresentation = new SerializedObject(presentation);
        
        var presentationOwner = serializedPresentation.FindProperty("presentationOwner");
        EditorGUILayout.PropertyField(presentationOwner);

        var platformFlags = serializedPresentation.FindProperty("platformFlags");
        var names = Enum.GetNames(typeof(PlatformFlag));
        platformFlags.intValue = EditorGUILayout.MaskField("Platforms", platformFlags.intValue, names );

        var variation = serializedPresentation.FindProperty("variation");
        EditorGUILayout.PropertyField(variation);
        
        serializedPresentation.ApplyModifiedProperties();
    }
}
#endif