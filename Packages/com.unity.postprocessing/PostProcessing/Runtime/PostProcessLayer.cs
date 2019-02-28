using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.PostProcessing
{
#if UNITY_2017_2_OR_NEWER
    using XRSettings = UnityEngine.XR.XRSettings;
#elif UNITY_5_6_OR_NEWER
    using XRSettings = UnityEngine.VR.VRSettings;
#endif

    /// <summary>
    /// This is the component responsible for rendering post-processing effects. It must be put on
    /// every camera you want post-processing to be applied to.
    /// </summary>
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    [DisallowMultipleComponent, ImageEffectAllowedInSceneView]
    [AddComponentMenu("Rendering/Post-process Layer", 1000)]
    [RequireComponent(typeof(Camera))]
    public sealed class PostProcessLayer : MonoBehaviour
    {
        /// <summary>
        /// Builtin anti-aliasing methods.
        /// </summary>
        public enum Antialiasing
        {
            /// <summary>
            /// No anti-aliasing.
            /// </summary>
            None,

            /// <summary>
            /// Fast Approximate Anti-aliasing (FXAA). Fast but low quality.
            /// </summary>
            FastApproximateAntialiasing,

            /// <summary>
            /// Subpixel Morphological Anti-aliasing (SMAA). Slower but higher quality than FXAA.
            /// </summary>
            SubpixelMorphologicalAntialiasing,

            /// <summary>
            /// Temporal Anti-aliasing (TAA). As fast as SMAA but generally higher quality. Because
            /// of it's temporal nature, it can introduce ghosting artifacts on fast moving objects
            /// in highly contrasted areas.
            /// </summary>
            TemporalAntialiasing
        }

        /// <summary>
        /// This is transform that will be drive the volume blending feature. In some cases you may
        /// want to use a transform other than the camera, e.g. for a top down game you'll want the
        /// player character to drive the blending instead of the actual camera transform.
        /// Setting this field to <c>null</c> will disable local volumes for this layer (global ones
        /// will still work).
        /// </summary>
        public Transform volumeTrigger;

        /// <summary>
        /// A mask of layers to consider for volume blending. It allows you to do volume filtering
        /// and is especially useful to optimize volume traversal. You should always have your
        /// volumes in dedicated layers instead of the default one for best performances.
        /// </summary>
        public LayerMask volumeLayer;

        /// <summary>
        /// If <c>true</c>, it will kill any invalid / NaN pixel and replace it with a black color
        /// before post-processing is applied. It's generally a good idea to keep this enabled to
        /// avoid post-processing artifacts cause by broken data in the scene.
        /// </summary>
        public bool stopNaNPropagation = true;
        public bool finalBlitToCameraTarget = true;

        /// <summary>
        /// The anti-aliasing method to use for this camera. By default it's set to <c>None</c>.
        /// </summary>
        public Antialiasing antialiasingMode = Antialiasing.None;

        /// <summary>
        /// Temporal Anti-aliasing settings for this camera.
        /// </summary>
        public TemporalAntialiasing temporalAntialiasing;

        /// <summary>
        /// Subpixel Morphological Anti-aliasing settings for this camera.
        /// </summary>
        public SubpixelMorphologicalAntialiasing subpixelMorphologicalAntialiasing;

        /// <summary>
        /// Fast Approximate Anti-aliasing settings for this camera.
        /// </summary>
        public FastApproximateAntialiasing fastApproximateAntialiasing;

        /// <summary>
        /// Fog settings for this camera.
        /// </summary>
        public Fog fog;

        Dithering dithering;

        /// <summary>
        /// The debug layer is reponsible for rendering debugging information on the screen. It will
        /// only be used if this layer is referenced in a <see cref="PostProcessDebug"/> component.
        /// </summary>
        /// <seealso cref="PostProcessDebug"/>
        public PostProcessDebugLayer debugLayer;

        [SerializeField]
        PostProcessResources m_Resources;

#pragma warning disable 169
        // UI states
        [SerializeField] bool m_ShowToolkit;
        [SerializeField] bool m_ShowCustomSorter;
#pragma warning restore 169

        /// <summary>
        /// If <c>true</c>, it will stop applying post-processing effects just before color grading
        /// is applied. This is used internally to export to EXR without color grading.
        /// </summary>
        public bool breakBeforeColorGrading = false;

        // Pre-ordered custom user effects
        // These are automatically populated and made to work properly with the serialization
        // system AND the editor. Modify at your own risk.
        [Serializable]
        public sealed class SerializedBundleRef
        {
            // We can't serialize Type so use assemblyQualifiedName instead, we only need this at
            // init time anyway so it's fine
            public string assemblyQualifiedName;

            // Not serialized, is set/reset when deserialization kicks in
            public PostProcessBundle bundle;
        }

        [SerializeField]
        List<SerializedBundleRef> m_BeforeTransparentBundles;

        [SerializeField]
        List<SerializedBundleRef> m_BeforeStackBundles;

        [SerializeField]
        List<SerializedBundleRef> m_AfterStackBundles;

        public Dictionary<PostProcessEvent, List<SerializedBundleRef>> sortedBundles { get; private set; }

        // We need to keep track of bundle initialization because for some obscure reason, on
        // assembly reload a MonoBehavior's Editor OnEnable will be called BEFORE the MonoBehavior's
        // own OnEnable... So we'll use it to pre-init bundles if the layer inspector is opened and
        // the component hasn't been enabled yet.
        public bool haveBundlesBeenInited { get; private set; }

        // Settings/Renderer bundles mapped to settings types
        Dictionary<Type, PostProcessBundle> m_Bundles;

        PropertySheetFactory m_PropertySheetFactory;
        CommandBuffer m_LegacyCmdBufferBeforeReflections;
        CommandBuffer m_LegacyCmdBufferBeforeLighting;
        CommandBuffer m_LegacyCmdBufferOpaque;
        CommandBuffer m_LegacyCmdBuffer;
        Camera m_Camera;
        PostProcessRenderContext m_CurrentContext;
        LogHistogram m_LogHistogram;

        bool m_SettingsUpdateNeeded = true;
        bool m_IsRenderingInSceneView = false;

        TargetPool m_TargetPool;

        bool m_NaNKilled = false;

        // Recycled list - used to reduce GC stress when gathering active effects in a bundle list
        // on each frame
        readonly List<PostProcessEffectRenderer> m_ActiveEffects = new List<PostProcessEffectRenderer>();
        readonly List<RenderTargetIdentifier> m_Targets = new List<RenderTargetIdentifier>();

        void OnEnable()
        {
            Init(null);

            if (!haveBundlesBeenInited)
                InitBundles();

            m_LogHistogram = new LogHistogram();
            m_PropertySheetFactory = new PropertySheetFactory();
            m_TargetPool = new TargetPool();

            debugLayer.OnEnable();

            if (RuntimeUtilities.scriptableRenderPipelineActive)
                return;

            InitLegacy();
        }

        void InitLegacy()
        {
            m_LegacyCmdBufferBeforeReflections = new CommandBuffer { name = "Deferred Ambient Occlusion" };
            m_LegacyCmdBufferBeforeLighting = new CommandBuffer { name = "Deferred Ambient Occlusion" };
            m_LegacyCmdBufferOpaque = new CommandBuffer { name = "Opaque Only Post-processing" };
            m_LegacyCmdBuffer = new CommandBuffer { name = "Post-processing" };

            m_Camera = GetComponent<Camera>();

#if !UNITY_2019_1_OR_NEWER // OnRenderImage (below) implies forceIntoRenderTexture
            m_Camera.forceIntoRenderTexture = true; // Needed when running Forward / LDR / No MSAA
#endif

            m_Camera.AddCommandBuffer(CameraEvent.BeforeReflections, m_LegacyCmdBufferBeforeReflections);
            m_Camera.AddCommandBuffer(CameraEvent.BeforeLighting, m_LegacyCmdBufferBeforeLighting);
            m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_LegacyCmdBufferOpaque);
            m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, m_LegacyCmdBuffer);
               
            // Internal context used if no SRP is set
            m_CurrentContext = new PostProcessRenderContext();
        }


