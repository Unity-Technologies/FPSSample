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
        const string asyncComputeSettingsHeaderContent = "Async Compute Settings";
        
        static readonly GUIContent transparentPrepassContent = CoreEditorUtils.GetContent("Transparent Prepass");
        static readonly GUIContent transparentPostpassContent = CoreEditorUtils.GetContent("Transparent Postpass");
        static readonly GUIContent motionVectorContent = CoreEditorUtils.GetContent("Motion Vectors");
        static readonly GUIContent objectMotionVectorsContent = CoreEditorUtils.GetContent("Object Motion Vectors");
        static readonly GUIContent decalsContent = CoreEditorUtils.GetContent("Decals");
        static readonly GUIContent roughRefractionContent = CoreEditorUtils.GetContent("Rough Refraction");
        static readonly GUIContent distortionContent = CoreEditorUtils.GetContent("Distortion");
        static readonly GUIContent postprocessContent = CoreEditorUtils.GetContent("Postprocess");
        static readonly GUIContent litShaderModeContent = CoreEditorUtils.GetContent("Lit Shader Mode");
        static readonly GUIContent depthPrepassWithDeferredRenderingContent = CoreEditorUtils.GetContent("Depth Prepass With Deferred Rendering");
        static readonly GUIContent opaqueObjectsContent = CoreEditorUtils.GetContent("Opaque Objects");
        static readonly GUIContent transparentObjectsContent = CoreEditorUtils.GetContent("Transparent Objects");
        static readonly GUIContent realtimePlanarReflectionContent = CoreEditorUtils.GetContent("Enable Realtime Planar Reflection"); 
        static readonly GUIContent msaaContent = CoreEditorUtils.GetContent("MSAA");
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

        // Async compute
        static readonly GUIContent asyncComputeContent = CoreEditorUtils.GetContent("Async Compute|This will have an effect only if target platform supports async compute.");
        static readonly GUIContent lightListAsyncContent = CoreEditorUtils.GetContent("Build Light List in Async");
        static readonly GUIContent SSRAsyncContent = CoreEditorUtils.GetContent("SSR in Async");
        static readonly GUIContent SSAOAsyncContent = CoreEditorUtils.GetContent("SSAO in Async");
        static readonly GUIContent contactShadowsAsyncContent = CoreEditorUtils.GetContent("Contact Shadows in Async");
        static readonly GUIContent volumeVoxelizationAsyncContent = CoreEditorUtils.GetContent("Volumetrics Voxelization in Async");


        static readonly GUIContent frameSettingsHeaderContent = CoreEditorUtils.GetContent("Frame Settings Override|Default FrameSettings are defined in HDRenderPipelineAsset.");
    }
}
