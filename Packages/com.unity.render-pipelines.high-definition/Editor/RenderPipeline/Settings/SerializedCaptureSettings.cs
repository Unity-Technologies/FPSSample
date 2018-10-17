using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedCaptureSettings
    {
        public SerializedProperty clearColorMode;
        public SerializedProperty backgroundColorHDR;
        public SerializedProperty clearDepth;

        public SerializedProperty cullingMask;
        public SerializedProperty useOcclusionCulling;

        public SerializedProperty volumeLayerMask;
        public SerializedProperty volumeAnchorOverride;

        public SerializedProperty projection;
        public SerializedProperty nearClipPlane;
        public SerializedProperty farClipPlane;
        public SerializedProperty fieldOfview;
        public SerializedProperty orthographicSize;

        public SerializedProperty renderingPath;

        public SerializedProperty shadowDistance;

        private SerializedProperty overrides;
        public bool overridesClearColorMode
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.ClearColorMode) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.ClearColorMode;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.ClearColorMode;
            }
        }
        public bool overridesBackgroundColorHDR
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.BackgroundColorHDR) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.BackgroundColorHDR;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.BackgroundColorHDR;
            }
        }
        public bool overridesClearDepth
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.ClearDepth) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.ClearDepth;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.ClearDepth;
            }
        }
        public bool overridesCullingMask
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.CullingMask) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.CullingMask;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.CullingMask;
            }
        }
        public bool overridesVolumeLayerMask
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.VolumeLayerMask) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.VolumeLayerMask;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.VolumeLayerMask;
            }
        }
        public bool overridesVolumeAnchorOverride
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.VolumeAnchorOverride) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.VolumeAnchorOverride;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.VolumeAnchorOverride;
            }
        }
        public bool overridesUseOcclusionCulling
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.UseOcclusionCulling) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.UseOcclusionCulling;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.UseOcclusionCulling;
            }
        }
        public bool overridesProjection
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.Projection) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.Projection;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.Projection;
            }
        }
        public bool overridesNearClip
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.NearClip) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.NearClip;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.NearClip;
            }
        }
        public bool overridesFarClip
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.FarClip) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.FarClip;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.FarClip;
            }
        }
        public bool overridesFieldOfview
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.FieldOfview) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.FieldOfview;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.FieldOfview;
            }
        }
        public bool overridesOrthographicSize
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.OrphographicSize) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.OrphographicSize;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.OrphographicSize;
            }
        }
        public bool overridesRenderingPath
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.RenderingPath) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.RenderingPath;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.RenderingPath;
            }
        }
        public bool overridesShadowDistance
        {
            get { return (overrides.intValue & (int)CaptureSettingsOverrides.ShadowDistance) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)CaptureSettingsOverrides.ShadowDistance;
                else
                    overrides.intValue &= ~(int)CaptureSettingsOverrides.ShadowDistance;
            }
        }

        public SerializedCaptureSettings(SerializedProperty root)
        {
            overrides = root.Find((CaptureSettings d) => d.overrides);

            clearColorMode = root.Find((CaptureSettings d) => d.clearColorMode);
            backgroundColorHDR = root.Find((CaptureSettings d) => d.backgroundColorHDR);
            clearDepth = root.Find((CaptureSettings d) => d.clearDepth);

            cullingMask = root.Find((CaptureSettings d) => d.cullingMask);
            useOcclusionCulling = root.Find((CaptureSettings d) => d.useOcclusionCulling);

            volumeLayerMask = root.Find((CaptureSettings d) => d.volumeLayerMask);
            volumeAnchorOverride = root.Find((CaptureSettings d) => d.volumeAnchorOverride);

            projection = root.Find((CaptureSettings d) => d.projection);
            nearClipPlane = root.Find((CaptureSettings d) => d.nearClipPlane);
            farClipPlane = root.Find((CaptureSettings d) => d.farClipPlane);
            fieldOfview = root.Find((CaptureSettings d) => d.fieldOfView);
            orthographicSize = root.Find((CaptureSettings d) => d.orthographicSize);

            renderingPath = root.Find((CaptureSettings d) => d.renderingPath);

            shadowDistance = root.Find((CaptureSettings d) => d.shadowDistance);
        }
    }
}
