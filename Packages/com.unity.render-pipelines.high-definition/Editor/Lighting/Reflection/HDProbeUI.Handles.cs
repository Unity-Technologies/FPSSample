using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI
    {

        internal static void DrawHandles(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            HDProbe probe = d.target as HDProbe;
            var mat = Matrix4x4.TRS(probe.transform.position, probe.transform.rotation, Vector3.one);

            switch (EditMode.editMode)
            {
                case EditBaseShape:
                    InfluenceVolumeUI.DrawHandles_EditBase(s.influenceVolume, d.influenceVolume, o, mat, probe);
                    break;
                case EditInfluenceShape:
                    InfluenceVolumeUI.DrawHandles_EditInfluence(s.influenceVolume, d.influenceVolume, o, mat, probe);
                    break;
                case EditInfluenceNormalShape:
                    InfluenceVolumeUI.DrawHandles_EditInfluenceNormal(s.influenceVolume, d.influenceVolume, o, mat, probe);
                    break;
                case EditCenter:
                    {
                        using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one)))
                        {
                            //Vector3 offsetWorld = probe.transform.position + probe.transform.rotation * probe.influenceVolume.offset;
                            EditorGUI.BeginChangeCheck();
                            var newCapturePosition = Handles.PositionHandle(probe.transform.position, probe.transform.rotation);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Vector3 newOffset = Quaternion.Inverse(probe.transform.rotation) * (newCapturePosition - probe.transform.position);
                                Undo.RecordObjects(new Object[] { probe, probe.transform }, "Translate Influence Position");
                                Vector3 delta = newCapturePosition - probe.transform.position;
                                Matrix4x4 oldLocalToWorld = Matrix4x4.TRS(probe.transform.position, probe.transform.rotation, Vector3.one);

                                //call modification to legacy ReflectionProbe
                                probe.influenceVolume.offset = newOffset;

                                probe.transform.position = newCapturePosition;
                                d.influenceVolume.offset.vector3Value -= oldLocalToWorld.inverse.MultiplyVector(delta);
                                d.influenceVolume.Apply();
                            }
                        }
                        break;
                    }
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        internal static void DrawGizmos(HDProbe d, GizmoType gizmoType)
        {
            HDProbeUI s;
            if (!HDProbeEditor.TryGetUIStateFor(d, out s))
                return;

            var mat = Matrix4x4.TRS(d.transform.position, d.transform.rotation, Vector3.one);

            switch (EditMode.editMode)
            {
                case EditBaseShape:
                    InfluenceVolumeUI.DrawGizmos(
                        s.influenceVolume, d.influenceVolume, mat,
                        InfluenceVolumeUI.HandleType.Base,
                        InfluenceVolumeUI.HandleType.All);
                    break;
                case EditInfluenceShape:
                    InfluenceVolumeUI.DrawGizmos(
                    s.influenceVolume,
                    d.influenceVolume,
                    mat,
                    InfluenceVolumeUI.HandleType.Influence,
                    InfluenceVolumeUI.HandleType.All);
                    break;
                case EditInfluenceNormalShape:
                    InfluenceVolumeUI.DrawGizmos(
                    s.influenceVolume,
                    d.influenceVolume,
                    mat,
                    InfluenceVolumeUI.HandleType.InfluenceNormal,
                    InfluenceVolumeUI.HandleType.All);
                    break;
                default:
                {
                    var showedHandles = s.influenceVolume.showInfluenceHandles
                        ? InfluenceVolumeUI.HandleType.All
                        : InfluenceVolumeUI.HandleType.Base;
                    InfluenceVolumeUI.DrawGizmos(
                        s.influenceVolume,
                        d.influenceVolume,
                        mat,
                        InfluenceVolumeUI.HandleType.None,
                        showedHandles);
                    break;
                }
            }

            if (d.proxyVolume != null)
                ReflectionProxyVolumeComponentUI.DrawGizmos_EditNone(d.proxyVolume);
        }
    }
}
