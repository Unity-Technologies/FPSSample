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

        public Gizmo6FacesBox boxBaseHandle;
        public Gizmo6FacesBoxContained boxInfluenceHandle;
        public Gizmo6FacesBoxContained boxInfluenceNormalHandle;

        public SphereBoundsHandle sphereBaseHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceNormalHandle = new SphereBoundsHandle();

        public AnimBool isSectionExpandedShape { get { return m_AnimBools[k_ShapeCount]; } }
        public bool showInfluenceHandles { get; set; }

        public InfluenceVolumeUI()
            : base(k_ShapeCount + k_AnimBoolFields)
        {
            isSectionExpandedShape.value = true;

            boxBaseHandle = new Gizmo6FacesBox(monochromeFace:true, monochromeSelectedFace:true);
            boxInfluenceHandle = new Gizmo6FacesBoxContained(boxBaseHandle, monochromeFace:true, monochromeSelectedFace:true);
            boxInfluenceNormalHandle = new Gizmo6FacesBoxContained(boxBaseHandle, monochromeFace:true, monochromeSelectedFace:true);

            Color[] handleColors = new Color[]
            {
                HDReflectionProbeEditor.k_handlesColor[0][0],
                HDReflectionProbeEditor.k_handlesColor[0][1],
                HDReflectionProbeEditor.k_handlesColor[0][2],
                HDReflectionProbeEditor.k_handlesColor[1][0],
                HDReflectionProbeEditor.k_handlesColor[1][1],
                HDReflectionProbeEditor.k_handlesColor[1][2]
            };
            boxBaseHandle.handleColors = handleColors;
            boxInfluenceHandle.handleColors = handleColors;
            boxInfluenceNormalHandle.handleColors = handleColors;

            boxBaseHandle.faceColors = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorExtent };
            boxBaseHandle.faceColorsSelected = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorExtentFace };
            boxInfluenceHandle.faceColors = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlend };
            boxInfluenceHandle.faceColorsSelected = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlendFace };
            boxInfluenceNormalHandle.faceColors = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlend };
            boxInfluenceNormalHandle.faceColorsSelected = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlendFace };
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
