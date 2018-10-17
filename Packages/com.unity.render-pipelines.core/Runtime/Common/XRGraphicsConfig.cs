using System;
using UnityEditor;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
using XRSettings = UnityEngine.XR.XRSettings;
#elif UNITY_5_6_OR_NEWER
using UnityEngine.VR;
using XRSettings = UnityEngine.VR.VRSettings;
#endif

namespace UnityEngine.Experimental.Rendering
{
    [Serializable]
    public class XRGraphicsConfig
    { // XRGConfig stores the desired XR settings for a given SRP asset.

        public float renderScale = 1.0f;
        public float viewportScale = 1.0f;
        public bool useOcclusionMesh = true;
        public float occlusionMeshScale = 1.0f;

        public void CopyTo(XRGraphicsConfig targetConfig)
        {
            targetConfig.renderScale = this.renderScale;
            targetConfig.viewportScale = this.viewportScale;
            targetConfig.useOcclusionMesh = this.useOcclusionMesh;
            targetConfig.occlusionMeshScale = this.occlusionMeshScale;
        }

        public void SetConfig()
        { // If XR is enabled, sets XRSettings from our saved config
            if (!enabled)
                return;

            XRSettings.eyeTextureResolutionScale = renderScale;
            XRSettings.renderViewportScale = viewportScale;
            XRSettings.useOcclusionMesh = useOcclusionMesh;
            XRSettings.occlusionMaskScale = occlusionMeshScale;
        }
        public void SetViewportScale(float viewportScale)
        { // Only sets viewport- since this is probably the only thing getting updated every frame
            if (!enabled)
                return;

            XRSettings.renderViewportScale = viewportScale;
        }

        public static readonly XRGraphicsConfig s_DefaultXRConfig = new XRGraphicsConfig
        {
            renderScale = 1.0f,
            viewportScale = 1.0f,
            useOcclusionMesh = true,
            occlusionMeshScale = 1.0f,
        };

        public static XRGraphicsConfig GetActualXRSettings()
        {
            XRGraphicsConfig getXRSettings = new XRGraphicsConfig();

            if (!enabled)
            {
                return getXRSettings;
            }

            getXRSettings.renderScale = XRSettings.eyeTextureResolutionScale;
            getXRSettings.viewportScale = XRSettings.renderViewportScale;
            getXRSettings.useOcclusionMesh = XRSettings.useOcclusionMesh;
            getXRSettings.occlusionMeshScale = XRSettings.occlusionMaskScale;
            return getXRSettings;
        }

#if UNITY_EDITOR
        public static bool tryEnable
        { // TryEnable gets updated before "play" is pressed- we use this for updating GUI only. 
            get { return PlayerSettings.virtualRealitySupported; }
        }
#endif

        public static bool enabled
        { // SRP should use this to safely determine whether XR is enabled at runtime.
            get
            {
#if ENABLE_VR
                return XRSettings.enabled;
#else
                return false;
#endif
            }
        }

#if UNITY_EDITOR
        // FIXME: We should probably have StereoREnderingPath defined in UnityEngine.XR, not UnityEditor...
        public static StereoRenderingPath stereoRenderingMode
        {
            get
            {
                if (!enabled)
                {
                    return StereoRenderingPath.SinglePass;
                }
#if UNITY_2018_3_OR_NEWER
                return (StereoRenderingPath)XRSettings.stereoRenderingMode;
#else
                if (eyeTextureDesc.vrUsage == VRTextureUsage.TwoEyes)
                    return StereoRenderingPath.SinglePass;
                else if (eyeTextureDesc.dimension == UnityEngine.Rendering.TextureDimension.Tex2DArray)
                    return StereoRenderingPath.Instancing;
                else
                    return StereoRenderingPath.MultiPass;
#endif
            }
        }
#endif

        public static uint GetPixelOffset(uint eye)
        {
            if (!enabled || XRSettings.eyeTextureDesc.vrUsage != VRTextureUsage.TwoEyes)
                return 0;
            return (uint)(Mathf.CeilToInt((eye * XRSettings.eyeTextureWidth) / 2));
        }

        public static RenderTextureDescriptor eyeTextureDesc
        {
            get
            {
                if (!enabled)
                {
                    return new RenderTextureDescriptor(0, 0);
                }

                return XRSettings.eyeTextureDesc;
            }
        }

        public static int eyeTextureWidth
        {
            get
            {
                if (!enabled)
                {
                    return 0;
                }

                return XRSettings.eyeTextureWidth;
            }
        }
        public static int eyeTextureHeight
        {
            get
            {
                if (!enabled)
                {
                    return 0;
                }

                return XRSettings.eyeTextureHeight;
            }
        }
    }
}
