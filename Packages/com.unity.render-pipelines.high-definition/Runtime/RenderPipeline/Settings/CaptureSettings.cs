using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum CameraProjection { Perspective, Orthographic };

    [Flags]
    public enum CaptureSettingsOverrides
    {
        //CubeResolution = 1 << 0,
        //PlanarResolution = 1 << 1,
        ClearColorMode = 1 << 2,
        BackgroundColorHDR = 1 << 3,
        ClearDepth = 1 << 4,
        CullingMask = 1 << 5,
        UseOcclusionCulling = 1 << 6,
        VolumeLayerMask = 1 << 7,
        VolumeAnchorOverride = 1 << 8,
        Projection = 1 << 9,
        NearClip = 1 << 10,
        FarClip = 1 << 11,
        FieldOfview = 1 << 12,
        OrphographicSize = 1 << 13,
        RenderingPath = 1 << 14,
        //Aperture = 1 << 15,
        //ShutterSpeed = 1 << 16,
        //Iso = 1 << 17,
        ShadowDistance = 1 << 18,
    }

    [Serializable]
    public class CaptureSettings
    {
        public static CaptureSettings @default = new CaptureSettings();

        public CaptureSettingsOverrides overrides;

        public HDAdditionalCameraData.ClearColorMode clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
        [ColorUsage(true, true)]
        public Color backgroundColorHDR = new Color32(6, 18, 48, 0);
        public bool clearDepth = true;

        public LayerMask cullingMask = -1; //= 0xFFFFFFFF which is c++ default
        public bool useOcclusionCulling = true;

        public LayerMask volumeLayerMask = -1; //= 0xFFFFFFFF which is c++ default
        public Transform volumeAnchorOverride;

        public CameraProjection projection = CameraProjection.Perspective;
        public float nearClipPlane = 0.3f;
        public float farClipPlane = 1000f;
        public float fieldOfView = 90.0f;   //90f for a face of a cubemap
        public float orthographicSize = 5f;

        public HDAdditionalCameraData.RenderingPath renderingPath = HDAdditionalCameraData.RenderingPath.UseGraphicsSettings;

        //public float aperture = 8f;
        //public float shutterSpeed = 1f / 200f;
        //public float iso = 400f;

        public float shadowDistance = 100.0f;
    }
}
