using System;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Linq.Expressions;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDReflectionProbeEditor
    {
        static Mesh sphere;
        static Material material;


        [DrawGizmo(GizmoType.Selected)]
        static void DrawSelectedGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            var e = GetEditorFor(reflectionProbe);
            if (e == null)
                return;

            if (reflectionProbe == e.target)
            {
                //will draw every preview, thus no need to do it multiple times
                Gizmos_CapturePoint(e);
            }

            if (!e.sceneViewEditing)
                return;

            DrawVerticalRay(reflectionProbe.transform);
        }

        static void DrawVerticalRay(Transform transform)
        {
            Ray ray = new Ray(transform.position, Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Handles.color = Color.green;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.DrawLine(transform.position - Vector3.up * 0.5f, hit.point);
                Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);

                Handles.color = Color.red;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.DrawLine(transform.position, hit.point);
                Handles.DrawWireDisc(hit.point, hit.normal, 0.5f);
            }
        }

        static void Gizmos_CapturePoint(HDReflectionProbeEditor e)
        {
            if(sphere == null)
            {
                sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            }
            if(material == null)
            {
                material = new Material(Shader.Find("Debug/ReflectionProbePreview"));
            }

            foreach (ReflectionProbe target in e.targets)
            {
                material.SetTexture("_Cubemap", GetTexture(e, target));
                material.SetPass(0);
                Graphics.DrawMeshNow(sphere, Matrix4x4.TRS(target.transform.position, Quaternion.identity, Vector3.one * capturePointPreviewSize));
            }
        }

        static Func<float> s_CapturePointPreviewSizeGetter = ComputeCapturePointPreviewSizeGetter();
        static Func<float> ComputeCapturePointPreviewSizeGetter()
        {
            var type = Type.GetType("UnityEditor.AnnotationUtility,UnityEditor");
            var property = type.GetProperty("iconSize", BindingFlags.Static | BindingFlags.NonPublic);
            var lambda = Expression.Lambda<Func<float>>(
                Expression.Multiply(
                    Expression.Property(null, property),
                    Expression.Constant(30.0f)
                )
            );
            return lambda.Compile();
        }

        static float capturePointPreviewSize
        {
            get
            { return s_CapturePointPreviewSizeGetter();
            }
        }
    }
}
