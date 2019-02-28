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
    ///     static HierarchicalBox box;
    ///     static HierarchicalBox containedBox;
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
    ///         box = new HierarchicalBox(new Color(1f, 1f, 1f, 0.25), handleColors);
    ///         containedBox = new HierarchicalBox(new Color(1f, 0f, 1f, 0.25), handleColors, container: box);
    ///     }
    ///
    ///     [DrawGizmo(GizmoType.Selected|GizmoType.Active)]
    ///     void DrawGizmo(MyComponent comp, GizmoType gizmoType)
    ///     {
    ///         box.center = comp.transform.position;
    ///         box.size = comp.transform.scale;
    ///         box.DrawHull(gizmoType == GizmoType.Selected);
    ///         
    ///         containedBox.center = comp.innerposition;
    ///         containedBox.size = comp.innerScale;
    ///         containedBox.DrawHull(gizmoType == GizmoType.Selected);
    ///     }
    ///
    ///     void OnSceneGUI()
    ///     {
    ///         EditorGUI.BeginChangeCheck();
    ///
    ///         //container box must be also set for contained box for clamping
    ///         box.center = comp.transform.position;
    ///         box.size = comp.transform.scale;
    ///         box.DrawHandle();
    ///         
    ///         containedBox.DrawHandle();
    ///         containedBox.center = comp.innerposition;
    ///         containedBox.size = comp.innerScale;
    ///         
    ///         if(EditorGUI.EndChangeCheck())
    ///         {
    ///             comp.innerposition = containedBox.center;
    ///             comp.innersize = containedBox.size;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class HierarchicalBox
    {
        const float k_HandleSizeCoef = 0.05f;

        enum NamedFace { Right, Top, Front, Left, Bottom, Back, None }

        readonly Mesh m_Face;
        readonly Material m_Material;
        readonly Color[] m_PolychromeHandleColor;
        Color m_MonochromeHandleColor;
        Color m_WireframeColor;
        Color m_WireframeColorBehind;

        readonly HierarchicalBox m_container;

        private bool m_MonoHandle = true;

        /// <summary>
        /// Allow to switch between the mode where all axis are controlled together or not
        /// Note that if there is several handles, they will use the polychrome colors.
        /// </summary>
        public bool monoHandle { get { return m_MonoHandle; } set { m_MonoHandle = value; } }

        private int[] m_ControlIDs = new int[6] { 0, 0, 0, 0, 0, 0 };

        /// <summary>The position of the center of the box in Handle.matrix space.</summary>
        public Vector3 center { get; set; }

        /// <summary>The size of the box in Handle.matrix space.</summary>
        public Vector3 size { get; set; }

        /// <summary>The baseColor used to fill hull. All other colors are deduced from it except specific handle colors.</summary>
        public Color baseColor
        {
            get { return m_Material.color; }
            set
            {
                m_Material.color = value;
                value.a = 1f;
                m_MonochromeHandleColor = value;
                value.a = 0.7f;
                m_WireframeColor = value;
                value.a = 0.2f;
                m_WireframeColorBehind = value;
            }
        }

        //Note: Handles.Slider not allow to use a specific ControlID.
        //Thus Slider1D is used (with reflection)
        static PropertyInfo k_scale = Type.GetType("UnityEditor.SnapSettings, UnityEditor").GetProperty("scale");
        static Type k_Slider1D = Type.GetType("UnityEditorInternal.Slider1D, UnityEditor");
        static MethodInfo k_Slider1D_Do = k_Slider1D
                .GetMethod(
                    "Do",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    CallingConventions.Any,
                    new[] { typeof(int), typeof(Vector3), typeof(Vector3), typeof(float), typeof(Handles.CapFunction), typeof(float) },
                    null);
        static void Slider1D(int controlID, ref Vector3 handlePosition, Vector3 handleOrientation, float snapScale, Color color)
        {
            using (new Handles.DrawingScope(color))
            {
                handlePosition = (Vector3)k_Slider1D_Do.Invoke(null, new object[]
                    {
                        controlID,
                        handlePosition,
                        handleOrientation,
                        HandleUtility.GetHandleSize(handlePosition) * k_HandleSizeCoef,
                        new Handles.CapFunction(Handles.DotHandleCap),
                        snapScale
                    });
            }
        }


        /// <summary>Constructor. Used to setup colors and also the container if any.</summary>
        /// <param name="baseColor">The color of each face of the box. Other colors are deduced from it.</param>
        /// <param name="polychromeHandleColors">The color of handle when they are separated. When they are grouped, they use a variation of the faceColor instead.</param>
        /// <param name="container">The HierarchicalBox containing this box. If null, the box will not be limited in size.</param>
        public HierarchicalBox(Color baseColor, Color[] polychromeHandleColors = null, HierarchicalBox container = null)
        {
            m_container = container;
            m_Material = new Material(Shader.Find("Hidden/UnlitTransparentColored"));
            this.baseColor = baseColor;
            m_Face = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            if(polychromeHandleColors != null && polychromeHandleColors.Length != 6)
            {
                throw new System.ArgumentException("polychromeHandleColors must be null or have a size of 6.");
            }
            m_PolychromeHandleColor = polychromeHandleColors ?? new Color[]
            {
                Handles.xAxisColor, Handles.yAxisColor, Handles.zAxisColor,
                Handles.xAxisColor, Handles.yAxisColor, Handles.zAxisColor
            };
        }

        Color GetHandleColor(NamedFace name)
        {
            return monoHandle ? m_MonochromeHandleColor : m_PolychromeHandleColor[(int)name];
        }

        /// <summary>Draw the hull which means the boxes without the handles</summary>
        public void DrawHull(bool filled)
        {
            Color previousColor = Handles.color;
            if (filled)
            {
                Vector3 xSize = new Vector3(size.z, size.y, 1f);
                m_Material.SetPass(0);
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.x * .5f * Vector3.left, Quaternion.FromToRotation(Vector3.forward, Vector3.left), xSize));
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.x * .5f * Vector3.right, Quaternion.FromToRotation(Vector3.forward, Vector3.right), xSize));
                
                Vector3 ySize = new Vector3(size.x, size.z, 1f);
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.y * .5f * Vector3.up, Quaternion.FromToRotation(Vector3.forward, Vector3.up), ySize));
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.y * .5f * Vector3.down, Quaternion.FromToRotation(Vector3.forward, Vector3.down), ySize));

                Vector3 zSize = new Vector3(size.x, size.y, 1f);
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.z * .5f * Vector3.forward, Quaternion.identity, zSize));
                Graphics.DrawMeshNow(m_Face, Handles.matrix * Matrix4x4.TRS(center + size.z * .5f * Vector3.back, Quaternion.FromToRotation(Vector3.forward, Vector3.back), zSize));

                //if contained, also draw handle distance to container here
                if (m_container != null)
                {
                    Vector3 centerDiff = center - m_container.center;
                    Vector3 xRecal = centerDiff;
                    Vector3 yRecal = centerDiff;
                    Vector3 zRecal = centerDiff;
                    xRecal.x = 0;
                    yRecal.y = 0;
                    zRecal.z = 0;
                    
                    Handles.color = GetHandleColor(NamedFace.Left);
                    Handles.DrawLine(m_container.center + xRecal + m_container.size.x * .5f * Vector3.left, center + size.x * .5f * Vector3.left);

                    Handles.color = GetHandleColor(NamedFace.Right);
                    Handles.DrawLine(m_container.center + xRecal + m_container.size.x * .5f * Vector3.right, center + size.x * .5f * Vector3.right);

                    Handles.color = GetHandleColor(NamedFace.Top);
                    Handles.DrawLine(m_container.center + yRecal + m_container.size.y * .5f * Vector3.up, center + size.y * .5f * Vector3.up);

                    Handles.color = GetHandleColor(NamedFace.Bottom);
                    Handles.DrawLine(m_container.center + yRecal + m_container.size.y * .5f * Vector3.down, center + size.y * .5f * Vector3.down);

                    Handles.color = GetHandleColor(NamedFace.Front);
                    Handles.DrawLine(m_container.center + zRecal + m_container.size.z * .5f * Vector3.forward, center + size.z * .5f * Vector3.forward);

                    Handles.color = GetHandleColor(NamedFace.Back);
                    Handles.DrawLine(m_container.center + zRecal + m_container.size.z * .5f * Vector3.back, center + size.z * .5f * Vector3.back);
                }
            }
            
            Handles.color = m_WireframeColor;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.DrawWireCube(center, size);
            Handles.color = m_WireframeColorBehind;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
            Handles.DrawWireCube(center, size);
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.color = previousColor;
        }

        /// <summary>Draw the manipulable handles</summary>
        public void DrawHandle()
        {
            for (int i = 0, count = m_ControlIDs.Length; i < count; ++i)
                m_ControlIDs[i] = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

            EditorGUI.BeginChangeCheck();

            Vector3 leftPosition = center + size.x * .5f * Vector3.left;
            Vector3 rightPosition = center + size.x * .5f * Vector3.right;
            Vector3 topPosition = center + size.y * .5f * Vector3.up;
            Vector3 bottomPosition = center + size.y * .5f * Vector3.down;
            Vector3 frontPosition = center + size.z * .5f * Vector3.forward;
            Vector3 backPosition = center + size.z * .5f * Vector3.back;

            float snapScale = (float)k_scale.GetValue(null, null);
            NamedFace theChangedFace = NamedFace.None;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Left], ref leftPosition, Vector3.left, snapScale, GetHandleColor(NamedFace.Left));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Left;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Right], ref rightPosition, Vector3.right, snapScale, GetHandleColor(NamedFace.Right));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Right;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Top], ref topPosition, Vector3.up, snapScale, GetHandleColor(NamedFace.Top));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Top;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Bottom], ref bottomPosition, Vector3.down, snapScale, GetHandleColor(NamedFace.Bottom));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Bottom;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Front], ref frontPosition, Vector3.forward, snapScale, GetHandleColor(NamedFace.Front));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Front;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Back], ref backPosition, Vector3.back, snapScale, GetHandleColor(NamedFace.Back));
            if (EditorGUI.EndChangeCheck() && monoHandle)
                theChangedFace = NamedFace.Back;

            if (EditorGUI.EndChangeCheck())
            {
                if (monoHandle)
                {
                    float decal = 0f;
                    switch (theChangedFace)
                    {
                        case NamedFace.Left:
                            decal = (leftPosition - center - size.x * .5f * Vector3.left).x;
                            break;
                        case NamedFace.Right:
                            decal = -(rightPosition - center - size.x * .5f * Vector3.right).x;
                            break;
                        case NamedFace.Top:
                            decal = -(topPosition - center - size.y * .5f * Vector3.up).y;
                            break;
                        case NamedFace.Bottom:
                            decal = (bottomPosition - center - size.y * .5f * Vector3.down).y;
                            break;
                        case NamedFace.Front:
                            decal = -(frontPosition - center - size.z * .5f * Vector3.forward).z;
                            break;
                        case NamedFace.Back:
                            decal = (backPosition - center - size.z * .5f * Vector3.back).z;
                            break;
                    }

                    Vector3 tempSize = size - Vector3.one * decal;

                    //ensure that the box face are still facing outside
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (tempSize[axis] < 0)
                        {
                            decal += tempSize[axis];
                            tempSize = size - Vector3.one * decal;
                        }
                    }

                    //ensure containedBox do not exit container
                    if (m_container != null)
                    {
                        for (int axis = 0; axis < 3; ++axis)
                        {
                            if (tempSize[axis] > m_container.size[axis])
                            {
                                tempSize[axis] = m_container.size[axis];
                            }
                        }
                    }

                    size = tempSize;
                }
                else
                {
                    Vector3 max = new Vector3(rightPosition.x, topPosition.y, frontPosition.z);
                    Vector3 min = new Vector3(leftPosition.x, bottomPosition.y, backPosition.z);

                    //ensure that the box face are still facing outside
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (min[axis] > max[axis])
                        {
                            if (GUIUtility.hotControl == m_ControlIDs[axis])
                            {
                                max[axis] = min[axis];
                            }
                            else
                            {
                                min[axis] = max[axis];
                            }
                        }
                    }

                    //ensure containedBox do not exit container
                    if (m_container != null)
                    {
                        for (int axis = 0; axis < 3; ++axis)
                        {
                            if (min[axis] < m_container.center[axis] - m_container.size[axis] * 0.5f)
                            {
                                min[axis] = m_container.center[axis] - m_container.size[axis] * 0.5f;
                            }
                            if (max[axis] > m_container.center[axis] + m_container.size[axis] * 0.5f)
                            {
                                max[axis] = m_container.center[axis] + m_container.size[axis] * 0.5f;
                            }
                        }
                    }
                    
                    center = (max + min) * .5f;
                    size = max - min;
                }
            }
        }
    }
}
