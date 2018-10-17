using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
#if UNITY_2017_2_OR_NEWER
    using XRSettings = UnityEngine.XR.XRSettings;
#elif UNITY_5_6_OR_NEWER
    using XRSettings = UnityEngine.VR.VRSettings;
#endif

    // Context object passed around all post-fx in a frame
    public sealed class PostProcessRenderContext
    {
        // -----------------------------------------------------------------------------------------
        // The following should be filled by the render pipeline

        // Camera currently rendering
        Camera m_Camera;
        public Camera camera
        {
            get { return m_Camera; }
            set
            {
                m_Camera = value;

#if !UNITY_SWITCH
                if (m_Camera.stereoEnabled)
                {
#if UNITY_2017_2_OR_NEWER
                    var xrDesc = XRSettings.eyeTextureDesc;
                    width = xrDesc.width;
                    height = xrDesc.height;
                    m_sourceDescriptor = xrDesc;
#else
                    // Single-pass is only supported with 2017.2+ because
                    // that is when XRSettings.eyeTextureDesc is available.
                    // Without it, we don't have a robust method of determining
                    // if we are in single-pass.  Users can just double the width
                    // here if they KNOW they are using single-pass.
                    width = XRSettings.eyeTextureWidth;
                    height = XRSettings.eyeTextureHeight;
#endif

                    if (m_Camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
                        xrActiveEye = (int)Camera.StereoscopicEye.Right;

                    screenWidth = XRSettings.eyeTextureWidth;
                    screenHeight = XRSettings.eyeTextureHeight;
                    stereoActive = true;
                }
                else
#endif
                {
                    width = m_Camera.pixelWidth;
                    height = m_Camera.pixelHeight;

#if UNITY_2017_2_OR_NEWER
                    m_sourceDescriptor.width = width;
                    m_sourceDescriptor.height = height;
#endif
                    screenWidth = width;
                    screenHeight = height;
                    stereoActive = false;
                }
            }
        }


        // The command buffer to fill in
        public CommandBuffer command { get; set; }

        // Source target (can't be the same as destination)
        public RenderTargetIdentifier source { get; set; }

        // Destination target (can't be the same as source)
        public RenderTargetIdentifier destination { get; set; }

        // Texture format used for the source target
        // We need this to be set explictely as we don't have any way of knowing if we're rendering
        // using  HDR or not as scriptable render pipelines may ignore the HDR toggle on camera
        // completely
        public RenderTextureFormat sourceFormat { get; set; }

        // Should we flip the last pass?
        public bool flip { get; set; }

        // -----------------------------------------------------------------------------------------
        // The following is auto-populated by the post-processing stack

        // Contains references to external resources (shaders, builtin textures...)
        public PostProcessResources resources { get; internal set; }

        // Property sheet factory handled by the currently active PostProcessLayer
        public PropertySheetFactory propertySheets { get; internal set; }

        // Custom user data objects (unused by builtin effects, feel free to store whatever you want
        // in this dictionary)
        public Dictionary<string, object> userData { get; private set; }

        // Reference to the internal debug layer
        public PostProcessDebugLayer debugLayer { get; internal set; }

        // Current camera width in pixels
        public int width { get; private set; }

        // Current camera height in pixels
        public int height { get; private set; }

        public bool stereoActive { get; private set; }

        // Current active rendering eye (for XR)
        public int xrActiveEye { get; private set; }

        // Pixel dimensions of logical screen size
        public int screenWidth { get; private set; }

        public int screenHeight { get; private set; }

        // Are we currently rendering in the scene view?
        public bool isSceneView { get; internal set; }

        // Current antialiasing method set
        public PostProcessLayer.Antialiasing antialiasing { get; internal set; }

        // Mostly used to grab the jitter vector and other TAA-related values when an effect needs
        // to do temporal reprojection (see: Depth of Field)
        public TemporalAntialiasing temporalAntialiasing { get; internal set; }

        // Internal values used for builtin effects
        // Beware, these may not have been set before a specific builtin effect has been executed
        internal PropertySheet uberSheet;
        internal Texture autoExposureTexture;
        internal LogHistogram logHistogram;
        internal Texture logLut;
        internal AutoExposure autoExposure;
        internal int bloomBufferNameID;
#if UNITY_2018_2_OR_NEWER
        internal bool physicalCamera;
#endif
        public void Reset()
        {
            m_Camera = null;
            width = 0;
            height = 0;

#if UNITY_2017_2_OR_NEWER
            m_sourceDescriptor = new RenderTextureDescriptor(0, 0);
#endif
#if UNITY_2018_2_OR_NEWER
            physicalCamera = false;
#endif
            stereoActive = false;
            xrActiveEye = (int)Camera.StereoscopicEye.Left;
            screenWidth = 0;
            screenHeight = 0;

            command = null;
            source = 0;
            destination = 0;
            sourceFormat = RenderTextureFormat.ARGB32;
            flip = false;

            resources = null;
            propertySheets = null;
            debugLayer = null;
            isSceneView = false;
            antialiasing = PostProcessLayer.Antialiasing.None;
            temporalAntialiasing = null;

            uberSheet = null;
            autoExposureTexture = null;
            logLut = null;
            autoExposure = null;
            bloomBufferNameID = -1;

            if (userData == null)
                userData = new Dictionary<string, object>();

            userData.Clear();
        }

        // Checks if TAA is enabled & supported
        public bool IsTemporalAntialiasingActive()
        {
            return antialiasing == PostProcessLayer.Antialiasing.TemporalAntialiasing
                && !isSceneView
                && temporalAntialiasing.IsSupported();
        }

        // Checks if a specific debug overlay is enabled
        public bool IsDebugOverlayEnabled(DebugOverlay overlay)
        {
            return debugLayer.debugOverlay == overlay;
        }

        // Shortcut function
        public void PushDebugOverlay(CommandBuffer cmd, RenderTargetIdentifier source, PropertySheet sheet, int pass)
        {
            debugLayer.PushDebugOverlay(cmd, source, sheet, pass);
        }

        // TODO: Change w/h name to texture w/h in order to make
        // size usages explicit
#if UNITY_2017_2_OR_NEWER
        RenderTextureDescriptor m_sourceDescriptor;
        RenderTextureDescriptor GetDescriptor(int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default)
        {
            var modifiedDesc = new RenderTextureDescriptor(m_sourceDescriptor.width, m_sourceDescriptor.height,
                                                                                m_sourceDescriptor.colorFormat, depthBufferBits);
            modifiedDesc.dimension = m_sourceDescriptor.dimension;
            modifiedDesc.volumeDepth = m_sourceDescriptor.volumeDepth;
            modifiedDesc.vrUsage = m_sourceDescriptor.vrUsage;
            modifiedDesc.msaaSamples = m_sourceDescriptor.msaaSamples;
            modifiedDesc.memoryless = m_sourceDescriptor.memoryless;

            modifiedDesc.useMipMap = m_sourceDescriptor.useMipMap;
            modifiedDesc.autoGenerateMips = m_sourceDescriptor.autoGenerateMips;
            modifiedDesc.enableRandomWrite = m_sourceDescriptor.enableRandomWrite;
            modifiedDesc.shadowSamplingMode = m_sourceDescriptor.shadowSamplingMode;

            if (colorFormat != RenderTextureFormat.Default)
                modifiedDesc.colorFormat = colorFormat;

            modifiedDesc.sRGB = readWrite != RenderTextureReadWrite.Linear;

            return modifiedDesc;
        }
#endif

        public void GetScreenSpaceTemporaryRT(CommandBuffer cmd, int nameID,
                                            int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default,
                                            FilterMode filter = FilterMode.Bilinear, int widthOverride = 0, int heightOverride = 0)
        {
#if UNITY_2017_2_OR_NEWER
            var desc = GetDescriptor(depthBufferBits, colorFormat, readWrite);
            if (widthOverride > 0)
                desc.width = widthOverride;
            if (heightOverride > 0)
                desc.height = heightOverride;

            cmd.GetTemporaryRT(nameID, desc, filter);
#else
            int actualWidth = width;
            int actualHeight = height;
            if (widthOverride > 0)
                actualWidth = widthOverride;
            if (heightOverride > 0)
                actualHeight = heightOverride;

            cmd.GetTemporaryRT(nameID, actualWidth, actualHeight, depthBufferBits, filter, colorFormat, readWrite);
            // TODO: How to handle MSAA for XR in older versions?  Query cam?
            // TODO: Pass in vrUsage into the args
#endif
        }

        public RenderTexture GetScreenSpaceTemporaryRT(int depthBufferBits = 0, RenderTextureFormat colorFormat = RenderTextureFormat.Default,
                                                        RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default, int widthOverride = 0, int heightOverride = 0)
        {
#if UNITY_2017_2_OR_NEWER
            var desc = GetDescriptor(depthBufferBits, colorFormat, readWrite);
            if (widthOverride > 0)
                desc.width = widthOverride;
            if (heightOverride > 0)
                desc.height = heightOverride;

            return RenderTexture.GetTemporary(desc);
#else
            int actualWidth = width;
            int actualHeight = height;
            if (widthOverride > 0)
                actualWidth = widthOverride;
            if (heightOverride > 0)
                actualHeight = heightOverride;

            return RenderTexture.GetTemporary(actualWidth, actualHeight, depthBufferBits, colorFormat, readWrite);
#endif
        }
    }
}
