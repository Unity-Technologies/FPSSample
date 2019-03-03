using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(OrientedBox))]
    class VFXOrientedBoxGizmo : VFXSpaceableGizmo<OrientedBox>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_SizeXProperty;
        IProperty<float> m_SizeYProperty;
        IProperty<float> m_SizeZProperty;
        IProperty<Vector3> m_AnglesProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_SizeXProperty = context.RegisterProperty<float>("size.x");
            m_SizeYProperty = context.RegisterProperty<float>("size.y");
            m_SizeZProperty = context.RegisterProperty<float>("size.z");
            m_AnglesProperty = context.RegisterProperty<Vector3>("angles");
        }

        public override void OnDrawSpacedGizmo(OrientedBox box)
        {
            Matrix4x4 rotate = Matrix4x4.Rotate(Quaternion.Euler(box.angles));
            Matrix4x4 fullTranform = Matrix4x4.Translate(box.center) * rotate * Matrix4x4.Translate(-box.center);

            VFXAABoxGizmo.DrawBoxSizeDataAnchorGizmo(new AABox() {center = box.center, size = box.size}, component, this, m_CenterProperty, m_SizeXProperty, m_SizeYProperty, m_SizeZProperty, fullTranform);

            RotationGizmo(box.center, box.angles, m_AnglesProperty, true);
        }

        public override Bounds OnGetSpacedGizmoBounds(OrientedBox value)
        {
            return new Bounds(value.center, value.size); //TODO take orientation in account
        }
    }

    [VFXGizmo(typeof(AABox))]
    class VFXAABoxGizmo : VFXSpaceableGizmo<AABox>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_SizeXProperty;
        IProperty<float> m_SizeYProperty;
        IProperty<float> m_SizeZProperty;
        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_SizeXProperty = context.RegisterProperty<float>("size.x");
            m_SizeYProperty = context.RegisterProperty<float>("size.y");
            m_SizeZProperty = context.RegisterProperty<float>("size.z");
        }

        public override void OnDrawSpacedGizmo(AABox box)
        {
            DrawBoxSizeDataAnchorGizmo(box, component, this, m_CenterProperty, m_SizeXProperty, m_SizeYProperty, m_SizeZProperty, Matrix4x4.identity);
        }

        static bool TwoSidedSizeHandle(Color color, Vector3 otherMiddle, Vector3 middle, Vector3 center, IProperty<float> sizeProperty, IProperty<Vector3> centerProperty)
        {
            bool result = false;
            var savedColor = Handles.color;
            Handles.color = color;
            if (sizeProperty.isEditable)
            {
                result = SizeHandle(otherMiddle, middle, center, sizeProperty, centerProperty);
                result = SizeHandle(middle, otherMiddle, center, sizeProperty, centerProperty) || result;
            }
            Handles.color = savedColor;
            return result;
        }

        static bool SizeHandle(Vector3 otherMiddle, Vector3 middle, Vector3 center, IProperty<float> sizeProperty, IProperty<Vector3> centerProperty)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 middleResult = Handles.Slider(middle, (middle - center), handleSize * HandleUtility.GetHandleSize(middle), Handles.CubeHandleCap, 0);

            if (EditorGUI.EndChangeCheck())
            {
                sizeProperty.SetValue((middleResult - otherMiddle).magnitude);
                if (centerProperty.isEditable)
                {
                    centerProperty.SetValue((middleResult + otherMiddle) * 0.5f);
                }

                return true;
            }
            return false;
        }

        public static bool DrawBoxSizeDataAnchorGizmo(AABox box,
            VisualEffect component,
            VFXGizmo gizmo,
            IProperty<Vector3> centerProperty,
            IProperty<float> sizeXProperty,
            IProperty<float> sizeYProperty,
            IProperty<float> sizeZProperty,
            Matrix4x4 centerMatrix)
        {
            Vector3[] points = new Vector3[8];

            Vector3 center = box.center;
            Vector3 size = box.size;

            points[0] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[1] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[2] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
            points[3] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, size.z * 0.5f);

            points[4] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[5] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);

            points[6] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, -size.z * 0.5f);
            points[7] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, -size.z * 0.5f);


            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = centerMatrix.MultiplyPoint(points[i]);
            }

            Handles.DrawLine(points[0], points[1]);
            Handles.DrawLine(points[2], points[3]);
            Handles.DrawLine(points[4], points[5]);
            Handles.DrawLine(points[6], points[7]);

            Handles.DrawLine(points[0], points[2]);
            Handles.DrawLine(points[0], points[4]);
            Handles.DrawLine(points[1], points[3]);
            Handles.DrawLine(points[1], points[5]);

            Handles.DrawLine(points[2], points[6]);
            Handles.DrawLine(points[3], points[7]);
            Handles.DrawLine(points[4], points[6]);
            Handles.DrawLine(points[5], points[7]);

            bool changed = false;

            Vector3 xFaceMiddle = (points[0] + points[1] + points[4] + points[5]) * 0.25f;
            Vector3 minusXFaceMiddle = (points[2] + points[3] + points[6] + points[7]) * 0.25f;
            changed = TwoSidedSizeHandle(Color.red, xFaceMiddle, minusXFaceMiddle, center, sizeXProperty, centerProperty);


            Vector3 yFaceMiddle = (points[0] + points[2] + points[4] + points[6]) * 0.25f;
            Vector3 minusYFaceMiddle = (points[1] + points[3] + points[5] + points[7]) * 0.25f;
            changed = TwoSidedSizeHandle(Color.green, yFaceMiddle, minusYFaceMiddle, center, sizeYProperty, centerProperty) || changed;

            Vector3 zFaceMiddle = (points[0] + points[1] + points[2] + points[3]) * 0.25f;
            Vector3 minusZFaceMiddle = (points[4] + points[5] + points[6] + points[7]) * 0.25f;
            changed = TwoSidedSizeHandle(Color.blue, zFaceMiddle, minusZFaceMiddle, center, sizeZProperty, centerProperty) || changed;


            changed = gizmo.PositionGizmo(box.center, centerProperty, true) || changed;

            return changed;
        }

        public override Bounds OnGetSpacedGizmoBounds(AABox value)
        {
            return new Bounds(value.center, value.size);
        }
    }
}
