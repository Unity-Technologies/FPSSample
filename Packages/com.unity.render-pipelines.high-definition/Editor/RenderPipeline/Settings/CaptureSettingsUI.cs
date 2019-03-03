using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<CaptureSettingsUI, SerializedCaptureSettings>;

    partial class CaptureSettingsUI : BaseUI<SerializedCaptureSettings>
    {
        internal const string captureSettingsHeaderContent = "Capture Settings";

        static readonly GUIContent clearColorModeContent = CoreEditorUtils.GetContent("Clear Mode");
        static readonly GUIContent backgroundColorHDRContent = CoreEditorUtils.GetContent("Background Color");
        static readonly GUIContent clearDepthContent = CoreEditorUtils.GetContent("Clear Depth");

        static readonly GUIContent cullingMaskContent = CoreEditorUtils.GetContent("Culling Mask");
        static readonly GUIContent useOcclusionCullingContent = CoreEditorUtils.GetContent("Occlusion Culling");

        static readonly GUIContent volumeLayerMaskContent = CoreEditorUtils.GetContent("Volume Layer Mask");
        static readonly GUIContent volumeAnchorOverrideContent = CoreEditorUtils.GetContent("Volume Anchor Override");
        
        static readonly GUIContent nearClipPlaneContent = CoreEditorUtils.GetContent("Near Clip Plane");
        static readonly GUIContent farClipPlaneContent = CoreEditorUtils.GetContent("Far Clip Plane");
        static readonly GUIContent fieldOfViewContent = CoreEditorUtils.GetContent("Field Of View");
        static readonly GUIContent fieldOfViewDefault = CoreEditorUtils.GetContent("Automatic|Computed depending on point of view");

        static readonly GUIContent renderingPathContent = CoreEditorUtils.GetContent("Rendering Path");

        static readonly GUIContent shadowDistanceContent = CoreEditorUtils.GetContent("Shadow Distance|DEPRECATED: Still available for baked and custom probe.\nWill be soon replaced by volume usage.\nTo set up volume, create a layer used in probe capture settings as volume layer mask and not in the camera layer mask.\nCreate a volume on this specific layer affecting shadow distance.");

#pragma warning disable 618 //CED
        public static CED.IDrawer SectionCaptureSettings = CED.LabelWidth(150, CED.Action((s, p, o) => Drawer_SectionCaptureSettings(s, p, o)));
#pragma warning restore 618

        public AnimBool isSectionExpandedCaptureSettings { get { return m_AnimBools[0]; } }

        public CaptureSettingsUI()
            : base(1)
        {
        }

        static void Drawer_SectionCaptureSettings(CaptureSettingsUI s, SerializedCaptureSettings p, Editor owner)
        {
            OverridableSettingsArea area = new OverridableSettingsArea(16);
            area.Add(p.clearColorMode, clearColorModeContent, () => p.overridesClearColorMode, a => p.overridesClearColorMode = a);
            area.Add(p.backgroundColorHDR, backgroundColorHDRContent, () => p.overridesBackgroundColorHDR, a => p.overridesBackgroundColorHDR = a);
            area.Add(p.clearDepth, clearDepthContent, () => p.overridesClearDepth, a => p.overridesClearDepth = a);
            area.Add(p.cullingMask, cullingMaskContent, () => p.overridesCullingMask, a => p.overridesCullingMask = a);
            area.Add(p.volumeLayerMask, volumeLayerMaskContent, () => p.overridesVolumeLayerMask, a => p.overridesVolumeLayerMask = a);
            area.Add(p.volumeAnchorOverride, volumeAnchorOverrideContent, () => p.overridesVolumeAnchorOverride, a => p.overridesVolumeAnchorOverride = a);
            area.Add(p.useOcclusionCulling, useOcclusionCullingContent, () => p.overridesUseOcclusionCulling, a => p.overridesUseOcclusionCulling = a);
            
            if (!(owner is PlanarReflectionProbeEditor)) //fixed for planar
            {
                area.Add(p.nearClipPlane, nearClipPlaneContent, () => p.overridesNearClip, a => p.overridesNearClip = a);
            }

            area.Add(p.farClipPlane, farClipPlaneContent, () => p.overridesFarClip, a => p.overridesFarClip = a);
            
            if (owner is PlanarReflectionProbeEditor) //fixed for cubemap
            {
                area.Add(p.fieldOfview, fieldOfViewContent, () => p.overridesFieldOfview, a => p.overridesFieldOfview = a, () => (owner is PlanarReflectionProbeEditor), defaultValue: (owner is PlanarReflectionProbeEditor) ? fieldOfViewDefault : null, forceOverride: true);
            }

            area.Add(p.shadowDistance, shadowDistanceContent, () => p.overridesShadowDistance, a => p.overridesShadowDistance = a, () => owner is HDProbeEditor && (owner as HDProbeEditor).GetTarget(owner.target).mode != UnityEngine.Rendering.ReflectionProbeMode.Realtime);
            area.Add(p.renderingPath, renderingPathContent, () => p.overridesRenderingPath, a => p.overridesRenderingPath = a);
            EditorGUI.BeginChangeCheck();
            area.Draw(withOverride: false);




            //hack while we rely on legacy probe for baking.
            //to remove once we do not rely on them
            if (EditorGUI.EndChangeCheck() && owner is HDReflectionProbeEditor)
            {
                ReflectionProbe rp = owner.target as ReflectionProbe;
                rp.clearFlags = p.clearColorMode.enumValueIndex == (int)HDAdditionalCameraData.ClearColorMode.Sky ? UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox : UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
                rp.backgroundColor = p.backgroundColorHDR.colorValue;
                rp.hdr = true;
                rp.cullingMask = p.cullingMask.intValue;
                rp.nearClipPlane = p.nearClipPlane.floatValue;
                rp.farClipPlane = p.farClipPlane.floatValue;
                rp.shadowDistance = p.shadowDistance.floatValue;
            }
        }
    }
}
