using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(Cone))]
    class VFXConeGizmo : VFXSpaceableGizmo<Cone>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_Radius0Property;
        IProperty<float> m_Radius1Property;
        IProperty<float> m_HeightProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_Radius0Property = context.RegisterProperty<float>("radius0");
            m_Radius1Property = context.RegisterProperty<float>("radius1");
            m_HeightProperty = context.RegisterProperty<float>("height");
        }

        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };

        float radius1Screen;
        float radius0Screen;
        bool  m_Dragging;


        public struct Extremities
        {
            public void Build(Cone cone)
            {
                topCap = cone.height * Vector3.up;
                bottomCap = Vector3.zero;

                if (extremities == null)
                    extremities = new List<Vector3>(8);
                extremities.Clear();

                extremities.Add(topCap + Vector3.forward * cone.radius1);
                extremities.Add(topCap - Vector3.forward * cone.radius1);

                extremities.Add(topCap + Vector3.left * cone.radius1);
                extremities.Add(topCap - Vector3.left * cone.radius1);

                extremities.Add(bottomCap + Vector3.forward * cone.radius0);
                extremities.Add(bottomCap - Vector3.forward * cone.radius0);

                extremities.Add(bottomCap + Vector3.left * cone.radius0);
                extremities.Add(bottomCap - Vector3.left * cone.radius0);

                visibleCount = 4;
            }

            public void Build(Cone cone, float degArc)
            {
                topCap = cone.height * Vector3.up;
                bottomCap = Vector3.zero;
                int count = 4;

                visibleCount = Mathf.CeilToInt(degArc / 90);
                if (visibleCount <= 0)
                {
                    visibleCount = 1;
                }

                if (extremities == null)
                    extremities = new List<Vector3>(8);
                extremities.Clear();

                extremities.Add(topCap + Vector3.forward * cone.radius1);
                if (count > 1)
                {
                    extremities.Add(topCap - Vector3.left * cone.radius1);
                    if (count > 2)
                    {
                        extremities.Add(topCap - Vector3.forward * cone.radius1);
                        if (count > 3)
                        {
                            extremities.Add(topCap + Vector3.left * cone.radius1);
                        }
                    }
                }
                extremities.Add(bottomCap + Vector3.forward * cone.radius0);
                if (count > 1)
                {
                    extremities.Add(bottomCap - Vector3.left * cone.radius0);
                    if (count > 2)
                    {
                        extremities.Add(bottomCap - Vector3.forward * cone.radius0);
                        if (count > 3)
                        {
                            extremities.Add(bottomCap + Vector3.left * cone.radius0);
                        }
                    }
                }
            }

            public Vector3 topCap;
            public Vector3 bottomCap;
            public List<Vector3> extremities;
            public int visibleCount;
        }


        public static void DrawCone(Cone cone, VFXGizmo gizmo, ref Extremities extremities, IProperty<Vector3> centerProperty, IProperty<float> radius0Property, IProperty<float> radius1Property, IProperty<float> heightProperty, float radius0Screen, float radius1Screen)
        {
            gizmo.PositionGizmo(cone.center, centerProperty, true);

            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Translate(cone.center)))
            {
                if (radius0Screen > 2 && radius0Property.isEditable)
                {
                    for (int i = extremities.extremities.Count / 2; i < extremities.extremities.Count; ++i)
                    {
                        EditorGUI.BeginChangeCheck();

                        Vector3 pos = extremities.extremities[i];
                        Vector3 result = Handles.Slider(pos, pos - extremities.bottomCap, (i - extremities.extremities.Count / 2) < extremities.visibleCount ? handleSize * HandleUtility.GetHandleSize(pos) : 0, Handles.CubeHandleCap, 0);

                        if (EditorGUI.EndChangeCheck())
                        {
                            radius0Property.SetValue(result.magnitude);
                        }
                    }
                }

                if (radius1Screen > 2 && radius1Property.isEditable)
                {
                    for (int i = 0; i < extremities.extremities.Count / 2; ++i)
                    {
                        EditorGUI.BeginChangeCheck();

                        Vector3 pos = extremities.extremities[i];
                        Vector3 dir = pos - extremities.topCap;
                        Vector3 result = Handles.Slider(pos, dir, i < extremities.visibleCount ? handleSize * HandleUtility.GetHandleSize(pos) : 0, Handles.CubeHandleCap, 0);

                        if (EditorGUI.EndChangeCheck())
                        {
                            radius1Property.SetValue((result - extremities.topCap).magnitude);
                        }
                    }
                }

                if (heightProperty.isEditable)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 result = Handles.Slider(extremities.topCap, Vector3.up, handleSize * HandleUtility.GetHandleSize(extremities.topCap), Handles.CubeHandleCap, 0);

                    if (EditorGUI.EndChangeCheck())
                    {
                        heightProperty.SetValue(result.magnitude);
                    }
                }
            }
        }

        Extremities extremities;

        public override void OnDrawSpacedGizmo(Cone cone)
        {
            extremities.Build(cone);

            if (Event.current != null && Event.current.type == EventType.MouseDown)
            {
                m_Dragging = true;
            }
            if (Event.current != null && Event.current.type == EventType.MouseUp)
            {
                m_Dragging = false;
            }

            if (!m_Dragging)
            {
                radius1Screen = (HandleUtility.WorldToGUIPoint(extremities.topCap) - HandleUtility.WorldToGUIPoint(extremities.topCap + Vector3.forward * cone.radius1)).magnitude;
                radius0Screen = (HandleUtility.WorldToGUIPoint(extremities.bottomCap) - HandleUtility.WorldToGUIPoint(extremities.bottomCap + Vector3.forward * cone.radius0)).magnitude;
            }

            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Translate(cone.center)))
            {
                Handles.DrawWireDisc(extremities.topCap, Vector3.up, cone.radius1);
                Handles.DrawWireDisc(extremities.bottomCap, Vector3.up, cone.radius0);

                for (int i = 0; i < extremities.extremities.Count / 2; ++i)
                {
                    Handles.DrawLine(extremities.extremities[i], extremities.extremities[i + extremities.extremities.Count / 2]);
                }
            }

            DrawCone(cone, this, ref extremities, m_CenterProperty, m_Radius0Property, m_Radius1Property, m_HeightProperty, radius0Screen, radius1Screen);
        }

        public override Bounds OnGetSpacedGizmoBounds(Cone value)
        {
            return new Bounds(value.center, new Vector3(Mathf.Max(value.radius0, value.radius1), Mathf.Max(value.radius0, value.radius1), value.height)); //TODO take orientation in account
        }
    }
    [VFXGizmo(typeof(ArcCone))]
    class VFXArcConeGizmo : VFXSpaceableGizmo<ArcCone>
    {
        IProperty<Vector3> m_CenterProperty;
        IProperty<float> m_Radius0Property;
        IProperty<float> m_Radius1Property;
        IProperty<float> m_HeightProperty;
        IProperty<float> m_ArcProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_CenterProperty = context.RegisterProperty<Vector3>("center");
            m_Radius0Property = context.RegisterProperty<float>("radius0");
            m_Radius1Property = context.RegisterProperty<float>("radius1");
            m_HeightProperty = context.RegisterProperty<float>("height");
            m_ArcProperty = context.RegisterProperty<float>("arc");
        }

        public static readonly Vector3[] radiusDirections = new Vector3[] { Vector3.left, Vector3.up, Vector3.right, Vector3.down };

        VFXConeGizmo.Extremities extremities;

        bool m_Dragging;
        float radius1Screen;
        float radius0Screen;
        public override void OnDrawSpacedGizmo(ArcCone arcCone)
        {
            float arc = arcCone.arc * Mathf.Rad2Deg;
            Cone cone = new Cone { center = arcCone.center, radius0 = arcCone.radius0, radius1 = arcCone.radius1, height = arcCone.height };
            extremities.Build(cone, arc);
            Vector3 arcDirection = Quaternion.AngleAxis(arc, Vector3.up) * Vector3.forward;
            if (Event.current != null && Event.current.type == EventType.MouseDown)
            {
                m_Dragging = true;
            }
            if (Event.current != null && Event.current.type == EventType.MouseUp)
            {
                m_Dragging = false;
            }

            if (!m_Dragging)
            {
                radius1Screen = (HandleUtility.WorldToGUIPoint(extremities.topCap) - HandleUtility.WorldToGUIPoint(extremities.topCap + Vector3.forward * cone.radius1)).magnitude;
                radius0Screen = (HandleUtility.WorldToGUIPoint(extremities.bottomCap) - HandleUtility.WorldToGUIPoint(extremities.bottomCap + Vector3.forward * cone.radius0)).magnitude;
            }

            using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.Translate(arcCone.center)))
            {
                if (radius1Screen > 2)
                    Handles.DrawWireArc(extremities.topCap, Vector3.up, Vector3.forward, arc, arcCone.radius1);

                if (radius0Screen > 2)
                    Handles.DrawWireArc(extremities.bottomCap, Vector3.up, Vector3.forward, arc, arcCone.radius0);

                for (int i = 0; i < extremities.extremities.Count / 2 && i < extremities.visibleCount; ++i)
                {
                    Handles.DrawLine(extremities.extremities[i], extremities.extremities[i + extremities.extremities.Count / 2]);
                }

                Handles.DrawLine(extremities.topCap, extremities.extremities[0]);
                Handles.DrawLine(extremities.bottomCap, extremities.extremities[extremities.extremities.Count / 2]);


                Handles.DrawLine(extremities.topCap, extremities.topCap + arcDirection * arcCone.radius1);
                Handles.DrawLine(extremities.bottomCap, arcDirection * arcCone.radius0);

                Handles.DrawLine(arcDirection * arcCone.radius0, extremities.topCap + arcDirection * arcCone.radius1);
                float radius = arcCone.radius0 > arcCone.radius1 ? arcCone.radius0 : arcCone.radius1;
                Vector3 center = arcCone.radius0 > arcCone.radius1 ? Vector3.zero : extremities.topCap;

                if (radius != 0)
                    ArcGizmo(center, radius, arc, m_ArcProperty, Quaternion.identity, true);
            }

            VFXConeGizmo.DrawCone(cone, this, ref extremities, m_CenterProperty, m_Radius0Property, m_Radius1Property, m_HeightProperty, radius0Screen, radius1Screen);
        }

        public override Bounds OnGetSpacedGizmoBounds(ArcCone value)
        {
            return new Bounds(value.center, new Vector3(Mathf.Max(value.radius0, value.radius1), Mathf.Max(value.radius0, value.radius1), value.height)); //TODO take orientation in account
        }
    }
}
