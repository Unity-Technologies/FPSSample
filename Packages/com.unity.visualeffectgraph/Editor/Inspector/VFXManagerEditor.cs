using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Rendering;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using UnityObject = UnityEngine.Object;

[CustomEditor(typeof(UnityEditor.VFXManager))]
public class VFXManagerEditor : Editor
{
    void OnEnable()
    {
        CheckVFXManager();
    }

    void OnDisable()
    {
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var pathProperty = serializedObject.FindProperty("m_RenderPipeSettingsPath");
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(pathProperty.name));
        string resultPath = GUILayout.TextArea(pathProperty.stringValue, 500, GUILayout.Height(30));
        if (EditorGUI.EndChangeCheck())
        {
            pathProperty.stringValue = resultPath;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Default"))
        {
            pathProperty.stringValue = "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP";
        }
        if (GUILayout.Button("Reveal"))
        {
            EditorUtility.RevealInFinder(resultPath);
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(15);

        foreach (var propertyName in new string[] { "m_FixedTimeStep", "m_MaxDeltaTime" })
        {
            var property = serializedObject.FindProperty(propertyName);
            EditorGUILayout.PropertyField(property);
        }

        GUILayout.Space(15);

        foreach (var propertyName in new string[] { "m_IndirectShader", "m_CopyBufferShader", "m_SortShader" })
        {
            var property = serializedObject.FindProperty(propertyName);
            EditorGUILayout.PropertyField(property);
        }
        serializedObject.ApplyModifiedProperties();
    }

    public static void CheckVFXManager()
    {
        UnityObject vfxmanager = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/VFXManager.asset");
        if (vfxmanager == null)
            return;

        SerializedObject obj = new SerializedObject(vfxmanager);

        var pathProperty = obj.FindProperty("m_RenderPipeSettingsPath");
        if (string.IsNullOrEmpty(pathProperty.stringValue))
        {
            pathProperty.stringValue = "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP";
        }

        var indirectShaderProperty = obj.FindProperty("m_IndirectShader");
        if (indirectShaderProperty.objectReferenceValue == null)
        {
            indirectShaderProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.visualeffectgraph/Shaders/VFXFillIndirectArgs.compute");
        }
        var copyShaderProperty = obj.FindProperty("m_CopyBufferShader");
        if (copyShaderProperty.objectReferenceValue == null)
        {
            copyShaderProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.visualeffectgraph/Shaders/VFXCopyBuffer.compute");
        }
        var sortProperty = obj.FindProperty("m_SortShader");
        if (sortProperty.objectReferenceValue == null)
        {
            sortProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.visualeffectgraph/Shaders/Sort.compute");
        }

        obj.ApplyModifiedPropertiesWithoutUndo();
    }
}
