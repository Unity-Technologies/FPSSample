using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace UnityEngine.Experimental.Rendering
{
    public class Gizmo6FacesBox
    {
        const float k_HandleSizeCoef = 0.05f;

        protected enum NamedFace { Right, Top, Front, Left, Bottom, Back, None }
        protected enum Element { Face, SelectedFace, Handle }

        Mesh m_face = null;

        Mesh face
        {
            get
            {
                if (m_face == null)
                {
                    m_face = new Mesh();
                    m_face.vertices = new Vector3[] {
                        new Vector3(-.5f,-.5f,0f),
                        new Vector3(+.5f,-.5f,0f),
                        new Vector3(+.5f,+.5f,0f),
                        new Vector3(-.5f,+.5f,0f)
                    };
                    m_face.triangles = new int[] {
                        0, 1, 2,
                        2, 3, 0
                    };
                    m_face.RecalculateNormals();
                }
                return m_face;
            }
        }

        Color[] m_faceColorsSelected;

        public Color[] faceColorsSelected
        {
            get
            {
                return m_faceColorsSelected ?? (m_faceColorsSelected = monochromeSelectedFace
                    ? new Color[]
                    {
                        new Color(1f, 1f, 1f, .15f)
                    }
                    : new Color[]
                    {
                        new Color(1f, 0f, 0f, .15f),
                        new Color(0f, 1f, 0f, .15f),
                        new Color(0f, 0f, 1f, .15f),
                        new Color(1f, 0f, 0f, .15f),
                        new Color(0f, 1f, 0f, .15f),
                        new Color(0f, 0f, 1f, .15f)
                    });
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("FaceColor cannot be set to null.");
                }
                if (value.Length != (monochromeSelectedFace ? 1 : 6))
                {
                    throw new ArgumentException("FaceColor must have 6 entries: X Y Z -X -Y -Z or only one in monochrome mode");
                }
                m_faceColorsSelected = value;
            }
        }

        Color[] m_faceColors;

        public Color[] faceColors
        {
            get
            {
                return m_faceColors ?? (m_faceColors = monochromeFace
                    ? new Color[]
                    {
                        new Color(.5f, .5f, .5f, .15f)
                    }
                    : new Color[]
                    {
                        new Color(.5f, 0f, 0f, .15f),
                        new Color(0f, .5f, 0f, .15f),
                        new Color(0f, 0f, .5f, .15f),
                        new Color(.5f, 0f, 0f, .15f),
                        new Color(0f, .5f, 0f, .15f),
                        new Color(0f, 0f, .5f, .15f)
                    });
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("FaceColor cannot be set to null.");
                }
                if (value.Length != (monochromeFace ? 1 : 6))
                {
                    throw new ArgumentException("FaceColor must have 6 entries: X Y Z -X -Y -Z or only one in monochrome mode");
                }
                m_faceColors = value;
            }
        }

        Color[] m_handleColors;

        public Color[] handleColors
        {
            get
            {
                return m_handleColors ?? (m_handleColors = monochromeHandle
                    ? new Color[]
                    {
                        new Color(1f, 0f, 0f, 1f)
                    }
                    : new Color[]
                    {
                        new Color(1f, 0f, 0f, 1f),
                        new Color(0f, 1f, 0f, 1f),
                        new Color(0f, 0f, 1f, 1f),
                        new Color(1f, 0f, 0f, 1f),
                        new Color(0f, 1f, 0f, 1f),
                        new Color(0f, 0f, 1f, 1f)
                    });
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("HandleColor cannot be set to null.");
                }
                if (value.Length != (monochromeHandle ? 1 : 6))
                {
                    throw new ArgumentException("HandleColor must have 6 entries: X Y Z -X -Y -Z or only one in monochrome mode");
                }
                m_handleColors = value;
            }
        }

        public readonly bool monochromeHandle;
        public readonly bool monochromeFace;
        public readonly bool monochromeSelectedFace;

        public bool allHandleControledByOne = false;

        private int[] m_ControlIDs = new int[6] { 0, 0, 0, 0, 0, 0 };

        public Vector3 center { get; set; }

        public Vector3 size { get; set; }

        public Gizmo6FacesBox(bool monochromeHandle = false, bool monochromeFace = false, bool monochromeSelectedFace = false)
        {
            this.monochromeHandle = monochromeHandle;
            this.monochromeFace = monochromeFace;
            this.monochromeSelectedFace = monochromeSelectedFace;
        }

        protected Color GetColor(NamedFace name, Element element)
        {
            switch(element)
            {
                default:
                case Element.Face: return faceColors[monochromeFace ? 0 : (int)name];
                case Element.SelectedFace: return faceColorsSelected[monochromeSelectedFace ? 0 : (int)name];
                case Element.Handle: return handleColors[monochromeHandle ? 0 : (int)name];
            }
        }

        public virtual void DrawHull(bool selected)
        {
            Color colorGizmo = Gizmos.color;

            Element element = selected ? Element.SelectedFace : Element.Face;

            if (selected)
            {
                Vector3 xSize = new Vector3(size.z, size.y, 1f);
                Gizmos.color = GetColor(NamedFace.Left, element);
                Gizmos.DrawMesh(face, center + size.x * .5f * Vector3.left, Quaternion.FromToRotation(Vector3.forward, Vector3.left), xSize);
                Gizmos.color = GetColor(NamedFace.Right, element);
                Gizmos.DrawMesh(face, center + size.x * .5f * Vector3.right, Quaternion.FromToRotation(Vector3.forward, Vector3.right), xSize);

                Vector3 ySize = new Vector3(size.x, size.z, 1f);
                Gizmos.color = GetColor(NamedFace.Top, element);
                Gizmos.DrawMesh(face, center + size.y * .5f * Vector3.up, Quaternion.FromToRotation(Vector3.forward, Vector3.up), ySize);
                Gizmos.color = GetColor(NamedFace.Bottom, element);
                Gizmos.DrawMesh(face, center + size.y * .5f * Vector3.down, Quaternion.FromToRotation(Vector3.forward, Vector3.down), ySize);

                Vector3 zSize = new Vector3(size.x, size.y, 1f);
                Gizmos.color = GetColor(NamedFace.Front, element);
                Gizmos.DrawMesh(face, center + size.z * .5f * Vector3.forward, Quaternion.identity, zSize);
                Gizmos.color = GetColor(NamedFace.Back, element);
                Gizmos.DrawMesh(face, center + size.z * .5f * Vector3.back, Quaternion.FromToRotation(Vector3.forward, Vector3.back), zSize);
            }

            Gizmos.color = colorGizmo;
            Gizmos.DrawWireCube(center, size);
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
            Slider1D(m_ControlIDs[(int)NamedFace.Left], ref leftPosition, Vector3.left, snapScale, GetColor(NamedFace.Left, Element.Handle));
            if (EditorGUI.EndChangeCheck() && allHandleControledByOne)
                theChangedFace = NamedFace.Left;

            EditorGUI.BeginChangeCheck();
            using (new Handles.DrawingScope(GetColor(NamedFace.Right, Element.Handle)))
            Slider1D(m_ControlIDs[(int)NamedFace.Right], ref rightPosition, Vector3.right, snapScale, GetColor(NamedFace.Right, Element.Handle));
            if (EditorGUI.EndChangeCheck() && allHandleControledByOne)
                theChangedFace = NamedFace.Right;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Top], ref topPosition, Vector3.up, snapScale, GetColor(NamedFace.Top, Element.Handle));
            if (EditorGUI.EndChangeCheck() && allHandleControledByOne)
                theChangedFace = NamedFace.Top;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Bottom], ref bottomPosition, Vector3.down, snapScale, GetColor(NamedFace.Bottom, Element.Handle));
            if (EditorGUI.EndChangeCheck() && allHandleControledByOne)
                theChangedFace = NamedFace.Bottom;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Front], ref frontPosition, Vector3.forward, snapScale, GetColor(NamedFace.Front, Element.Handle));
            if (EditorGUI.EndChangeCheck() && allHandleControledByOne)
                theChangedFace = NamedFace.Front;

            EditorGUI.BeginChangeCheck();
            Slider1D(m_ControlIDs[(int)NamedFace.Back], ref backPosition, Vector3.back, snapScale, GetColor(NamedFace.Back, Element.Handle));
            if (EditorGUI.EndChangeCheck() && allHandleControledByOne)
                theChangedFace = NamedFace.Back;

            if (EditorGUI.EndChangeCheck())
            {
                if(allHandleControledByOne)
                {
                    float decal = 0f;
                    switch(theChangedFace)
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
                    for (int axis = 0; axis < 3; ++axis)
                    {
                        if (tempSize[axis] < 0)
                        {
                            decal += tempSize[axis];
                            tempSize = size - Vector3.one * decal;
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

                    center = (max + min) * .5f;
                    size = max - min;
                }
            }
        }
    }

    public class Gizmo6FacesBoxContained : Gizmo6FacesBox
    {
        private Gizmo6FacesBox m_container;

        public Gizmo6FacesBox container
        {
            get
            {
                return m_container;
            }
            set
            {
                if (value == null)
                    throw new System.ArgumentNullException("Container cannot be null. Use Gizmo6FacesBox instead.");
                m_container = value;
            }
        }

        public Gizmo6FacesBoxContained(Gizmo6FacesBox container, bool monochromeHandle = false, bool monochromeFace = false, bool monochromeSelectedFace = false) : base(monochromeHandle, monochromeFace, monochromeSelectedFace)
        {
            m_container = container;
        }

        public override void DrawHull(bool selected)
        {
            Color colorGizmo = Gizmos.color;
            base.DrawHull(selected);

            //if selected, also draw handle distance to container here
            if (selected)
            {
                Vector3 centerDiff = center - m_container.center;
                Vector3 xRecal = centerDiff;
                Vector3 yRecal = centerDiff;
                Vector3 zRecal = centerDiff;
                xRecal.x = 0;
                yRecal.y = 0;
                zRecal.z = 0;

                Gizmos.color = GetColor(NamedFace.Left, Element.Handle);
                Gizmos.DrawLine(m_container.center + xRecal + m_container.size.x * .5f * Vector3.left, center + size.x * .5f * Vector3.left);

                Gizmos.color = GetColor(NamedFace.Right, Element.Handle);
                Gizmos.DrawLine(m_container.center + xRecal + m_container.size.x * .5f * Vector3.right, center + size.x * .5f * Vector3.right);

                Gizmos.color = GetColor(NamedFace.Top, Element.Handle);
                Gizmos.DrawLine(m_container.center + yRecal + m_container.size.y * .5f * Vector3.up, center + size.y * .5f * Vector3.up);

                Gizmos.color = GetColor(NamedFace.Bottom, Element.Handle);
                Gizmos.DrawLine(m_container.center + yRecal + m_container.size.y * .5f * Vector3.down, center + size.y * .5f * Vector3.down);

                Gizmos.color = GetColor(NamedFace.Front, Element.Handle);
                Gizmos.DrawLine(m_container.center + zRecal + m_container.size.z * .5f * Vector3.forward, center + size.z * .5f * Vector3.forward);

                Gizmos.color = GetColor(NamedFace.Back, Element.Handle);
                Gizmos.DrawLine(m_container.center + zRecal + m_container.size.z * .5f * Vector3.back, center + size.z * .5f * Vector3.back);
            }

            Gizmos.color = colorGizmo;
        }
    }
}