#if UNITY_2019_1_OR_NEWER
        // We always use a CommandBuffer to blit to the final render target
        // OnRenderImage is used only to avoid the automatic blit from the RenderTexture of Camera.forceIntoRenderTexture to the actual target
        [ImageEffectUsesCommandBuffer]
        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (finalBlitToCameraTarget)
                RenderTexture.active = dst; // silence warning
            else
                Graphics.Blit(src, dst);
        }
#endif

        /// <summary>
        /// Initializes this layer. If you create the layer via scripting you should always call
        /// this method.
        /// </summary>
        /// <param name="resources">A reference to the resource asset</param>
        public void Init(PostProcessResources resources)
        {
            if (resources != null) m_Resources = resources;

            RuntimeUtilities.CreateIfNull(ref temporalAntialiasing);
            RuntimeUtilities.CreateIfNull(ref subpixelMorphologicalAntialiasing);
            RuntimeUtilities.CreateIfNull(ref fastApproximateAntialiasing);
            RuntimeUtilities.CreateIfNull(ref dithering);
            RuntimeUtilities.CreateIfNull(ref fog);
            RuntimeUtilities.CreateIfNull(ref debugLayer);
        }

        public void InitBundles()
        {
            if (haveBundlesBeenInited)
                return;

            // Create these lists only once, the serialization system will take over after that
            RuntimeUtilities.CreateIfNull(ref m_BeforeTransparentBundles);
            RuntimeUtilities.CreateIfNull(ref m_BeforeStackBundles);
            RuntimeUtilities.CreateIfNull(ref m_AfterStackBundles);

            // Create a bundle for each effect type
            m_Bundles = new Dictionary<Type, PostProcessBundle>();

            foreach (var type in PostProcessManager.instance.settingsTypes.Keys)
            {
                var settings = (PostProcessEffectSettings)ScriptableObject.CreateInstance(type);
                var bundle = new PostProcessBundle(settings);
                m_Bundles.Add(type, bundle);
            }

            // Update sorted lists with newly added or removed effects in the assemblies
            UpdateBundleSortList(m_BeforeTransparentBundles, PostProcessEvent.BeforeTransparent);
            UpdateBundleSortList(m_BeforeStackBundles, PostProcessEvent.BeforeStack);
            UpdateBundleSortList(m_AfterStackBundles, PostProcessEvent.AfterStack);

            // Push all sorted lists in a dictionary for easier access
            sortedBundles = new Dictionary<PostProcessEvent, List<SerializedBundleRef>>(new PostProcessEventComparer())
            {
                { PostProcessEvent.BeforeTransparent, m_BeforeTransparentBundles },
                { PostProcessEvent.BeforeStack,       m_BeforeStackBundles },
                { PostProcessEvent.AfterStack,        m_AfterStackBundles }
            };

            // Done
            haveBundlesBeenInited = true;
        }

        void UpdateBundleSortList(List<SerializedBundleRef> sortedList, PostProcessEvent evt)
        {
            // First get all effects associated with the injection point
            var effects = m_Bundles.Where(kvp => kvp.Value.attribute.eventType == evt && !kvp.Value.attribute.builtinEffect)
                                   .Select(kvp => kvp.Value)
                                   .ToList();

            // Remove types that don't exist anymore
            sortedList.RemoveAll(x =>
            {
                string searchStr = x.assemblyQualifiedName;
                return !effects.Exists(b => b.settings.GetType().AssemblyQualifiedName == searchStr);
            });

            // Add new ones
            foreach (var effect in effects)
            {
                string typeName = effect.settings.GetType().AssemblyQualifiedName;

                if (!sortedList.Exists(b => b.assemblyQualifiedName == typeName))
                {
                    var sbr = new SerializedBundleRef { assemblyQualifiedName = typeName };
                    sortedList.Add(sbr);
                }
            }

            // Link internal references
            foreach (var effect in sortedList)
            {
                string typeName = effect.assemblyQualifiedName;
                var bundle = effects.Find(b => b.settings.GetType().AssemblyQualifiedName == typeName);
                effect.bundle = bundle;
            }
        }

        void OnDisable()
        {
            // Have to check for null camera in case the user is doing back'n'forth between SRP and
            // legacy
            if (m_Camera != null)
            {
                if (m_LegacyCmdBufferBeforeReflections != null)
                    m_Camera.RemoveCommandBuffer(CameraEvent.BeforeReflections, m_LegacyCmdBufferBeforeReflections);
                if (m_LegacyCmdBufferBeforeLighting != null)
                    m_Camera.RemoveCommandBuffer(CameraEvent.BeforeLighting, m_LegacyCmdBufferBeforeLighting);
                if (m_LegacyCmdBufferOpaque != null)
                    m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_LegacyCmdBufferOpaque);
                if (m_LegacyCmdBuffer != null)
                    m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, m_LegacyCmdBuffer);
            }

            temporalAntialiasing.Release();
            m_LogHistogram.Release();

            foreach (var bundle in m_Bundles.Values)
                bundle.Release();

            m_Bundles.Clear();
            m_PropertySheetFactory.Release();

            if (debugLayer != null)
                debugLayer.OnDisable();

            // Might be an issue if several layers are blending in the same frame...
            TextureLerper.instance.Clear();

            haveBundlesBeenInited = false;
        }

        // Called everytime the user resets the component from the inspector and more importantly
        // the first time it's added to a GameObject. As we don't have added/removed event for
        // components, this will do fine
        void Reset()
        {
            volumeTrigger = transform;
        }

        void OnPreCull()
        {
            // Unused in scriptable render pipelines
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                return;

            if (m_Camera == null || m_CurrentContext == null)
                InitLegacy();

            // Resets the projection matrix from previous frame in case TAA was enabled.
            // We also need to force reset the non-jittered projection matrix here as it's not done
            // when ResetProjectionMatrix() is called and will break transparent rendering if TAA
            // is switched off and the FOV or any other camera property changes.
 
#if UNITY_2018_2_OR_NEWER
            if (!m_Camera.usePhysicalProperties)
#endif
                m_Camera.ResetProjectionMatrix();
            m_Camera.nonJitteredProjectionMatrix = m_Camera.projectionMatrix;

#if !UNITY_SWITCH
            if (m_Camera.stereoEnabled)
            {
                m_Camera.ResetStereoProjectionMatrices();
                Shader.SetGlobalFloat(ShaderIDs.RenderViewportScaleFactor, XRSettings.renderViewportScale);
            }
            else
#endif
            {
                Shader.SetGlobalFloat(ShaderIDs.RenderViewportScaleFactor, 1.0f);
            }

            BuildCommandBuffers();
        }

        void OnPreRender()
        {
            // Unused in scriptable render pipelines
            // Only needed for multi-pass stereo right eye
            if (RuntimeUtilities.scriptableRenderPipelineActive ||
                (m_Camera.stereoActiveEye != Camera.MonoOrStereoscopicEye.Right))
                return;

            BuildCommandBuffers();
        }

        static bool RequiresInitialBlit(Camera camera, PostProcessRenderContext context)
        {
#if UNITY_2019_1_OR_NEWER
            if (camera.allowMSAA) // this shouldn't be necessary, but until re-tested on older Unity versions just do the blits
                return true;
            if (RuntimeUtilities.scriptableRenderPipelineActive) // Should never be called from SRP
                return true;
              
            return false;
#else
            return true;
#endif
        }

        void UpdateSrcDstForOpaqueOnly(ref int src, ref int dst, PostProcessRenderContext context, RenderTargetIdentifier cameraTarget, int opaqueOnlyEffectsRemaining)
        {
            if (src > -1)
                context.command.ReleaseTemporaryRT(src);

            context.source = context.destination;
            src = dst;

            if (opaqueOnlyEffectsRemaining == 1)
            {
                context.destination = cameraTarget;
            }
            else
            {
                dst = m_TargetPool.Get();
                context.destination = dst;
                context.GetScreenSpaceTemporaryRT(context.command, dst, 0, context.sourceFormat);
            }
        }

        void BuildCommandBuffers()
        {
            var context = m_CurrentContext;
            var sourceFormat = m_Camera.allowHDR ? RuntimeUtilities.defaultHDRRenderTextureFormat : RenderTextureFormat.Default;

            if (!RuntimeUtilities.isFloatingPointFormat(sourceFormat))
                m_NaNKilled = true;

            context.Reset();
            context.camera = m_Camera;
            context.sourceFormat = sourceFormat;

            // TODO: Investigate retaining command buffers on XR multi-pass right eye
            m_LegacyCmdBufferBeforeReflections.Clear();
            m_LegacyCmdBufferBeforeLighting.Clear();
            m_LegacyCmdBufferOpaque.Clear();
            m_LegacyCmdBuffer.Clear();

            SetupContext(context);

            context.command = m_LegacyCmdBufferOpaque;
            TextureLerper.instance.BeginFrame(context);
            UpdateVolumeSystem(context.camera, context.command);

            // Lighting & opaque-only effects
            var aoBundle = GetBundle<AmbientOcclusion>();
            var aoSettings = aoBundle.CastSettings<AmbientOcclusion>();
            var aoRenderer = aoBundle.CastRenderer<AmbientOcclusionRenderer>();

            bool aoSupported = aoSettings.IsEnabledAndSupported(context);
            bool aoAmbientOnly = aoRenderer.IsAmbientOnly(context);
            bool isAmbientOcclusionDeferred = aoSupported && aoAmbientOnly;
            bool isAmbientOcclusionOpaque = aoSupported && !aoAmbientOnly;

            var ssrBundle = GetBundle<ScreenSpaceReflections>();
            var ssrSettings = ssrBundle.settings;
            var ssrRenderer = ssrBundle.renderer;
            bool isScreenSpaceReflectionsActive = ssrSettings.IsEnabledAndSupported(context);

            // Ambient-only AO is a special case and has to be done in separate command buffers
            if (isAmbientOcclusionDeferred)
            {
                var ao = aoRenderer.Get();

                // Render as soon as possible - should be done async in SRPs when available
                context.command = m_LegacyCmdBufferBeforeReflections;
                ao.RenderAmbientOnly(context);

                // Composite with GBuffer right before the lighting pass
                context.command = m_LegacyCmdBufferBeforeLighting;
                ao.CompositeAmbientOnly(context);
            }
            else if (isAmbientOcclusionOpaque)
            {
                context.command = m_LegacyCmdBufferOpaque;
                aoRenderer.Get().RenderAfterOpaque(context);
            }

            bool isFogActive = fog.IsEnabledAndSupported(context);
            bool hasCustomOpaqueOnlyEffects = HasOpaqueOnlyEffects(context);
            int opaqueOnlyEffects = 0;
            opaqueOnlyEffects += isScreenSpaceReflectionsActive ? 1 : 0;
            opaqueOnlyEffects += isFogActive ? 1 : 0;
            opaqueOnlyEffects += hasCustomOpaqueOnlyEffects ? 1 : 0;

            // This works on right eye because it is resolved/populated at runtime
            var cameraTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

            if (opaqueOnlyEffects > 0)
            {
                var cmd = m_LegacyCmdBufferOpaque;
                context.command = cmd;
                context.source = cameraTarget;
                context.destination = cameraTarget;
                int srcTarget = -1;
                int dstTarget = -1;

                UpdateSrcDstForOpaqueOnly(ref srcTarget, ref dstTarget, context, cameraTarget, opaqueOnlyEffects + 1); // + 1 for blit

                if (RequiresInitialBlit(m_Camera, context) || opaqueOnlyEffects == 1)
                {
                    cmd.BuiltinBlit(context.source, context.destination, RuntimeUtilities.copyStdMaterial, stopNaNPropagation ? 1 : 0);
                    UpdateSrcDstForOpaqueOnly(ref srcTarget, ref dstTarget, context, cameraTarget, opaqueOnlyEffects);
                }
 
                if (isScreenSpaceReflectionsActive)
                {
                    ssrRenderer.Render(context);
                    opaqueOnlyEffects--;
                    UpdateSrcDstForOpaqueOnly(ref srcTarget, ref dstTarget, context, cameraTarget, opaqueOnlyEffects);
                }

                if (isFogActive)
                {
                    fog.Render(context);
                    opaqueOnlyEffects--;
                    UpdateSrcDstForOpaqueOnly(ref srcTarget, ref dstTarget, context, cameraTarget, opaqueOnlyEffects);
                }

                if (hasCustomOpaqueOnlyEffects)
                    RenderOpaqueOnly(context);

                cmd.ReleaseTemporaryRT(srcTarget);
            }
            
            // Post-transparency stack
            int tempRt = -1;
            bool forceNanKillPass = (!m_NaNKilled && stopNaNPropagation && RuntimeUtilities.isFloatingPointFormat(sourceFormat));
            if (RequiresInitialBlit(m_Camera, context) || forceNanKillPass)
            {
                tempRt = m_TargetPool.Get();
                context.GetScreenSpaceTemporaryRT(m_LegacyCmdBuffer, tempRt, 0, sourceFormat, RenderTextureReadWrite.sRGB);
                m_LegacyCmdBuffer.BuiltinBlit(cameraTarget, tempRt, RuntimeUtilities.copyStdMaterial, stopNaNPropagation ? 1 : 0);
                if (!m_NaNKilled)
                    m_NaNKilled = stopNaNPropagation;

                context.source = tempRt;
            }
            else
            {
                context.source = cameraTarget;
            }

            context.destination = cameraTarget;

#if UNITY_2019_1_OR_NEWER
            if (finalBlitToCameraTarget && !RuntimeUtilities.scriptableRenderPipelineActive)
            {
                if (m_Camera.targetTexture)
                {
                    context.destination = m_Camera.targetTexture.colorBuffer;
                }
                else
                {
                    context.flip = true;
                    context.destination = Display.main.colorBuffer;
                }
            } 
#endif

            context.command = m_LegacyCmdBuffer;

            Render(context);

            if (tempRt > -1)
                m_LegacyCmdBuffer.ReleaseTemporaryRT(tempRt);
        }

        void OnPostRender()
        {
            // Unused in scriptable render pipelines
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                return;

            if (m_CurrentContext.IsTemporalAntialiasingActive())
            {
#if UNITY_2018_2_OR_NEWER
                // TAA calls SetProjectionMatrix so if the camera projection mode was physical, it gets set to explicit. So we set it back to physical.
                if (m_CurrentContext.physicalCamera)   
                    m_Camera.usePhysicalProperties = true;
                else 
#endif
                    m_Camera.ResetProjectionMatrix();

                if (m_CurrentContext.stereoActive)
                {
                    if (RuntimeUtilities.isSinglePassStereoEnabled || m_Camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
                        m_Camera.ResetStereoProjectionMatrices();
                }
            }
        }

        public PostProcessBundle GetBundle<T>()
            where T : PostProcessEffectSettings
        {
            return GetBundle(typeof(T));
        }

        public PostProcessBundle GetBundle(Type settingsType)
        {
            Assert.IsTrue(m_Bundles.ContainsKey(settingsType), "Invalid type");
            return m_Bundles[settingsType];
        }

        /// <summary>
        /// Gets the current settings for a given effect.
        /// </summary>
        /// <typeparam name="T">The type of effect to look for</typeparam>
        /// <returns>The current state of an effect</returns>
        public T GetSettings<T>()
            where T : PostProcessEffectSettings
        {
            return GetBundle<T>().CastSettings<T>();
        }

		/// <summary>
        /// Utility method to bake a multi-scale volumetric obscurance map for the current camera.
        /// This will only work if ambient occlusion is active in the scene.
        /// </summary>
        /// <param name="cmd">The command buffer to use for rendering steps</param>
        /// <param name="camera">The camera to render ambient occlusion for</param>
        /// <param name="destination">The destination render target</param>
        /// <param name="depthMap">The depth map to use. If <c>null</c>, it will use the depth map
        /// from the given camera</param>
        /// <param name="invert">Should the result be inverted?</param>
        /// <param name="isMSAA">Should use MSAA?</param>
        public void BakeMSVOMap(CommandBuffer cmd, Camera camera, RenderTargetIdentifier destination, RenderTargetIdentifier? depthMap, bool invert, bool isMSAA = false)
        {
            var bundle = GetBundle<AmbientOcclusion>();
            var renderer = bundle.CastRenderer<AmbientOcclusionRenderer>().GetMultiScaleVO();
            renderer.SetResources(m_Resources);
            renderer.GenerateAOMap(cmd, camera, destination, depthMap, invert, isMSAA);
        }

        internal void OverrideSettings(List<PostProcessEffectSettings> baseSettings, float interpFactor)
        {
            // Go through all settings & overriden parameters for the given volume and lerp values
            foreach (var settings in baseSettings)
            {
                if (!settings.active)
                    continue;

                var target = GetBundle(settings.GetType()).settings;
                int count = settings.parameters.Count;

                for (int i = 0; i < count; i++)
                {
                    var toParam = settings.parameters[i];
                    if (toParam.overrideState)
                    {
                        var fromParam = target.parameters[i];
                        fromParam.Interp(fromParam, toParam, interpFactor);
                    }
                }
            }
        }

        // In the legacy render loop you have to explicitely set flags on camera to tell that you
        // need depth, depth+normals or motion vectors... This won't have any effect with most
        // scriptable render pipelines.
        void SetLegacyCameraFlags(PostProcessRenderContext context)
        {
            var flags = context.camera.depthTextureMode;

            foreach (var bundle in m_Bundles)
            {
                if (bundle.Value.settings.IsEnabledAndSupported(context))
                    flags |= bundle.Value.renderer.GetCameraFlags();
            }

            // Special case for AA & lighting effects
            if (context.IsTemporalAntialiasingActive())
                flags |= temporalAntialiasing.GetCameraFlags();

            if (fog.IsEnabledAndSupported(context))
                flags |= fog.GetCameraFlags();

            if (debugLayer.debugOverlay != DebugOverlay.None)
                flags |= debugLayer.GetCameraFlags();

            context.camera.depthTextureMode = flags;
        }

        /// <summary>
        /// This method should be called whenever you need to reset any temporal effect, e.g. when
        /// doing camera cuts.
        /// </summary>
        public void ResetHistory()
        {
            foreach (var bundle in m_Bundles)
                bundle.Value.ResetHistory();

            temporalAntialiasing.ResetHistory();
        }

        /// <summary>
        /// Checks if this layer has any active opaque-only effect.
        /// </summary>
        /// <param name="context">The current render context</param>
        /// <returns><c>true</c> if opaque-only effects are active, <c>false</c> otherwise</returns>
        public bool HasOpaqueOnlyEffects(PostProcessRenderContext context)
        {
            return HasActiveEffects(PostProcessEvent.BeforeTransparent, context);
        }

        /// <summary>
        /// Checks if this layer has any active effect at the given injection point.
        /// </summary>
        /// <param name="evt">The injection point to look for</param>
        /// <param name="context">The current render context</param>
        /// <returns><c>true</c> if any effect at the given injection point is active, <c>false</c>
        /// otherwise</returns>
        public bool HasActiveEffects(PostProcessEvent evt, PostProcessRenderContext context)
        {
            var list = sortedBundles[evt];

            foreach (var item in list)
            {
                if (item.bundle.settings.IsEnabledAndSupported(context))
                    return true;
            }

            return false;
        }

        void SetupContext(PostProcessRenderContext context)
        {
            RuntimeUtilities.s_Resources = m_Resources;

            m_IsRenderingInSceneView = context.camera.cameraType == CameraType.SceneView;
            context.isSceneView = m_IsRenderingInSceneView;
            context.resources = m_Resources;
            context.propertySheets = m_PropertySheetFactory;
            context.debugLayer = debugLayer;
            context.antialiasing = antialiasingMode;
            context.temporalAntialiasing = temporalAntialiasing;
            context.logHistogram = m_LogHistogram;

#if UNITY_2018_2_OR_NEWER
            context.physicalCamera = context.camera.usePhysicalProperties;
#endif

            SetLegacyCameraFlags(context);

            // Prepare debug overlay
            debugLayer.SetFrameSize(context.width, context.height);

            // Unsafe to keep this around but we need it for OnGUI events for debug views
            // Will be removed eventually
            m_CurrentContext = context;
        }

        /// <summary>
        /// Updates the state of the volume system. This should be called before any other
        /// post-processing method when running in a scriptable render pipeline. You don't need to
        /// call this method when running in one of the builtin pipelines.
        /// </summary>
        /// <param name="cam">The currently rendering camera.</param>
        /// <param name="cmd">A command buffer to fill.</param>
        public void UpdateVolumeSystem(Camera cam, CommandBuffer cmd)
        {
            if (m_SettingsUpdateNeeded)
            {
                cmd.BeginSample("VolumeBlending");
                PostProcessManager.instance.UpdateSettings(this, cam);
                cmd.EndSample("VolumeBlending");
                m_TargetPool.Reset();

                // TODO: fix me once VR support is in SRP
                // Needed in SRP so that _RenderViewportScaleFactor isn't 0
                if (RuntimeUtilities.scriptableRenderPipelineActive)
                    Shader.SetGlobalFloat(ShaderIDs.RenderViewportScaleFactor, 1f);
            }

            m_SettingsUpdateNeeded = false;
        }

        /// <summary>
        /// Renders effects in the <see cref="PostProcessEvent.BeforeTransparent"/> bucket. You
        /// should call <see cref="HasOpaqueOnlyEffects"/> before calling this method as it won't
        /// automatically blit source into destination if no opaque-only effect is active.
        /// </summary>
        /// <param name="context">The current post-processing context.</param>
        public void RenderOpaqueOnly(PostProcessRenderContext context)
        {
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                SetupContext(context);

            TextureLerper.instance.BeginFrame(context);

            // Update & override layer settings first (volume blending), will only be done once per
            // frame, either here or in Render() if there isn't any opaque-only effect to render.
            // TODO: should be removed, keeping this here for older SRPs
            UpdateVolumeSystem(context.camera, context.command);

            RenderList(sortedBundles[PostProcessEvent.BeforeTransparent], context, "OpaqueOnly");
        }

        /// <summary>
        /// Renders all effects not in the <see cref="PostProcessEvent.BeforeTransparent"/> bucket.
        /// </summary>
        /// <param name="context">The current post-processing context.</param>
        public void Render(PostProcessRenderContext context)
        {
            if (RuntimeUtilities.scriptableRenderPipelineActive)
                SetupContext(context);

            TextureLerper.instance.BeginFrame(context);
            var cmd = context.command;

            // Update & override layer settings first (volume blending) if the opaque only pass
            // hasn't been called this frame.
            // TODO: should be removed, keeping this here for older SRPs
            UpdateVolumeSystem(context.camera, context.command);

            // Do a NaN killing pass if needed
            int lastTarget = -1;
            RenderTargetIdentifier cameraTexture = context.source;

#if UNITY_2019_1_OR_NEWER
            if (context.stereoActive && context.numberOfEyes > 1 && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass)
            {
                cmd.SetSinglePassStereo(SinglePassStereoMode.None);
                cmd.DisableShaderKeyword("UNITY_SINGLE_PASS_STEREO");
            }
#endif

            for (int eye = 0; eye < context.numberOfEyes; eye++)
            {
                bool preparedStereoSource = false;

                if (stopNaNPropagation && !m_NaNKilled)
                {
                    lastTarget = m_TargetPool.Get();
                    context.GetScreenSpaceTemporaryRT(cmd, lastTarget, 0, context.sourceFormat);
                    if (context.stereoActive && context.numberOfEyes > 1)
                    {
                        if (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePassInstanced)
                        {
                            cmd.BlitFullscreenTriangleFromTexArray(context.source, lastTarget, RuntimeUtilities.copyFromTexArraySheet, 1, false, eye);
                            preparedStereoSource = true;
                        }
                        else if (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass)
                        {
                            cmd.BlitFullscreenTriangleFromDoubleWide(context.source, lastTarget, RuntimeUtilities.copyStdFromDoubleWideMaterial, 1, eye);
                            preparedStereoSource = true;
                        }
                    }
                    else
                        cmd.BlitFullscreenTriangle(context.source, lastTarget, RuntimeUtilities.copySheet, 1);
                    context.source = lastTarget;
                    m_NaNKilled = true;
                }

                if (!preparedStereoSource && context.numberOfEyes > 1)
                {
                    lastTarget = m_TargetPool.Get();
                    context.GetScreenSpaceTemporaryRT(cmd, lastTarget, 0, context.sourceFormat);
                    if (context.stereoActive)
                    {
                        if (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePassInstanced)
                        {
                            cmd.BlitFullscreenTriangleFromTexArray(context.source, lastTarget, RuntimeUtilities.copyFromTexArraySheet, 1, false, eye);
                            preparedStereoSource = true;
                        }
                        else if (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass)
                        {
                            cmd.BlitFullscreenTriangleFromDoubleWide(context.source, lastTarget, RuntimeUtilities.copyStdFromDoubleWideMaterial, stopNaNPropagation ? 1 : 0, eye);
                            preparedStereoSource = true;
                        }
                    }
                    context.source = lastTarget;
                }

                // Do temporal anti-aliasing first
                if (context.IsTemporalAntialiasingActive())
                {
                    if (!RuntimeUtilities.scriptableRenderPipelineActive)
                    {
                        if (context.stereoActive)
                        {
                            // We only need to configure all of this once for stereo, during OnPreCull
                            if (context.camera.stereoActiveEye != Camera.MonoOrStereoscopicEye.Right)
                                temporalAntialiasing.ConfigureStereoJitteredProjectionMatrices(context);
                        }
                        else
                        {
                            temporalAntialiasing.ConfigureJitteredProjectionMatrix(context);
                        }
                    }

                    var taaTarget = m_TargetPool.Get();
                    var finalDestination = context.destination;
                    context.GetScreenSpaceTemporaryRT(cmd, taaTarget, 0, context.sourceFormat);
                    context.destination = taaTarget;
                    temporalAntialiasing.Render(context);
                    context.source = taaTarget;
                    context.destination = finalDestination;

                    if (lastTarget > -1)
                        cmd.ReleaseTemporaryRT(lastTarget);

                    lastTarget = taaTarget;
                }

                bool hasBeforeStackEffects = HasActiveEffects(PostProcessEvent.BeforeStack, context);
                bool hasAfterStackEffects = HasActiveEffects(PostProcessEvent.AfterStack, context) && !breakBeforeColorGrading;
                bool needsFinalPass = (hasAfterStackEffects
                    || (antialiasingMode == Antialiasing.FastApproximateAntialiasing) || (antialiasingMode == Antialiasing.SubpixelMorphologicalAntialiasing && subpixelMorphologicalAntialiasing.IsSupported()))
                    && !breakBeforeColorGrading;

                // Right before the builtin stack
                if (hasBeforeStackEffects)
                    lastTarget = RenderInjectionPoint(PostProcessEvent.BeforeStack, context, "BeforeStack", lastTarget);

                // Builtin stack
                lastTarget = RenderBuiltins(context, !needsFinalPass, lastTarget, eye);

                // After the builtin stack but before the final pass (before FXAA & Dithering)
                if (hasAfterStackEffects)
                    lastTarget = RenderInjectionPoint(PostProcessEvent.AfterStack, context, "AfterStack", lastTarget);

                // And close with the final pass
                if (needsFinalPass)
                    RenderFinalPass(context, lastTarget, eye);

                if (context.stereoActive)
                    context.source = cameraTexture;
            }

#if UNITY_2019_1_OR_NEWER
            if (context.stereoActive && context.numberOfEyes > 1 && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass)
            {
                cmd.SetSinglePassStereo(SinglePassStereoMode.SideBySide);
                cmd.EnableShaderKeyword("UNITY_SINGLE_PASS_STEREO");
            }
#endif

            // Render debug monitors & overlay if requested
            debugLayer.RenderSpecialOverlays(context);
            debugLayer.RenderMonitors(context);

            // End frame cleanup
            TextureLerper.instance.EndFrame();
            debugLayer.EndFrame();
            m_SettingsUpdateNeeded = true;
            m_NaNKilled = false;
        }

        int RenderInjectionPoint(PostProcessEvent evt, PostProcessRenderContext context, string marker, int releaseTargetAfterUse = -1)
        {
            int tempTarget = m_TargetPool.Get();
            var finalDestination = context.destination;

            var cmd = context.command;
            context.GetScreenSpaceTemporaryRT(cmd, tempTarget, 0, context.sourceFormat);
            context.destination = tempTarget;
            RenderList(sortedBundles[evt], context, marker);
            context.source = tempTarget;
            context.destination = finalDestination;

            if (releaseTargetAfterUse > -1)
                cmd.ReleaseTemporaryRT(releaseTargetAfterUse);

            return tempTarget;
        }

        void RenderList(List<SerializedBundleRef> list, PostProcessRenderContext context, string marker)
        {
            var cmd = context.command;
            cmd.BeginSample(marker);

            // First gather active effects - we need this to manage render targets more efficiently
            m_ActiveEffects.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var effect = list[i].bundle;
                if (effect.settings.IsEnabledAndSupported(context))
                {
                    if (!context.isSceneView || (context.isSceneView && effect.attribute.allowInSceneView))
                        m_ActiveEffects.Add(effect.renderer);
                }
            }

            int count = m_ActiveEffects.Count;

            // If there's only one active effect, we can simply execute it and skip the rest
            if (count == 1)
            {
                m_ActiveEffects[0].Render(context);
            }
            else
            {
                // Else create the target chain
                m_Targets.Clear();
                m_Targets.Add(context.source); // First target is always source

                int tempTarget1 = m_TargetPool.Get();
                int tempTarget2 = m_TargetPool.Get();

                for (int i = 0; i < count - 1; i++)
                    m_Targets.Add(i % 2 == 0 ? tempTarget1 : tempTarget2);

                m_Targets.Add(context.destination); // Last target is always destination

                // Render
                context.GetScreenSpaceTemporaryRT(cmd, tempTarget1, 0, context.sourceFormat);
                if (count > 2)
                    context.GetScreenSpaceTemporaryRT(cmd, tempTarget2, 0, context.sourceFormat);

                for (int i = 0; i < count; i++)
                {
                    context.source = m_Targets[i];
                    context.destination = m_Targets[i + 1];
                    m_ActiveEffects[i].Render(context);
                }

                cmd.ReleaseTemporaryRT(tempTarget1);
                if (count > 2)
                    cmd.ReleaseTemporaryRT(tempTarget2);
            }

            cmd.EndSample(marker);
        }

        void ApplyFlip(PostProcessRenderContext context, MaterialPropertyBlock properties)
        {
            if (context.flip && !context.isSceneView)
                properties.SetVector(ShaderIDs.UVTransform, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
            else
                ApplyDefaultFlip(properties);
        }

        void ApplyDefaultFlip(MaterialPropertyBlock properties)
        {
            properties.SetVector(ShaderIDs.UVTransform, SystemInfo.graphicsUVStartsAtTop ? new Vector4(1.0f, -1.0f, 0.0f, 1.0f) : new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
        }

        int RenderBuiltins(PostProcessRenderContext context, bool isFinalPass, int releaseTargetAfterUse = -1, int eye = -1)
        {
            var uberSheet = context.propertySheets.Get(context.resources.shaders.uber);
            uberSheet.ClearKeywords();
            uberSheet.properties.Clear();
            context.uberSheet = uberSheet;
            context.autoExposureTexture = RuntimeUtilities.whiteTexture;
            context.bloomBufferNameID = -1;

            if (isFinalPass && context.stereoActive && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePassInstanced)
                uberSheet.EnableKeyword("STEREO_INSTANCING_ENABLED");

            var cmd = context.command;
            cmd.BeginSample("BuiltinStack");

            int tempTarget = -1;
            var finalDestination = context.destination;

            if (!isFinalPass)
            {
                // Render to an intermediate target as this won't be the final pass
                tempTarget = m_TargetPool.Get();
                context.GetScreenSpaceTemporaryRT(cmd, tempTarget, 0, context.sourceFormat);
                context.destination = tempTarget;

                // Handle FXAA's keep alpha mode
                if (antialiasingMode == Antialiasing.FastApproximateAntialiasing && !fastApproximateAntialiasing.keepAlpha)
                    uberSheet.properties.SetFloat(ShaderIDs.LumaInAlpha, 1f);
            }

            // Depth of field final combination pass used to be done in Uber which led to artifacts
            // when used at the same time as Bloom (because both effects used the same source, so
            // the stronger bloom was, the more DoF was eaten away in out of focus areas)
            int depthOfFieldTarget = RenderEffect<DepthOfField>(context, true);

            // Motion blur is a separate pass - could potentially be done after DoF depending on the
            // kind of results you're looking for...
            int motionBlurTarget = RenderEffect<MotionBlur>(context, true);

            // Prepare exposure histogram if needed
            if (ShouldGenerateLogHistogram(context))
                m_LogHistogram.Generate(context);

            // Uber effects
            RenderEffect<AutoExposure>(context);
            uberSheet.properties.SetTexture(ShaderIDs.AutoExposureTex, context.autoExposureTexture);

            RenderEffect<LensDistortion>(context);
            RenderEffect<ChromaticAberration>(context);
            RenderEffect<Bloom>(context);
            RenderEffect<Vignette>(context);
            RenderEffect<Grain>(context);

            if (!breakBeforeColorGrading)
                RenderEffect<ColorGrading>(context);

            if (isFinalPass)
            {
                uberSheet.EnableKeyword("FINALPASS");
                dithering.Render(context);
                ApplyFlip(context, uberSheet.properties);
            }
            else
            {
                ApplyDefaultFlip(uberSheet.properties);
            }

            if (context.stereoActive && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePassInstanced)
            {
                uberSheet.properties.SetFloat(ShaderIDs.DepthSlice, eye);
                cmd.BlitFullscreenTriangleToTexArray(context.source, context.destination, uberSheet, 0, false, eye);
            }
            else if (isFinalPass && context.stereoActive && context.numberOfEyes > 1 && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass)
            {
                cmd.BlitFullscreenTriangleToDoubleWide(context.source, context.destination, uberSheet, 0, eye);
            }
            else if (isFinalPass)
                cmd.BlitFullscreenTriangle(context.source, context.destination, uberSheet, 0, false, context.camera.pixelRect);
            else
                cmd.BlitFullscreenTriangle(context.source, context.destination, uberSheet, 0);

            context.source = context.destination;
            context.destination = finalDestination;

            if (releaseTargetAfterUse > -1) cmd.ReleaseTemporaryRT(releaseTargetAfterUse);
            if (motionBlurTarget > -1) cmd.ReleaseTemporaryRT(motionBlurTarget);
            if (depthOfFieldTarget > -1) cmd.ReleaseTemporaryRT(depthOfFieldTarget);
            if (context.bloomBufferNameID > -1) cmd.ReleaseTemporaryRT(context.bloomBufferNameID);

            cmd.EndSample("BuiltinStack");

            return tempTarget;
        }

        // This pass will have to be disabled for HDR screen output as it's an LDR pass
        void RenderFinalPass(PostProcessRenderContext context, int releaseTargetAfterUse = -1, int eye = -1)
        {
            var cmd = context.command;
            cmd.BeginSample("FinalPass");

            if (breakBeforeColorGrading)
            {
                var sheet = context.propertySheets.Get(context.resources.shaders.discardAlpha);
                if (context.stereoActive && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePassInstanced)
                    sheet.EnableKeyword("STEREO_INSTANCING_ENABLED");

                if (context.stereoActive && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePassInstanced)
                {
                    sheet.properties.SetFloat(ShaderIDs.DepthSlice, eye);
                    cmd.BlitFullscreenTriangleToTexArray(context.source, context.destination, sheet, 0, false, eye);
                }
                else if (context.stereoActive && context.numberOfEyes > 1 && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass)
                {
                    cmd.BlitFullscreenTriangleToDoubleWide(context.source, context.destination, sheet, 0, eye);
                }
                else
                    cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            }
            else
            {
                var uberSheet = context.propertySheets.Get(context.resources.shaders.finalPass);
                uberSheet.ClearKeywords();
                uberSheet.properties.Clear();
                context.uberSheet = uberSheet;
                int tempTarget = -1;

                if (context.stereoActive && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePassInstanced)
                    uberSheet.EnableKeyword("STEREO_INSTANCING_ENABLED");

                if (antialiasingMode == Antialiasing.FastApproximateAntialiasing)
                {
                    uberSheet.EnableKeyword(fastApproximateAntialiasing.fastMode
                        ? "FXAA_LOW"
                        : "FXAA"
                    );

                    if (fastApproximateAntialiasing.keepAlpha)
                        uberSheet.EnableKeyword("FXAA_KEEP_ALPHA");
                }
                else if (antialiasingMode == Antialiasing.SubpixelMorphologicalAntialiasing && subpixelMorphologicalAntialiasing.IsSupported())
                {
                    tempTarget = m_TargetPool.Get();
                    var finalDestination = context.destination;
                    context.GetScreenSpaceTemporaryRT(context.command, tempTarget, 0, context.sourceFormat);
                    context.destination = tempTarget;
                    subpixelMorphologicalAntialiasing.Render(context);
                    context.source = tempTarget;
                    context.destination = finalDestination;
                }

                dithering.Render(context);

                ApplyFlip(context, uberSheet.properties);
                if (context.stereoActive && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePassInstanced)
                {
                    uberSheet.properties.SetFloat(ShaderIDs.DepthSlice, eye);
                    cmd.BlitFullscreenTriangleToTexArray(context.source, context.destination, uberSheet, 0, false, eye);
                }
                else if (context.stereoActive && context.numberOfEyes > 1 && context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass)
                {
                    cmd.BlitFullscreenTriangleToDoubleWide(context.source, context.destination, uberSheet, 0, eye);
                }
                else
                    cmd.BlitFullscreenTriangle(context.source, context.destination, uberSheet, 0, false, context.camera.pixelRect);

                if (tempTarget > -1)
                    cmd.ReleaseTemporaryRT(tempTarget);
            }

            if (releaseTargetAfterUse > -1)
                cmd.ReleaseTemporaryRT(releaseTargetAfterUse);

            cmd.EndSample("FinalPass");
        }

        int RenderEffect<T>(PostProcessRenderContext context, bool useTempTarget = false)
            where T : PostProcessEffectSettings
        {
            var effect = GetBundle<T>();

            if (!effect.settings.IsEnabledAndSupported(context))
                return -1;

            if (m_IsRenderingInSceneView && !effect.attribute.allowInSceneView)
                return -1;

            if (!useTempTarget)
            {
                effect.renderer.Render(context);
                return -1;
            }

            var finalDestination = context.destination;
            var tempTarget = m_TargetPool.Get();
            context.GetScreenSpaceTemporaryRT(context.command, tempTarget, 0, context.sourceFormat);
            context.destination = tempTarget;
            effect.renderer.Render(context);
            context.source = tempTarget;
            context.destination = finalDestination;
            return tempTarget;
        }

        bool ShouldGenerateLogHistogram(PostProcessRenderContext context)
        {
            bool autoExpo = GetBundle<AutoExposure>().settings.IsEnabledAndSupported(context);
            bool lightMeter = debugLayer.lightMeter.IsRequestedAndSupported(context);
            return autoExpo || lightMeter;
        }
    }
}
