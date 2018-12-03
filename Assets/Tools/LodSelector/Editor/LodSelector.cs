using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[InitializeOnLoad]
public class LodSelector
{
    static bool enabled = false;

    static LodSelector()
    {
        EditorApplication.update -= CheckSelected;
        EditorApplication.update += CheckSelected;
    }

    [MenuItem("FPS Sample/Hotkeys/LodSelector &L")]
    static void ToggleLodSelector()
    {
        enabled = !enabled;
        Debug.Log("Lod selection hackery is: " + (enabled ? "enabled" : "disabled"));
    }

    private static void CheckSelected()
    {
        if (!enabled)
            return;

        if (Selection.transforms.Length != 1)
            return;

        var t = Selection.transforms[0];
        if (t.parent != null && t.parent.GetComponent<LODGroup>() != null)
        {
            Selection.activeGameObject = t.parent.gameObject;
        }
    }
}