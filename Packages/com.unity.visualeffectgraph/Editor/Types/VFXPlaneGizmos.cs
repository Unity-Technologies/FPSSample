using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(Plane))]
    class VFXPlaneGizmo : VFXSpaceableGizmo<Plane>
    {
        IProperty<Vector3> m_PositionProperty;
        IProperty<Vector3> m_NormalProperty;
        public override void RegisterEditableMembers(IContext context)
        {
            m_PositionProperty = context.RegisterProperty<Vector3>("position");
            m_NormalProperty = context.RegisterProperty<Vector3>("normal");
        }

        public override void OnDrawSpacedGizmo(Plane plane)
        {
            Vector3 normal = plane.normal.normalized;
            if (normal == Vector3.zero)
            {
                normal = Vector3.up;
            }

            var normalQuat = Quaternion.FromToRotation(Vector3.forward, normal);

            float size = 10;

            Vector3[] points = new Vector3[]
            {
                new Vector3(size, size, 0),
                new Vector3(size, -size, 0),
                new Vector3(-size, -size, 0),
                new Vector3(-size, size, 0),
                new Vector3(size, size, 0),
            };

            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Translate(plane.position) * Matrix4x4.Rotate(normalQuat)))
            {
                Handles.DrawPolyLine(points);
            }
            Handles.ArrowHandleCap(0, plane.position, normalQuat, 5, Event.current.type);

            PositionGizmo(plane.position, m_PositionProperty, false);

            if (m_NormalProperty.isEditable && NormalGizmo(plane.position, ref normal, false))
            {
                normal.Normalize();
                m_NormalProperty.SetValue(normal);
            }
        }

        public override Bounds OnGetSpacedGizmoBounds(Plane value)
        {
            return new Bounds(value.position, Vector3.one);
        }
    }
}
