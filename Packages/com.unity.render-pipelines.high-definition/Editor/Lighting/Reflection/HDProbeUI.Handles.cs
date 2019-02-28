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
                    //important: following will init the container for box.
                    //This must be done before drawing the contained handles
                    if(o is PlanarReflectionProbeEditor && (InfluenceShape)d.influenceVolume.shape.intValue == InfluenceShape.Box)
                    {
                        //Planar need to update its transform position.
                        //Let PlanarReflectionProbeUI.Handle do this work.
                        break;
                    }
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
                        if (o is PlanarReflectionProbeEditor && (InfluenceShape)d.influenceVolume.shape.intValue == InfluenceShape.Box)
                        {
                            //Planar need to update its transform position.
                            //Let PlanarReflectionProbeUI.Handle do this work.
                            break;
                        }
                        using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one)))
                        {
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

        [DrawGizmo(GizmoType.Selected|GizmoType.Active)]
        internal static void DrawGizmos(HDProbe d, GizmoType gizmoType)
        {
            HDProbeUI s;
            if (!HDProbeEditor.TryGetUIStateFor(d, out s))
                return;

            var mat = Matrix4x4.TRS(d.transform.position, d.transform.rotation, Vector3.one);

            switch (EditMode.editMode)
            {
                case EditBaseShape:
                    if (d is PlanarReflectionProbe && (InfluenceShape)d.influenceVolume.shape == InfluenceShape.Box)
                    {
                        //Planar need to update its transform position.
                        //Let PlanarReflectionProbeUI.Handle do this work.
                        break;
                    }
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
                        : InfluenceVolumeUI.HandleType.Base | InfluenceVolumeUI.HandleType.Influence;
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
