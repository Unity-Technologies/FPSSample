using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class PlanarReflectionProbeUI
    {
        static readonly Color k_GizmoMirrorPlaneCamera = new Color(128f / 255f, 128f / 255f, 233f / 255f, 128f / 255f);

        internal static void DrawHandlesOverride(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            //Note: HDProbeUI.DrawHandles is called in parent 
            PlanarReflectionProbe probe = d.target;

            switch (EditMode.editMode)
            {
                case EditBaseShape:
                    if ((InfluenceShape)d.influenceVolume.shape.intValue != InfluenceShape.Box)
                        return;

                    //override base handle behavior to also translate object along x and z axis and offset the y axis
                    using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, d.target.transform.rotation, Vector3.one)))
                    {
                        Vector3 origin = Quaternion.Inverse(probe.transform.rotation) * probe.transform.position;
                        s.influenceVolume.boxBaseHandle.center = origin + probe.influenceVolume.offset;
                        s.influenceVolume.boxBaseHandle.size = probe.influenceVolume.boxSize;

                        EditorGUI.BeginChangeCheck();
                        s.influenceVolume.boxBaseHandle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new Object[] { probe, probe.transform }, "Modified Planar Base Volume AABB");

                            Vector3 size = s.influenceVolume.boxBaseHandle.size;

                            //clamp offset value
                            float extend = probe.influenceVolume.boxSize.y * 0.5f;
                            float offset = s.influenceVolume.boxBaseHandle.center.y - origin.y;
                            if(offset < -extend)
                            {
                                size.y += 2f * (-offset - extend);
                                offset = -extend;
                            }
                            if (offset > extend)
                            {
                                size.y += 2f * (offset - extend);
                                offset = extend;
                            }
                            if(size.y < 0)
                            {
                                size.y = 0;
                            }
                            probe.influenceVolume.boxSize = size;
                            probe.influenceVolume.offset = new Vector3(0, offset, 0);
                            Vector3 centerXZ = s.influenceVolume.boxBaseHandle.center;
                            centerXZ.y = origin.y;
                            Vector3 deltaXZ = probe.transform.rotation * centerXZ - probe.transform.position;
                            probe.transform.position += deltaXZ;
                        }
                    }
                    break;
                case EditCenter:
                    {
                        if ((InfluenceShape)d.influenceVolume.shape.intValue != InfluenceShape.Box)
                            break;

                        //override base handle behavior to only translate object along y
                        using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one)))
                        {
                            EditorGUI.BeginChangeCheck();
                            var newCapturePosition = Handles.Slider(
                                probe.transform.position,
                                probe.transform.rotation * Vector3.up,
                                HandleUtility.GetHandleSize(probe.transform.position),
                                Handles.ArrowHandleCap,
                                0f
                                );
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObjects(new Object[] { probe, probe.transform }, "Translate Influence Position");
                                Vector3 delta = newCapturePosition - probe.transform.position;
                                Matrix4x4 oldLocalToWorld = Matrix4x4.TRS(probe.transform.position, probe.transform.rotation, Vector3.one);
                                probe.transform.position = newCapturePosition;
                                Vector3 offset = d.influenceVolume.offset.vector3Value - oldLocalToWorld.inverse.MultiplyVector(delta);

                                //clamp offset value
                                float extend = probe.influenceVolume.boxSize.y * 0.5f;
                                offset.y = Mathf.Clamp(offset.y, -extend, extend);
                                d.influenceVolume.offset.vector3Value = offset;

                                d.influenceVolume.Apply();
                            }
                        }
                        break;
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
            
            var mat = Matrix4x4.TRS(d.transform.position + d.transform.rotation * d.influenceVolume.offset, d.transform.rotation, Vector3.one);

            //gizmo overrides
            switch (EditMode.editMode)
            {
                case EditBaseShape:
                    if (d.influenceVolume.shape != InfluenceShape.Box)
                        break;

                    using (new Handles.DrawingScope(mat))
                    {
                        s.influenceVolume.boxBaseHandle.center = Vector3.zero;
                        s.influenceVolume.boxBaseHandle.size = d.influenceVolume.boxSize;
                        s.influenceVolume.boxBaseHandle.DrawHull(true);
                        s.influenceVolume.boxInfluenceHandle.center = d.influenceVolume.boxBlendOffset;
                        s.influenceVolume.boxInfluenceHandle.size = d.influenceVolume.boxSize + d.influenceVolume.boxBlendSize;
                        s.influenceVolume.boxInfluenceHandle.DrawHull(false);
                        s.influenceVolume.boxInfluenceNormalHandle.center = d.influenceVolume.boxBlendNormalOffset;
                        s.influenceVolume.boxInfluenceNormalHandle.size = d.influenceVolume.boxSize + d.influenceVolume.boxBlendNormalSize;
                        s.influenceVolume.boxInfluenceNormalHandle.DrawHull(false);
                    }
                    break;
            }

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
                    d.transform.position,
                    d.transform.rotation * Quaternion.Euler(90f, 0f, 0f),
                    Vector3.one);
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(d.influenceVolume.boxSize.x, d.influenceVolume.boxSize.z, 0));
            Color c2 = InfluenceVolumeUI.k_GizmoThemeColorInfluenceNormal.linear;
            c2.a = 1f;
            Gizmos.color = c2;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward);
            Gizmos.color = k_GizmoMirrorPlaneCamera;
            Gizmos.DrawCube(Vector3.zero, new Vector3(d.influenceVolume.boxSize.x, d.influenceVolume.boxSize.z, 0));

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
