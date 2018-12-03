using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LPPVTool))]
public class LPPVToolEditor : Editor
{

    public override void OnInspectorGUI()
    {

        EditorGUILayout.HelpBox("Use this tool to assign all renderers under this group that are not LightMapStatic to the LPP Volume selected below.", MessageType.Info);
        DrawDefaultInspector();

        var tool = (LPPVTool)target;

        if (tool.Volume == null)
        {
            EditorGUILayout.HelpBox("No proxy volume assigned. This will never work...", MessageType.Warning);
            return;
        }


        var all = FindThemAll(tool);

        var unassigned = all.FindAll(delegate (GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            return r.lightProbeUsage != UnityEngine.Rendering.LightProbeUsage.UseProxyVolume || r.lightProbeProxyVolumeOverride != tool.Volume.gameObject;
        });

        GUILayout.Space(10);
        GUILayout.Label("Unassigned: " + unassigned.Count, unassigned.Count > 0 ? EditorStyles.boldLabel : EditorStyles.label);
        GUILayout.Space(20);

        if (GUILayout.Button("Select all candidates (" + all.Count + ")"))
        {
            Selection.objects = all.ToArray();
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select unassigned candidates (" + unassigned.Count + ")"))
        {
            Selection.objects = unassigned.ToArray();
        }
        if (GUILayout.Button("Assign (" + unassigned.Count + ")"))
        {
            Undo.RegisterCompleteObjectUndo(unassigned.ToArray(), "LPPV Tool");
            foreach(var go in unassigned)
            {
                var r = go.GetComponent<Renderer>();
                r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.UseProxyVolume;
                r.lightProbeProxyVolumeOverride = tool.Volume.gameObject;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(20);
    }

    public void AssignThemAll(LPPVTool tool)
    {
        foreach (var g in FindThemAll(tool))
        {
            var r = g.GetComponent<Renderer>();
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.UseProxyVolume;
            r.lightProbeProxyVolumeOverride = tool.Volume.gameObject;
        }
    }

    public List<GameObject> FindThemAll(LPPVTool tool)
    {
        var result = new List<GameObject>();

        foreach (var r in tool.transform.GetComponentsInChildren<Renderer>())
        {
            if ((GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.LightmapStatic) == StaticEditorFlags.LightmapStatic)
                continue;
            result.Add(r.gameObject);
        }
        return result;
    }
}
