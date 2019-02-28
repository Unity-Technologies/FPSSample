using System;
using UnityEngine;
using System.Reflection;

namespace UnityEditor.Experimental.Rendering
{
    /// <summary>
    /// Provide a gizmo/handle representing a box where all face can be moved independently.
    /// Also add a contained sub gizmo/handle box if contained is used at creation.
    /// </summary>
    /// <example>
    /// <code>
    /// class MyComponentEditor : Editor
    /// {
    ///     static HierarchicalSphere sphere;
    ///     static HierarchicalSphere containedSphere;
    ///
    ///     static MyComponentEditor()
    ///     {
    ///         Color[] handleColors = new Color[]
    ///         {
    ///             Color.red,
    ///             Color.green,
    ///             Color.Blue,
    ///             new Color(0.5f, 0f, 0f, 1f),
    ///             new Color(0f, 0.5f, 0f, 1f),
    ///             new Color(0f, 0f, 0.5f, 1f)
    ///         };
    ///         sphere = new HierarchicalSphere(new Color(1f, 1f, 1f, 0.25));
    ///         containedSphere = new HierarchicalSphere(new Color(1f, 0f, 1f, 0.25), container: sphere);
    ///     }
    ///
    ///     [DrawGizmo(GizmoType.Selected|GizmoType.Active)]
    ///     void DrawGizmo(MyComponent comp, GizmoType gizmoType)
    ///     {
    ///         sphere.center = comp.transform.position;
    ///         sphere.size = comp.transform.scale;
    ///         sphere.DrawHull(gizmoType == GizmoType.Selected);
    ///         
    ///         containedSphere.center = comp.innerposition;
    ///         containedSphere.size = comp.innerScale;
    ///         containedSphere.DrawHull(gizmoType == GizmoType.Selected);
    ///     }
    ///
    ///     void OnSceneGUI()
    ///     {
    ///         EditorGUI.BeginChangeCheck();
    ///         
    ///         //container sphere must be also set for contained sphere for clamping
    ///         sphere.center = comp.transform.position;
    ///         sphere.size = comp.transform.scale;
    ///         sphere.DrawHandle();
    ///         
    ///         containedSphere.center = comp.innerposition;
    ///         containedSphere.size = comp.innerScale;
    ///         containedSphere.DrawHandle();
    ///         
    ///         if(EditorGUI.EndChangeCheck())
    ///         {
    ///             comp.innerposition = containedSphere.center;
    ///             comp.innersize = containedSphere.size;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class HierarchicalSphere
    {
        const float k_HandleSizeCoef = 0.05f;

        readonly Mesh m_Sphere;
        readonly Material m_Material;
        Color m_HandleColor;
        Color m_WireframeColor;
        Color m_WireframeColorBehind;

        readonly HierarchicalSphere m_Container;

        /// <summary>The position of the center of the box in Handle.matrix space.</summary>
        public Vector3 center { get; set; }

        /// <summary>The size of the box in Handle.matrix space.</summary>
        public float radius { get; set; }

        /// <summary>The baseColor used to fill hull. All other colors are deduced from it.</summary>
        public Color baseColor
        {
            get { return m_Material.color; }
            set
            {
                m_Material.color = value;
                value.a = 1f;
                m_HandleColor = value;
                value.a = 0.7f;
                m_WireframeColor = value;
                value.a = 0.2f;
                m_WireframeColorBehind = value;
            }
        }

        /// <summary>Constructor. Used to setup colors and also the container if any.</summary>
        /// <param name="baseColor">The color of filling. All other colors are deduced from it.</param>
        /// <param name="container">The HierarchicalSphere containing this sphere. If null, the sphere will not be limited in size.</param>
        public HierarchicalSphere(Color baseColor, HierarchicalSphere container = null)
        {
            m_Container = container;
            m_Sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            m_Material = new Material(Shader.Find("Hidden/UnlitTransparentColored"));
            this.baseColor = baseColor;
        }

        /// <summary>Draw the hull which means the boxes without the handles</summary>
        public void DrawHull(bool filled)
        {
            Color wireframeColor = m_HandleColor;
            wireframeColor.a = 0.8f;
            using (new Handles.DrawingScope(m_WireframeColor, Matrix4x4.TRS((Vector3)Handles.matrix.GetColumn(3) + center, Quaternion.identity, Vector3.one)))
            {
                if (filled)
                {
                    m_Material.SetPass(0);
                    Matrix4x4 drawMatrix = Matrix4x4.TRS((Vector3)Handles.matrix.GetColumn(3), Quaternion.identity, Vector3.one * radius * 2f);
                    Graphics.DrawMeshNow(m_Sphere, drawMatrix);
                }

                Vector3 drawCenter = Vector3.zero;
                Vector3 viewPlaneNormal;
                float drawnRadius = radius;
                if (Camera.current.orthographic)
                {
                    viewPlaneNormal = Camera.current.transform.forward;
                }
                else
                {
                    viewPlaneNormal = (Vector3)Handles.matrix.GetColumn(3) - Camera.current.transform.position;
                    float sqrDist = viewPlaneNormal.sqrMagnitude; // squared distance from camera to center
                    float sqrRadius = radius * radius; // squared radius
                    float sqrOffset = sqrRadius * sqrRadius / sqrDist; // squared distance from actual center to drawn disc center
                    float insideAmount = sqrOffset / sqrRadius;

                    // If we are not inside the sphere, calculate where to draw the periphery
                    if (insideAmount < 1)
                    {
                        drawnRadius = Mathf.Sqrt(sqrRadius - sqrOffset); // the radius of the drawn disc
                        drawCenter -= (sqrRadius / sqrDist) * viewPlaneNormal;
                    }
                }

                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
                //Handles.DrawWireDisc(Vector3.zero, Vector3.right, radius);
                //Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius);
                Handles.DrawWireDisc(drawCenter, viewPlaneNormal, drawnRadius);
                Handles.color = m_WireframeColorBehind;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
                //Handles.DrawWireDisc(Vector3.zero, Vector3.right, radius);
                //Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius);
                Handles.DrawWireDisc(drawCenter, viewPlaneNormal, drawnRadius);
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            }
        }

        /// <summary>Draw the manipulable handles</summary>
        public void DrawHandle()
        {
            using (new Handles.DrawingScope(m_HandleColor))
            {
                radius = Handles.RadiusHandle(Quaternion.identity, center, radius, handlesOnly: true);
                if(m_Container != null)
                {
                    radius = Mathf.Min(radius, m_Container.radius - Vector3.Distance(center, m_Container.center));
                }
            }
        }
    }
}
