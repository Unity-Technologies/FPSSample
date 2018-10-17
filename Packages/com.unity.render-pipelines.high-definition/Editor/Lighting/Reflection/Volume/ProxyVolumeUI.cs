using System;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<ProxyVolumeUI, SerializedProxyVolume>;

    class ProxyVolumeUI : BaseUI<SerializedProxyVolume>
    {
        //Skin
        internal static Color k_GizmoThemeColorProjection = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        //internal static Color k_GizmoThemeColorProjectionFace = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        //internal static Color k_GizmoThemeColorDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);
        //internal static Color k_GizmoThemeColorDisabledFace = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);

        internal static GUIContent shapeContent = CoreEditorUtils.GetContent("Shape|The shape of the Proxy.\nInfinite is compatible with any kind of InfluenceShape.");
        internal static GUIContent boxSizeContent = CoreEditorUtils.GetContent("Box Size|The size of the box.");
        internal static GUIContent sphereRadiusContent = CoreEditorUtils.GetContent("Sphere Radius|The radius of the sphere.");


        //logic
        static readonly int k_ShapeCount = Enum.GetValues(typeof(InfluenceShape)).Length;

        public static readonly CED.IDrawer SectionShape;
        public static readonly CED.IDrawer SectionShapeBox = CED.Action(Drawer_SectionShapeBox);
        public static readonly CED.IDrawer SectionShapeSphere = CED.Action(Drawer_SectionShapeSphere);

        static ProxyVolumeUI()
        {
            SectionShape = CED.Group(
                    CED.Action(Drawer_FieldShapeType),
                    CED.FadeGroup(
                        (s, d, o, i) => s.IsSectionExpanded_Shape((InfluenceShape)i),
                        FadeOption.Indent,
                        SectionShapeBox,
                        SectionShapeSphere
                        )
                    );
        }

        public ProxyVolumeUI()
            : base(k_ShapeCount)
        {
        }

        public override void Update()
        {
            base.Update();
            if (data != null)
                SetIsSectionExpanded_Shape((InfluenceShape)data.shape.intValue);
        }

        void SetIsSectionExpanded_Shape(InfluenceShape shape)
        {
            for (var i = 0; i < k_ShapeCount; i++)
                m_AnimBools[i].target = (int)shape == i;
        }

        public AnimBool IsSectionExpanded_Shape(InfluenceShape shapeType)
        {
            return m_AnimBools[(int)shapeType];
        }


        //gizmo
        public static void DrawGizmos(Transform transform, ProxyVolume proxyVolume)
        {
            switch (proxyVolume.shape)
            {
                case ProxyShape.Box:
                    Gizmos_Box(transform, proxyVolume);
                    break;
                case ProxyShape.Sphere:
                    Gizmos_Sphere(transform, proxyVolume);
                    break;
            }
        }

        static void Gizmos_Sphere(Transform t, ProxyVolume d)
        {
            var mat = Gizmos.matrix;
            Color col = Gizmos.color;

            Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
            Gizmos.color = k_GizmoThemeColorProjection;

            Gizmos.DrawWireSphere(Vector3.zero, d.sphereRadius);

            Gizmos.color = col;
            Gizmos.matrix = mat;
        }

        static void Gizmos_Box(Transform t, ProxyVolume d)
        {
            var mat = Gizmos.matrix;
            Color col = Gizmos.color;

            Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
            Gizmos.color = k_GizmoThemeColorProjection;

            Gizmos.DrawWireCube(Vector3.zero, d.boxSize);

            Gizmos.color = col;
            Gizmos.matrix = mat;
        }


        //handles
        public BoxBoundsHandle boxProjectionHandle = new BoxBoundsHandle();
        public SphereBoundsHandle sphereProjectionHandle = new SphereBoundsHandle();

        static void Drawer_FieldShapeType(ProxyVolumeUI s, SerializedProxyVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.shape, shapeContent);
        }

        static void Drawer_SectionShapeBox(ProxyVolumeUI s, SerializedProxyVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.boxSize, boxSizeContent);
        }

        static void Drawer_SectionShapeSphere(ProxyVolumeUI s, SerializedProxyVolume d, Editor o)
        {
            EditorGUILayout.PropertyField(d.sphereRadius, sphereRadiusContent);
        }

        public static void DrawHandles_EditBase(Transform transform, ProxyVolume proxyVolume, ProxyVolumeUI ui, Object sourceAsset)
        {
            switch (proxyVolume.shape)
            {
                case ProxyShape.Box:
                    Handles_EditBase_Box(transform, proxyVolume, ui, sourceAsset);
                    break;
                case ProxyShape.Sphere:
                    Handles_EditBase_Sphere(transform, proxyVolume, ui, sourceAsset);
                    break;
            }
        }

        static void Handles_EditBase_Sphere(Transform transform, ProxyVolume proxyVolume, ProxyVolumeUI s, Object sourceAsset)
        {
            s.sphereProjectionHandle.center = Vector3.zero;
            s.sphereProjectionHandle.radius = proxyVolume.sphereRadius;
            
            Matrix4x4 mat = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            EditorGUI.BeginChangeCheck();
            using (new Handles.DrawingScope(k_GizmoThemeColorProjection, mat))
            {
                s.sphereProjectionHandle.DrawHandle();
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Projection Volume");

                proxyVolume.sphereRadius = s.sphereProjectionHandle.radius;

                //the gizmo is not an object of the scene. Flag it as dirty
                EditorUtility.SetDirty(sourceAsset);
            }
        }

        static void Handles_EditBase_Box(Transform transform, ProxyVolume proxyVolume, ProxyVolumeUI s, Object sourceAsset)
        {
            s.boxProjectionHandle.center = Vector3.zero;
            s.boxProjectionHandle.size = proxyVolume.boxSize;

            Matrix4x4 mat = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            EditorGUI.BeginChangeCheck();
            using (new Handles.DrawingScope(k_GizmoThemeColorProjection, mat))
            {
                s.boxProjectionHandle.DrawHandle();
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sourceAsset, "Modified Projection Volume AABB");

                proxyVolume.boxSize = s.boxProjectionHandle.size;

                //the gizmo is not an object of the scene. Flag it as dirty
                EditorUtility.SetDirty(sourceAsset);
            }
        }
    }
}
