using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<HDCameraUI, SerializedHDCamera>;

    partial class HDCameraUI
    {

        static HDCameraUI()
        {
            Inspector = new[]
            {
                CED.space,
                SectionGeneralSettings,
                // Not used for now
                //SectionPhysicalSettings,
                SectionOutputSettings,
                SectionXRSettings,
                SectionRenderLoopSettings
            };
        }

        public static readonly CED.IDrawer[] Inspector = null;

        public static readonly CED.IDrawer SectionGeneralSettings = CED.FoldoutGroup(
                generalSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedGeneralSettings,
                FoldoutOption.Indent,
                CED.Action(Drawer_FieldClear),
                CED.Action(Drawer_FieldCullingMask),
                CED.Action(Drawer_FieldVolumeLayerMask),
                CED.Action(Drawer_FieldVolumeAnchorOverride),
                CED.Action(Drawer_FieldOcclusionCulling),
                CED.space,
                CED.Action(Drawer_Projection),
                CED.Action(Drawer_FieldClippingPlanes),
                CED.space,
                CED.Action(Drawer_CameraWarnings),
                CED.Action(Drawer_FieldRenderingPath),
                CED.space
                );

        public static readonly CED.IDrawer SectionPhysicalSettings = CED.FoldoutGroup(
                physicalSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedPhysicalSettings,
                FoldoutOption.Indent,
                CED.Action(Drawer_FieldAperture),
                CED.Action(Drawer_FieldShutterSpeed),
                CED.Action(Drawer_FieldIso),
                CED.space);

        public static readonly CED.IDrawer SectionOutputSettings = CED.FoldoutGroup(
                outputSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedOutputSettings,
                FoldoutOption.Indent,
#if ENABLE_MULTIPLE_DISPLAYS
                CED.Action(Drawer_SectionMultiDisplay),
#endif
                CED.Action(Drawer_FieldRenderTarget),
                CED.Action(Drawer_FieldDepth),
                CED.Action(Drawer_FieldNormalizedViewPort),
                CED.space);

        public static readonly CED.IDrawer SectionXRSettings = CED.FadeGroup(
                (s, d, o, i) => s.isSectionAvailableXRSettings,
                FadeOption.None,
                CED.FoldoutGroup(
                    xrSettingsHeaderContent,
                    (s, p, o) => s.isSectionExpandedXRSettings,
                    FoldoutOption.Indent,
                    CED.Action(Drawer_FieldVR),
                    CED.Action(Drawer_FieldTargetEye)));

        public static readonly CED.IDrawer SectionRenderLoopSettings = CED.FadeGroup(
                (s, d, o, i) => s.isSectionAvailableRenderLoopSettings,
                FadeOption.None,
                CED.Select(
                    (s, d, o) => s.frameSettingsUI,
                    (s, d, o) => d.frameSettings,
                    FrameSettingsUI.Inspector(withXR: false)));

        static void Drawer_FieldBackgroundColorHDR(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.backgroundColorHDR, backgroundColorContent);
        }

        static void Drawer_FieldVolumeLayerMask(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.volumeLayerMask, volumeLayerMaskContent);
        }
        static void Drawer_FieldVolumeAnchorOverride(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.volumeAnchorOverride, volumeAnchorOverrideContent);
        }

        static void Drawer_FieldCullingMask(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.cullingMask, cullingMaskContent);
        }

        static void Drawer_Projection(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            CameraProjection projection = p.orthographic.boolValue ? CameraProjection.Orthographic : CameraProjection.Perspective;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.orthographic.hasMultipleDifferentValues;
            projection = (CameraProjection)EditorGUILayout.EnumPopup(projectionContent, projection);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                p.orthographic.boolValue = (projection == CameraProjection.Orthographic);

            if (!p.orthographic.hasMultipleDifferentValues)
            {
                if (projection == CameraProjection.Orthographic)
                    EditorGUILayout.PropertyField(p.orthographicSize, sizeContent);
                else
                    EditorGUILayout.Slider(p.fieldOfView, 1f, 179f, fieldOfViewContent);
            }
        }

        static void Drawer_FieldClippingPlanes(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            CoreEditorUtils.DrawMultipleFields(
                clippingPlaneMultiFieldTitle,
                new[] { p.nearClippingPlane, p.farClippingPlane },
                new[] { nearPlaneContent, farPlaneContent });
        }

        static void Drawer_FieldAperture(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.aperture, apertureContent);
        }

        static void Drawer_FieldShutterSpeed(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            float shutterSpeed = 1f / p.shutterSpeed.floatValue;
            EditorGUI.BeginChangeCheck();
            shutterSpeed = EditorGUILayout.FloatField(shutterSpeedContent, shutterSpeed);
            if (EditorGUI.EndChangeCheck())
            {
                p.shutterSpeed.floatValue = 1f / shutterSpeed;
                p.Apply();
            }
        }

        static void Drawer_FieldIso(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.iso, isoContent);
        }

        static void Drawer_FieldNormalizedViewPort(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.normalizedViewPortRect, viewportContent);
        }

        static void Drawer_FieldDepth(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.depth, depthContent);
        }

        static void Drawer_FieldClear(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.clearColorMode, clearModeContent);
            //if(p.clearColorMode.enumValueIndex == (int)HDAdditionalCameraData.ClearColorMode.BackgroundColor) or no sky in scene
            EditorGUILayout.PropertyField(p.backgroundColorHDR, backgroundColorContent);
            EditorGUILayout.PropertyField(p.clearDepth, clearDepthContent);
        }

        static void Drawer_FieldRenderingPath(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.renderingPath, renderingPathContent);
        }

        static void Drawer_FieldRenderTarget(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.targetTexture);

            // show warning if we have deferred but manual MSAA set
            // only do this if the m_TargetTexture has the same values across all target cameras
            if (!p.targetTexture.hasMultipleDifferentValues)
            {
                var targetTexture = p.targetTexture.objectReferenceValue as RenderTexture;
                if (targetTexture
                    && targetTexture.antiAliasing > 1
                    && !p.frameSettings.enableForwardRenderingOnly.boolValue)
                {
                    EditorGUILayout.HelpBox(msaaWarningMessage, MessageType.Warning, true);
                }
            }
        }

        static void Drawer_FieldOcclusionCulling(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.occlusionCulling, occlusionCullingContent);
        }

        static void Drawer_CameraWarnings(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            foreach (Camera camera in p.serializedObject.targetObjects)
            {
                var warnings = GetCameraBufferWarnings(camera);
                if (warnings.Length > 0)
                    EditorGUILayout.HelpBox(string.Join("\n\n", warnings), MessageType.Warning, true);
            }
        }

        static void Drawer_FieldVR(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            if (s.canOverrideRenderLoopSettings)
                EditorGUILayout.PropertyField(p.frameSettings.enableStereo, enableStereoContent);
            else
            {
                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                Assert.IsNotNull(hdrp, "This Editor is valid only for HDRP");
                var enableStereo = hdrp.GetFrameSettings().enableStereo;
                GUI.enabled = false;
                EditorGUILayout.Toggle(hdrpEnableStereoContent, enableStereo);
                GUI.enabled = true;
            }
            EditorGUILayout.PropertyField(p.stereoSeparation, stereoSeparationContent);
            EditorGUILayout.PropertyField(p.stereoConvergence, stereoConvergenceContent);
        }

