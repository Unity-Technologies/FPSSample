using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDCameraUI
    {
        const string generalSettingsHeaderContent = "General";
        const string physicalSettingsHeaderContent = "Physical Settings";
        const string outputSettingsHeaderContent = "Output Settings";
        const string xrSettingsHeaderContent = "XR Settings";

        const string clippingPlaneMultiFieldTitle = "Clipping Planes";

        const string msaaWarningMessage = "Manual MSAA target set with deferred rendering. This will lead to undefined behavior.";

        static readonly GUIContent clearModeContent = CoreEditorUtils.GetContent("Clear Mode|The Camera clears the screen to selected mode.");
        static readonly GUIContent backgroundColorContent = CoreEditorUtils.GetContent("Background Color|The BackgroundColor used to clear the screen when selecting BackgrounColor before rendering.");
        static readonly GUIContent clearDepthContent = CoreEditorUtils.GetContent("ClearDepth|The Camera clears the depth buffer before rendering.");
        static readonly GUIContent cullingMaskContent = CoreEditorUtils.GetContent("Culling Mask");
        static readonly GUIContent volumeLayerMaskContent = CoreEditorUtils.GetContent("Volume Layer Mask");
        static readonly GUIContent volumeAnchorOverrideContent = CoreEditorUtils.GetContent("Volume Anchor Override");
        static readonly GUIContent occlusionCullingContent = CoreEditorUtils.GetContent("Occlusion Culling");

        //static readonly GUIContent projectionContent = CoreEditorUtils.GetContent("Projection|How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective.");
        //static readonly GUIContent sizeContent = CoreEditorUtils.GetContent("Size");
        //static readonly GUIContent fieldOfViewContent = CoreEditorUtils.GetContent("Field of View|The width of the Camera’s view angle, measured in degrees along the local Y axis.");
        static readonly GUIContent nearPlaneContent = CoreEditorUtils.GetContent("Near|The closest point relative to the camera that drawing will occur.");
        static readonly GUIContent farPlaneContent = CoreEditorUtils.GetContent("Far|The furthest point relative to the camera that drawing will occur.");

        static readonly GUIContent renderingPathContent = CoreEditorUtils.GetContent("Rendering Path");

        static readonly GUIContent apertureContent = CoreEditorUtils.GetContent("Aperture");
        static readonly GUIContent shutterSpeedContent = CoreEditorUtils.GetContent("Shutter Speed (1 / x)");
        static readonly GUIContent isoContent = CoreEditorUtils.GetContent("ISO");

        static readonly GUIContent viewportContent = CoreEditorUtils.GetContent("Viewport Rect|Four values that indicate where on the screen this camera view will be drawn. Measured in Viewport Coordinates (values 0–1).");
        static readonly GUIContent depthContent = CoreEditorUtils.GetContent("Depth");
#if ENABLE_MULTIPLE_DISPLAYS
        static readonly GUIContent targetDisplayContent = CoreEditorUtils.GetContent("Target Display");
#endif


        static readonly GUIContent stereoSeparationContent = CoreEditorUtils.GetContent("Stereo Separation");
        static readonly GUIContent stereoConvergenceContent = CoreEditorUtils.GetContent("Stereo Convergence");
        static readonly GUIContent targetEyeContent = CoreEditorUtils.GetContent("Target Eye");
        static readonly GUIContent[] k_TargetEyes = //order must match k_TargetEyeValues
        {
            new GUIContent("Both"),
            new GUIContent("Left"),
            new GUIContent("Right"),
            new GUIContent("None (Main Display)"),
        };

    }
}
