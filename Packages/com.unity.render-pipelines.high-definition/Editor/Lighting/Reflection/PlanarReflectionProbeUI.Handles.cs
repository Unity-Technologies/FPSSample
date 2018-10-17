using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class PlanarReflectionProbeUI
    {
        static readonly Color k_GizmoMirrorPlaneCamera = new Color(128f / 255f, 128f / 255f, 233f / 255f, 128f / 255f);

        internal static void DrawHandles(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            PlanarReflectionProbe probe = d.target;
            HDProbeUI.DrawHandles(s, d, o);

            if (probe.useMirrorPlane)
            {
                var m = Handles.matrix;
                var mat = Matrix4x4.TRS(probe.transform.position, probe.transform.rotation, Vector3.one*1.5f);
                using (new Handles.DrawingScope(k_GizmoMirrorPlaneCamera, mat))
                {
                    Handles.ArrowHandleCap(
                        0,
                        probe.captureMirrorPlaneLocalPosition,
                        Quaternion.LookRotation(probe.captureMirrorPlaneLocalNormal),
                        HandleUtility.GetHandleSize(probe.captureMirrorPlaneLocalPosition),
                        Event.current.type
                        );
                }
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        internal static void DrawGizmos(PlanarReflectionProbe d, GizmoType gizmoType)
        {
            HDProbeUI.DrawGizmos(d, gizmoType);

            HDProbeUI s;
            if (!HDProbeEditor.TryGetUIStateFor(d, out s))
                return;

            if (s.showCaptureHandles || EditMode.editMode == EditCenter)
                DrawGizmos_CaptureFrustrum(d);

            if (d.useMirrorPlane)
                DrawGizmos_CaptureMirror(d);
        }

        static void DrawGizmos_CaptureMirror(PlanarReflectionProbe d)
        {
            var c = Gizmos.color;
            var m = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                    d.captureMirrorPlanePosition,
                    Quaternion.LookRotation(d.captureMirrorPlaneNormal, Vector3.up),
                    Vector3.one);
            Gizmos.color = k_GizmoMirrorPlaneCamera;

            Gizmos.DrawCube(Vector3.zero, new Vector3(1, 1, 0));

            Gizmos.matrix = m;
            Gizmos.color = c;
        }

        static void DrawGizmos_CaptureFrustrum(PlanarReflectionProbe d)
        {
            var viewerCamera = Camera.current;
            var c = Gizmos.color;
            var m = Gizmos.matrix;

            float nearClipPlane, farClipPlane, aspect, fov;
            Color backgroundColor;
            CameraClearFlags clearFlags;
            Vector3 capturePosition;
            Quaternion captureRotation;
            Matrix4x4 worldToCameraRHS, projection;

            ReflectionSystem.CalculateCaptureCameraProperties(d,
                out nearClipPlane, out farClipPlane,
                out aspect, out fov, out clearFlags, out backgroundColor,
                out worldToCameraRHS, out projection,
                out capturePosition, out captureRotation, viewerCamera);

            Gizmos.DrawSphere(capturePosition, HandleUtility.GetHandleSize(capturePosition) * 0.2f);
            Gizmos.color = c;
        }
    }
}
