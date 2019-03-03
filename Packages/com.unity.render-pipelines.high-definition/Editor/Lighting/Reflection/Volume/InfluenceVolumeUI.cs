using System;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI : BaseUI<SerializedInfluenceVolume>
    {
        const int k_AnimBoolFields = 2;
        static readonly int k_ShapeCount = Enum.GetValues(typeof(InfluenceShape)).Length;

        public HierarchicalBox boxBaseHandle;
        public HierarchicalBox boxInfluenceHandle;
        public HierarchicalBox boxInfluenceNormalHandle;


        public HierarchicalSphere sphereBaseHandle;
        public HierarchicalSphere sphereInfluenceHandle;
        public HierarchicalSphere sphereInfluenceNormalHandle;

        public AnimBool isSectionExpandedShape { get { return m_AnimBools[k_ShapeCount]; } }
        public bool showInfluenceHandles { get; set; }

        public InfluenceVolumeUI()
            : base(k_ShapeCount + k_AnimBoolFields)
        {
            isSectionExpandedShape.value = true;

            Color baseHandle = InfluenceVolumeUI.k_GizmoThemeColorBase;
            baseHandle.a = 1f;
            Color[] basehandleColors = new Color[]
            {
                baseHandle, baseHandle, baseHandle,
                baseHandle, baseHandle, baseHandle
            };
            boxBaseHandle = new HierarchicalBox(InfluenceVolumeUI.k_GizmoThemeColorBase, basehandleColors);
            boxBaseHandle.monoHandle = false;
            boxInfluenceHandle = new HierarchicalBox(InfluenceVolumeUI.k_GizmoThemeColorInfluence, k_HandlesColor, container: boxBaseHandle);
            boxInfluenceNormalHandle = new HierarchicalBox(InfluenceVolumeUI.k_GizmoThemeColorInfluenceNormal, k_HandlesColor, container: boxBaseHandle);

            sphereBaseHandle = new HierarchicalSphere(InfluenceVolumeUI.k_GizmoThemeColorBase);
            sphereInfluenceHandle = new HierarchicalSphere(InfluenceVolumeUI.k_GizmoThemeColorInfluence, container: sphereBaseHandle);
            sphereInfluenceNormalHandle = new HierarchicalSphere(InfluenceVolumeUI.k_GizmoThemeColorInfluenceNormal, container: sphereBaseHandle);
        }

        public void SetIsSectionExpanded_Shape(InfluenceShape shape)
        {
            SetIsSectionExpanded_Shape((int)shape);
        }

        public void SetIsSectionExpanded_Shape(int shape)
        {
            for (var i = 0; i < k_ShapeCount; i++)
                m_AnimBools[i].target = shape == i;
        }

        public AnimBool IsSectionExpanded_Shape(InfluenceShape shapeType)
        {
            return m_AnimBools[(int)shapeType];
        }
    }
}
