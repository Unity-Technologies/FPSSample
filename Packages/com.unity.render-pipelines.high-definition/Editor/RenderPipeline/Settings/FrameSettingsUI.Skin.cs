using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class FrameSettingsUI
    {
        const string renderingPassesHeaderContent = "Rendering Passes";
        const string renderingSettingsHeaderContent = "Rendering Settings";
        const string xrSettingsHeaderContent = "XR Settings";
        const string lightSettingsHeaderContent = "Lighting Settings";
        
        static readonly GUIContent transparentPrepassContent = CoreEditorUtils.GetContent("Transparent Prepass");
        static readonly GUIContent transparentPostpassContent = CoreEditorUtils.GetContent("Transparent Postpass");
        static readonly GUIContent motionVectorContent = CoreEditorUtils.GetContent("Motion Vectors");
        static readonly GUIContent objectMotionVectorsContent = CoreEditorUtils.GetContent("Object Motion Vectors");
        static readonly GUIContent decalsContent = CoreEditorUtils.GetContent("Decals");
        static readonly GUIContent roughRefractionContent = CoreEditorUtils.GetContent("Rough Refraction");
        static readonly GUIContent distortionContent = CoreEditorUtils.GetContent("Distortion");
        static readonly GUIContent postprocessContent = CoreEditorUtils.GetContent("Postprocess");
        static readonly GUIContent forwardRenderingOnlyContent = CoreEditorUtils.GetContent("Forward Rendering Only");
        static readonly GUIContent depthPrepassWithDeferredRenderingContent = CoreEditorUtils.GetContent("Depth Prepass With Deferred Rendering");
        static readonly GUIContent asyncComputeContent = CoreEditorUtils.GetContent("Async Compute");
        static readonly GUIContent opaqueObjectsContent = CoreEditorUtils.GetContent("Opaque Objects");
        static readonly GUIContent transparentObjectsContent = CoreEditorUtils.GetContent("Transparent Objects");
        static readonly GUIContent realtimePlanarReflectionContent = CoreEditorUtils.GetContent("Enable Realtime Planar Reflection"); 
        static readonly GUIContent msaaContent = CoreEditorUtils.GetContent("MSAA");
        static readonly GUIContent stereoContent = CoreEditorUtils.GetContent("Stereo");
        static readonly GUIContent xrGraphicConfigContent = CoreEditorUtils.GetContent("XR Graphics Config");
        static readonly GUIContent shadowContent = CoreEditorUtils.GetContent("Shadow");
        static readonly GUIContent contactShadowContent = CoreEditorUtils.GetContent("Contact Shadows");
        static readonly GUIContent shadowMaskContent = CoreEditorUtils.GetContent("Shadow Masks");
        static readonly GUIContent ssrContent = CoreEditorUtils.GetContent("SSR");
        static readonly GUIContent ssaoContent = CoreEditorUtils.GetContent("SSAO");
        static readonly GUIContent subsurfaceScatteringContent = CoreEditorUtils.GetContent("Subsurface Scattering");
        static readonly GUIContent transmissionContent = CoreEditorUtils.GetContent("Transmission");
        static readonly GUIContent atmosphericScatteringContent = CoreEditorUtils.GetContent("Atmospheric Scattering");
        static readonly GUIContent volumetricContent = CoreEditorUtils.GetContent("Volumetric");
        static readonly GUIContent reprojectionForVolumetricsContent = CoreEditorUtils.GetContent("Reprojection For Volumetrics");
        static readonly GUIContent lightLayerContent = CoreEditorUtils.GetContent("LightLayers");

        static readonly GUIContent frameSettingsHeaderContent = CoreEditorUtils.GetContent("Frame Settings Override|Default FrameSettings are defined in HDRenderPipelineAsset.");
    }
}
