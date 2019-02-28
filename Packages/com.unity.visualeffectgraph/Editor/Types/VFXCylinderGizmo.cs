using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(Cylinder))]
    class VFXCylinderGizmo : VFXSpaceableGizmo<Cylinder>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_RadiusProperty;
        IProperty<float> m_HeightProperty;
        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_RadiusProperty = context.RegisterProperty<float>("radius");
            m_HeightProperty = context.RegisterProperty<float>("height");
        }

        public override void OnDrawSpacedGizmo(Cylinder cylinder)
        {
            Vector3 topCap = cylinder.height * 0.5f * Vector3.up;
            Vector3 bottomCap = -cylinder.height * 0.5f * Vector3.up;

            Vector3[] extremities = new Vector3[8];

            extremities[0] = topCap + Vector3.forward * cylinder.radius;
            extremities[1] = topCap - Vector3.forward * cylinder.radius;

            extremities[2] = topCap + Vector3.left * cylinder.radius;
            extremities[3] = topCap - Vector3.left * cylinder.radius;

            extremities[4] = bottomCap + Vector3.forward * cylinder.radius;
            extremities[5] = bottomCap - Vector3.forward * cylinder.radius;

            extremities[6] = bottomCap + Vector3.left * cylinder.radius;
            extremities[7] = bottomCap - Vector3.left * cylinder.radius;


            PositionGizmo(cylinder.center, m_CenterProperty, true);

            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Translate(cylinder.center)))
            {
                for (int i = 0; i < extremities.Length / 2; ++i)
                {
                    Handles.DrawLine(extremities[i], extremities[i + extremities.Length / 2]);
                }
                Handles.DrawWireDisc(topCap, Vector3.up, cylinder.radius);
                Handles.DrawWireDisc(bottomCap, Vector3.up, cylinder.radius);

                if (m_RadiusProperty.isEditable)
                {
                    Vector3 result;
                    for (int i = 0; i < extremities.Length / 2; ++i)
                    {
                        EditorGUI.BeginChangeCheck();

                        Vector3 pos = (extremities[i] + extremities[i + +extremities.Length / 2]) * 0.5f;
                        result = Handles.Slider(pos, pos, handleSize * HandleUtility.GetHandleSize(pos), Handles.CubeHandleCap, 0);

                        if (EditorGUI.EndChangeCheck())
                        {
                            m_RadiusProperty.SetValue(result.magnitude);
                        }
                    }
                }

                if (m_HeightProperty.isEditable)
                {
                    foreach (var cap in new Vector3[] { topCap, bottomCap })
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 result = Handles.Slider(cap, Vector3.up, handleSize * HandleUtility.GetHandleSize(topCap), Handles.CubeHandleCap, 0);

                        if (EditorGUI.EndChangeCheck())
                        {
                            m_HeightProperty.SetValue(result.magnitude * 2);
                        }
                    }
                }
            }
        }

        public override Bounds OnGetSpacedGizmoBounds(Cylinder value)
        {
            return new Bounds(value.center, new Vector3(value.radius, value.radius, value.height));
        }
    }
}
