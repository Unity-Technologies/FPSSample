using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[InitializeOnLoad]
public static class SkeletonDrawer
{       
    static List<Skeleton> s_SkeletonComponents = new List<Skeleton>();
    
    static SkeletonDrawer()
    {        
        Skeleton.SkeletonEnabled += OnSkeletonEnabled;
        Skeleton.SkeletonDisabled += OnSkeletonDisabled;
        SceneView.onSceneGUIDelegate += DrawSkeletons;
    }

    static void DrawSkeletons(SceneView sceneview)
    {
        var gizmoColor = Gizmos.color;
        
        for (var i = 0; i < s_SkeletonComponents.Count; i++)
        {
            var skeleton = s_SkeletonComponents[i];
            var size = skeleton.boneSize * 0.025f;

            if (skeleton.drawSkeleton)
            {
                var color = skeleton.skeletonColor;
                var nubColor = new Color(color.r, color.g, color.b, color.a);
                var selectionColor = Color.white;
                
                for (var j = 0; j < skeleton.bones.Length; j++)
                {
                    var bone = skeleton.bones[j].transform;
                    Handles.color = color;              

                    if (bone.parent)
                        Handles.DrawLine(bone.position, bone.parent.position);

                    if (Selection.activeGameObject == bone.gameObject)
                        Handles.color = selectionColor;
                
                    if (bone.childCount > 0)
                    {
                        if (Handles.Button(bone.position, bone.rotation, size, size, Handles.SphereHandleCap))
                        {
                            Selection.activeGameObject = bone.gameObject;                    
                        }                    
                    }

                    else
                    {
                        Handles.color = nubColor; 
                        if (Handles.Button(bone.position, bone.rotation, size * 0.666f, size * 0.333f, Handles.SphereHandleCap))
                        {
                            Selection.activeGameObject = bone.gameObject;                    
                        }    
                    }        
                }              
            }

            if (skeleton.drawTripods)
            {
                for (var j = 0; j < skeleton.bones.Length; j++)
                {               
                    var tripodSize = 1f;
                    var bone = skeleton.bones[j].transform;

                    var position = bone.position;
                    var xAxis = position + bone.rotation * Vector3.left * size * tripodSize;
                    var yAxis = position + bone.rotation * Vector3.up * size * tripodSize;
                    var zAxis = position + bone.rotation * Vector3.forward * size * tripodSize;

                    Handles.color = Color.red;
                    Handles.DrawLine(position, xAxis);
                    Handles.color = Color.green;
                    Handles.DrawLine(position, yAxis);
                    Handles.color = Color.blue;
                    Handles.DrawLine(position, zAxis);
                }
            }
            
        }
        
        Gizmos.color = gizmoColor;
     }

    static void OnSkeletonEnabled(Skeleton obj)
    {
        s_SkeletonComponents.Add(obj);
    }
    
    static void OnSkeletonDisabled(Skeleton obj)
    {
        s_SkeletonComponents.Remove(obj);
    }
}