#if ENABLE_MULTIPLE_DISPLAYS
        static void Drawer_SectionMultiDisplay(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            if (ModuleManager_ShouldShowMultiDisplayOption())
            {
                var prevDisplay = p.targetDisplay.intValue;
                EditorGUILayout.IntPopup(p.targetDisplay, DisplayUtility_GetDisplayNames(), DisplayUtility_GetDisplayIndices(), targetDisplayContent);
                if (prevDisplay != p.targetDisplay.intValue)
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

#endif

        static readonly int[] k_TargetEyeValues = { (int)StereoTargetEyeMask.Both, (int)StereoTargetEyeMask.Left, (int)StereoTargetEyeMask.Right, (int)StereoTargetEyeMask.None };

        static void Drawer_FieldTargetEye(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.IntPopup(p.targetEye, k_TargetEyes, k_TargetEyeValues, targetEyeContent);
        }

        static MethodInfo k_DisplayUtility_GetDisplayIndices = Type.GetType("UnityEditor.DisplayUtility,UnityEditor")
            .GetMethod("GetDisplayIndices");
        static int[] DisplayUtility_GetDisplayIndices()
        {
            return (int[])k_DisplayUtility_GetDisplayIndices.Invoke(null, null);
        }

        static MethodInfo k_DisplayUtility_GetDisplayNames = Type.GetType("UnityEditor.DisplayUtility,UnityEditor")
            .GetMethod("GetDisplayNames");
        static GUIContent[] DisplayUtility_GetDisplayNames()
        {
            return (GUIContent[])k_DisplayUtility_GetDisplayNames.Invoke(null, null);
        }

        static MethodInfo k_ModuleManager_ShouldShowMultiDisplayOption = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor")
            .GetMethod("ShouldShowMultiDisplayOption", BindingFlags.Static | BindingFlags.NonPublic);
        static bool ModuleManager_ShouldShowMultiDisplayOption()
        {
            return (bool)k_ModuleManager_ShouldShowMultiDisplayOption.Invoke(null, null);
        }

        static readonly MethodInfo k_Camera_GetCameraBufferWarnings = typeof(Camera).GetMethod("GetCameraBufferWarnings", BindingFlags.Instance | BindingFlags.NonPublic);
        static string[] GetCameraBufferWarnings(Camera camera)
        {
            return (string[])k_Camera_GetCameraBufferWarnings.Invoke(camera, null);
        }
    }
}
