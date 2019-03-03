using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX.Utils;
using UnityEditor;

[CustomEditor(typeof(VisualEffectInitialState)), CanEditMultipleObjects]
public class VisualEffectInitialStateEditor : Editor
{
    SerializedProperty m_DefaultState;
    SerializedProperty m_CustomEventName;

    private void OnEnable()
    {
        m_DefaultState = serializedObject.FindProperty("defaultState");
        m_CustomEventName = serializedObject.FindProperty("customEventName");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(m_DefaultState);

        if (m_DefaultState.intValue == 2)
            EditorGUILayout.PropertyField(m_CustomEventName);

        serializedObject.ApplyModifiedProperties();
    }

    
}
