using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{

    partial class InfluenceVolumeUI
    {
 
        static readonly Color k_GizmoThemeColorBase = new Color(255f / 255f, 229f / 255f, 148f / 255f, 80f / 255f);
        //static readonly Color k_GizmoThemeColorBaseFace = new Color(255f / 255f, 229f / 255f, 148f / 255f, 45f / 255f);
        static readonly Color k_GizmoThemeColorInfluence = new Color(83f / 255f, 255f / 255f, 95f / 255f, 75f / 255f);
        //static readonly Color k_GizmoThemeColorInfluenceFace = new Color(83f / 255f, 255f / 255f, 95f / 255f, 17f / 255f);
        static readonly Color k_GizmoThemeColorInfluenceNormal = new Color(0f / 255f, 229f / 255f, 255f / 255f, 80f / 255f);
        //static readonly Color k_GizmoThemeColorInfluenceNormalFace = new Color(0f / 255f, 229f / 255f, 255f / 255f, 36f / 255f);
        //static readonly Color k_GizmoThemeColorProjection = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        //static readonly Color k_GizmoThemeColorProjectionFace = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        //static readonly Color k_GizmoThemeColorDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);
        //static readonly Color k_GizmoThemeColorDisabledFace = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);
        

        static readonly GUIContent shapeContent = CoreEditorUtils.GetContent("Shape");
        static readonly GUIContent boxSizeContent = CoreEditorUtils.GetContent("Box Size|The size of the box in which the reflections will be applied to objects. The value is not affected by the Transform of the Game Object.");
        static readonly GUIContent offsetContent = CoreEditorUtils.GetContent("Offset|The center of the InfluenceVolume in which the reflections will be applied to objects. The value is relative to the position of the Game Object.");
        static readonly GUIContent blendDistanceContent = CoreEditorUtils.GetContent("Blend Distance|Area around the probe where it is blended with other probes. Only used in deferred probes.");
        static readonly GUIContent blendNormalDistanceContent = CoreEditorUtils.GetContent("Blend Normal Distance|Area around the probe where the normals influence the probe. Only used in deferred probes.");
        static readonly GUIContent faceFadeContent = CoreEditorUtils.GetContent("Face fade|Fade faces of the cubemap.");

        static readonly GUIContent radiusContent = CoreEditorUtils.GetContent("Radius");

        static readonly GUIContent normalModeContent = CoreEditorUtils.GetContent("Normal|Normal parameters mode (only change for box shape).");
        static readonly GUIContent advancedModeContent = CoreEditorUtils.GetContent("Advanced|Advanced parameters mode (only change for box shape).");
        
        static readonly string influenceVolumeHeader = "Influence Volume";
    }
}
