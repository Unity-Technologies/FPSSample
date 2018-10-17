using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // The HDRenderPipeline assumes linear lighting. Doesn't work with gamma.
    public class HDRenderPipelineAsset : RenderPipelineAsset, ISerializationCallbackReceiver
    {
        [HideInInspector]
        const int currentVersion = 1;
        // Currently m_Version is not used and produce a warning, remove these pragmas at the next version incrementation
#pragma warning disable 414
        [SerializeField]
        int m_Version = currentVersion;
#pragma warning restore 414

        HDRenderPipelineAsset()
        {
        }

        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new HDRenderPipeline(this);
        }

        [SerializeField]
        RenderPipelineResources m_RenderPipelineResources;
        public RenderPipelineResources renderPipelineResources
        {
            get { return m_RenderPipelineResources; }
            set { m_RenderPipelineResources = value; }
        }

        // To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
        // we create a runtime copy (m_ActiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)
        [SerializeField]
        [FormerlySerializedAs("serializedFrameSettings")]
        FrameSettings m_FrameSettings = new FrameSettings(); // This are the defaultFrameSettings for all the camera and apply to sceneView, public to be visible in the inspector
        // Not serialized, not visible, the settings effectively used
        FrameSettings m_FrameSettingsRuntime = new FrameSettings();

        [SerializeField]
        FrameSettings m_BakedOrCustomReflectionFrameSettings = new FrameSettings();

        [SerializeField]
        FrameSettings m_RealtimeReflectionFrameSettings = new FrameSettings();
        
        bool m_frameSettingsIsDirty = true;
        public bool frameSettingsIsDirty
        {
            get { return m_frameSettingsIsDirty; }
        }

        public FrameSettings GetFrameSettings()
        {
            return m_FrameSettingsRuntime;
        }
        
        public FrameSettings GetBakedOrCustomReflectionFrameSettings()
        {
            return m_BakedOrCustomReflectionFrameSettings;
        }

        public FrameSettings GetRealtimeReflectionFrameSettings()
        {
            return m_RealtimeReflectionFrameSettings;
        }

        // See comment in FrameSettings.UpdateDirtyFrameSettings()
        // for detail about this function
        public void UpdateDirtyFrameSettings()
        {
            if (m_frameSettingsIsDirty)
            {
                m_FrameSettings.CopyTo(m_FrameSettingsRuntime);

                m_frameSettingsIsDirty = false;

                // In Editor we can have plenty of camera that are not render at the same time as SceneView.
                // It is really tricky to keep in sync with them. To have a coherent state. When a change is done
                // on HDRenderPipelineAsset, we tag all camera as dirty so we are sure that they will get the
                // correct default FrameSettings when the camera will be in the HDRenderPipeline.Render() call
                // otherwise, as SceneView and Game camera are not in the same call Render(), Game camera that use default
                // will not be update correctly.
                #if UNITY_EDITOR
                Camera[] cameras = Camera.allCameras;
                foreach (Camera camera in cameras)
                {
                    var additionalCameraData = camera.GetComponent<HDAdditionalCameraData>();
                    if (additionalCameraData)
                    {
                        // Call OnAfterDeserialize that set dirty on FrameSettings
                        additionalCameraData.OnAfterDeserialize();
                    }
                }
                #endif
            }
        }

        public ReflectionSystemParameters reflectionSystemParameters
        {
            get
            {
                return new ReflectionSystemParameters
                {
                    maxPlanarReflectionProbePerCamera = renderPipelineSettings.lightLoopSettings.planarReflectionProbeCacheSize,
                    maxActivePlanarReflectionProbe = 512,
                    planarReflectionProbeSize = (int)renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize,
                    maxActiveReflectionProbe = 512,
                    reflectionProbeSize = (int)renderPipelineSettings.lightLoopSettings.reflectionCubemapSize
                };
            }
        }

        // Store the various RenderPipelineSettings for each platform (for now only one)
        public RenderPipelineSettings renderPipelineSettings = new RenderPipelineSettings();

        // Return the current use RenderPipelineSettings (i.e for the current platform)
        public RenderPipelineSettings GetRenderPipelineSettings()
        {
            return renderPipelineSettings;
        }

        public bool allowShaderVariantStripping = true;
        public bool enableSRPBatcher = false;

        [SerializeField]
        public DiffusionProfileSettings diffusionProfileSettings;

        // HDRP use GetRenderingLayerMaskNames to create its light linking system
        // Mean here we define our name for light linking.
        [System.NonSerialized]
        string[] m_RenderingLayerNames = null;
        string[] renderingLayerNames
        {
            get
            {
                if (m_RenderingLayerNames == null)
                {
                    m_RenderingLayerNames = new string[32];

                    // By design we can't touch this one, but we can rename it
                    m_RenderingLayerNames[0] = "Light Layer default";

                    // We only support up to 7 layer + default.
                    for (int i = 1; i < 8; ++i)
                    {
                        m_RenderingLayerNames[i] = string.Format("Light Layer {0}", i);
                    }

                    // Unused
                    for (int i = 8; i < m_RenderingLayerNames.Length; ++i)
                    {
                        m_RenderingLayerNames[i] = string.Format("Unused {0}", i);
                    }
                }

                return m_RenderingLayerNames;
            }
        }

        public override string[] GetRenderingLayerMaskNames()
        {
            return renderingLayerNames;
        }

        public override Shader GetDefaultShader()
        {
            return m_RenderPipelineResources.shaders.defaultPS;
        }

        public override Material GetDefaultMaterial()
        {
            return m_RenderPipelineResources.materials.defaultDiffuseMat;
        }

        #if UNITY_EDITOR
        // call to GetAutodeskInteractiveShaderXXX are only from within editor
        public override Shader GetAutodeskInteractiveShader()
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/ShaderGraph/AutodeskInteractive.ShaderGraph");
        }

        public override Shader GetAutodeskInteractiveTransparentShader()
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/ShaderGraph/AutodeskInteractiveTransparent.ShaderGraph");
        }

        public override Shader GetAutodeskInteractiveMaskedShader()
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/ShaderGraph/AutodeskInteractiveMasked.ShaderGraph");
        }
        #endif

        // Note: This function is HD specific
        public Material GetDefaultDecalMaterial()
        {
            return m_RenderPipelineResources.materials.defaultDecalMat;
        }

        // Note: This function is HD specific
        public Material GetDefaultMirrorMaterial()
        {
            return m_RenderPipelineResources.materials.defaultMirrorMat;
        }

        public override Material GetDefaultParticleMaterial()
        {
            return null;
        }

        public override Material GetDefaultLineMaterial()
        {
            return null;
        }

        public override Material GetDefaultTerrainMaterial()
        {
            return m_RenderPipelineResources.materials.defaultTerrainMat;
        }

        public override Material GetDefaultUIMaterial()
        {
            return null;
        }

        public override Material GetDefaultUIOverdrawMaterial()
        {
            return null;
        }

        public override Material GetDefaultUIETC1SupportedMaterial()
        {
            return null;
        }

        public override Material GetDefault2DMaterial()
        {
            return null;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // This is call on load or when this settings are change.
            // When FrameSettings are manipulated we reset them to reflect the change, discarding all the Debug Windows change.
            // Tag as dirty so frameSettings are correctly initialize at next HDRenderPipeline.Render() call
            m_frameSettingsIsDirty = true;

            if (m_Version != currentVersion)
            {
                // Add here data migration code
                m_Version = currentVersion;
            }
        }
    }
}
