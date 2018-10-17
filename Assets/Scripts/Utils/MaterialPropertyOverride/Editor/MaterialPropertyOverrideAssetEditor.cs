using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MaterialPropertyOverrideAsset))]
public class MaterialPropertyOverrideAssetEditor : Editor
{
    private bool m_ShowAll = false;

    public override void OnInspectorGUI()
    {
        var myMatProps = target as MaterialPropertyOverrideAsset;

        EditorGUILayout.Space();

        var headStyle = new GUIStyle("ShurikenModuleTitle");
        headStyle.fixedHeight = 20.0f;
        headStyle.contentOffset = new Vector2(5, -2);
        headStyle.font = EditorStyles.boldFont;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("shader"));

        serializedObject.ApplyModifiedProperties();

        if (myMatProps.shader == null)
        {
            EditorGUILayout.HelpBox("No shader selected!. This asset type needs a shader to make sense.", MessageType.Error);
            return;
        }

        // Draw properties header
        EditorGUILayout.Space();
        var re = GUILayoutUtility.GetRect(16f, 22f, headStyle);
        GUI.Box(re, "Properties:", headStyle);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        m_ShowAll = GUILayout.Toggle(m_ShowAll, "Show all", "Button");
        GUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // Draw property override GUI
        var changed = MatPropsOverrideEditor.DrawOverrideGUI(myMatProps.shader, myMatProps.propertyOverrides, m_ShowAll, myMatProps);

        // Draw button to select all
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Select all affected objects in scene", GUILayout.Height(30), GUILayout.Width(250)))
        {
            Selection.activeObject = null;
            var objs = new List<GameObject>();
            foreach (var mpo in GameObject.FindObjectsOfType<MaterialPropertyOverride>())
            {
                foreach (var o in mpo.materialOverrides)
                {
                    if (o.propertyOverrideAsset == myMatProps)
                    {
                        objs.Add(mpo.gameObject);
                        break;
                    }
                }
            }
            Selection.objects = objs.ToArray();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (changed)
        {
            // Refresh all objects in scene that uses our override.
            foreach (var mpo in GameObject.FindObjectsOfType<MaterialPropertyOverride>())
            {
                foreach (var o in mpo.materialOverrides)
                {
                    if (o.propertyOverrideAsset == myMatProps)
                    {
                        mpo.Clear();
                        mpo.Apply();
                        break;
                    }
                }
            }
            SceneView.RepaintAll();
        }

        if (changed)
            EditorUtility.SetDirty(myMatProps);
    }
}

