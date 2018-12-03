using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class PlacementTools
{
    static PlacementTools()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }
    
    static Ray lastMouseRay;
    static void OnSceneGUI(SceneView sceneview)
    {
        lastMouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
    }

    [MenuItem("FPS Sample/Hotkeys/Position under mouse %#q")]
    static void MousePlace()
    {
        var transforms = Selection.transforms;
        foreach (var myTransform in transforms)
        {
            Undo.RegisterCompleteObjectUndo(Selection.transforms, "MousePlace");
            var hit = FindClosestObjectUnderMouseNotSelected();
            myTransform.position = hit.point;
        }
    }

    static RaycastHit FindClosestObjectUnderMouseNotSelected()
    {
        var transforms = Selection.transforms;
        // Find closest object under mouse, not selected
        var hits = Physics.RaycastAll(lastMouseRay);
        RaycastHit hit = new RaycastHit();
        var closest_dist = float.MaxValue;
        foreach (var h in hits)
        {
            var skipit = false;
            foreach (var t in transforms)
            {
                if (h.collider.transform.IsChildOf(t))
                    skipit = true;
            }
            if (skipit)
                continue;
            if (h.distance < closest_dist)
            {
                hit = h;
                closest_dist = h.distance;
            }
        }
        return hit;
    }

    [MenuItem("FPS Sample/Hotkeys/Align and position under mouse %#z")]
    static void MousePlaceAndAlign()
    {
        var transforms = Selection.transforms;
        if (transforms.Length == 0)
            return;

        var hit = FindClosestObjectUnderMouseNotSelected();

        if (hit.distance == 0)
            return;

        foreach (var myTransform in transforms)
        {
            Undo.RegisterCompleteObjectUndo(Selection.transforms, "MousePlaceAndAlign");

            myTransform.position = hit.point;

            // Decide what is most up
            var xdot = Vector3.Dot(myTransform.right, Vector3.up);
            var ydot = Vector3.Dot(myTransform.up, Vector3.up);
            if (Mathf.Abs(xdot) > 0.7f)
            {
                var rot = Quaternion.FromToRotation(myTransform.right, hit.normal);
                myTransform.rotation = rot * myTransform.rotation;
            }
            else if (Mathf.Abs(ydot) > 0.7f)
            {
                var rot = Quaternion.FromToRotation(myTransform.up, hit.normal);
                myTransform.rotation = rot * myTransform.rotation;
            }
            else
            {
                var rot = Quaternion.FromToRotation(myTransform.forward, hit.normal);
                myTransform.rotation = rot * myTransform.rotation;
            }
        }
    }
}
