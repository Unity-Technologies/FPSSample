using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(Line))]
    class VFXLineGizmo : VFXSpaceableGizmo<Line>
    {
        IProperty<Vector3> m_StartProperty;
        IProperty<Vector3> m_EndProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_StartProperty = context.RegisterProperty<Vector3>("start");
            m_EndProperty = context.RegisterProperty<Vector3>("end");
        }

        public override void OnDrawSpacedGizmo(Line line)
        {
            Handles.DrawLine(line.start, line.end);

            PositionGizmo(line.start, m_StartProperty, true);
            PositionGizmo(line.end, m_EndProperty, true);
        }

        public override Bounds OnGetSpacedGizmoBounds(Line value)
        {
            Vector3 center = (value.start + value.end) * 0.5f;

            Vector3 size = value.end - value.start;
            size.x = Mathf.Abs(size.x);
            size.y = Mathf.Abs(size.y);
            size.z = Mathf.Abs(size.z);

            return new Bounds(center, size);
        }
    }
}
