using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(Position))]
    class VFXPositionGizmo : VFXSpaceableGizmo<Position>
    {
        IProperty<Position> m_Property;
        public override void RegisterEditableMembers(IContext context)
        {
            m_Property = context.RegisterProperty<Position>("");
        }

        public override void OnDrawSpacedGizmo(Position position)
        {
            if (m_Property.isEditable && PositionGizmo(ref position.position, true))
            {
                m_Property.SetValue(position);
            }
        }

        public override Bounds OnGetSpacedGizmoBounds(Position value)
        {
            return new Bounds(value.position, Vector3.one);
        }
    }
    [VFXGizmo(typeof(DirectionType))]
    class VFXDirectionGizmo : VFXSpaceableGizmo<DirectionType>
    {
        IProperty<DirectionType> m_Property;
        public override void RegisterEditableMembers(IContext context)
        {
            m_Property = context.RegisterProperty<DirectionType>("");
        }

        public override void OnDrawSpacedGizmo(DirectionType direction)
        {
            direction.direction.Normalize();
            if (direction.direction == Vector3.zero)
            {
                direction.direction = Vector3.up;
            }
            Quaternion normalQuat = Quaternion.FromToRotation(Vector3.forward, direction.direction);
            Handles.ArrowHandleCap(0, Vector3.zero, normalQuat, HandleUtility.GetHandleSize(Vector3.zero) * 1, Event.current.type);

            if (m_Property.isEditable && NormalGizmo(Vector3.zero, ref direction.direction, true))
            {
                direction.direction.Normalize();
                m_Property.SetValue(direction);
            }
        }

        Quaternion m_PrevQuaternion;


        public static void AngleHandleDrawFunction(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            Handles.DrawWireDisc(Vector3.zero, Vector3.forward, size * 10);
            Handles.DrawLine(Vector3.zero, position);
        }

        public override Bounds OnGetSpacedGizmoBounds(DirectionType value)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }
    }
    [VFXGizmo(typeof(Vector))]
    class VFXVectorGizmo : VFXSpaceableGizmo<Vector>
    {
        IProperty<Vector> m_Property;
        public override void RegisterEditableMembers(IContext context)
        {
            m_Property = context.RegisterProperty<Vector>("");
        }

        public override void OnDrawSpacedGizmo(Vector vector)
        {
            if (vector.vector == Vector3.zero)
            {
                vector.vector = Vector3.up;
            }

            Quaternion normalQuat = Quaternion.FromToRotation(Vector3.forward, vector.vector);

            float length = vector.vector.magnitude;

            if (m_Property.isEditable && NormalGizmo(Vector3.zero, ref vector.vector, true))
            {
                m_Property.SetValue(vector);
            }

            if (m_Property.isEditable)
            {
                Handles.DrawLine(Vector3.zero, vector.vector);
                EditorGUI.BeginChangeCheck();
                Vector3 result = Handles.Slider(vector.vector, vector.vector, handleSize * 2 * HandleUtility.GetHandleSize(vector.vector), Handles.ConeHandleCap, 0);
                if (EditorGUI.EndChangeCheck())
                {
                    vector.vector = vector.vector.normalized * result.magnitude;
                    m_Property.SetValue(vector);
                }
            }
            else
            {
                Handles.ArrowHandleCap(0, Vector3.zero, normalQuat, length, Event.current.type);
            }
        }

        public override Bounds OnGetSpacedGizmoBounds(Vector value)
        {
            return new Bounds(Vector3.zero, Vector3.one * value.vector.magnitude * 2);
        }
    }
}
