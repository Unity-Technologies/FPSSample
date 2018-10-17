using System;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // Optimized version of 'DensityVolumeArtistParameters'.
    // TODO: pack better. This data structure contains a bunch of UNORMs.
    [GenerateHLSL]
    public struct DensityVolumeEngineData
    {
        public Vector3 scattering;    // [0, 1]
        public float   extinction;    // [0, 1]
        public Vector3 textureTiling;
        public int     textureIndex;
        public Vector3 textureScroll;
        public int     invertFade;    // No bool support :-(
        public Vector3 rcpPosFade;
        public float   pad1;
        public Vector3 rcpNegFade;
        public float   pad2;

        public static DensityVolumeEngineData GetNeutralValues()
        {
            DensityVolumeEngineData data;

            data.scattering    = Vector3.zero;
            data.extinction    = 0;
            data.textureIndex  = -1;
            data.textureTiling = Vector3.one;
            data.textureScroll = Vector3.zero;
            data.rcpPosFade    = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpNegFade    = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.invertFade    = 0;
            data.pad1          = 0;
            data.pad2          = 0;

            return data;
        }
    } // struct VolumeProperties

    public class VolumeRenderingUtils
    {
        public static float MeanFreePathFromExtinction(float extinction)
        {
            return 1.0f / extinction;
        }

        public static float ExtinctionFromMeanFreePath(float meanFreePath)
        {
            return 1.0f / meanFreePath;
        }

        public static Vector3 AbsorptionFromExtinctionAndScattering(float extinction, Vector3 scattering)
        {
            return new Vector3(extinction, extinction, extinction) - scattering;
        }

        public static Vector3 ScatteringFromExtinctionAndAlbedo(float extinction, Vector3 albedo)
        {
            return extinction * albedo;
        }

        public static Vector3 AlbedoFromMeanFreePathAndScattering(float meanFreePath, Vector3 scattering)
        {
            return meanFreePath * scattering;
        }
    }

    public struct DensityVolumeList
    {
        public List<OrientedBBox>      bounds;
        public List<DensityVolumeEngineData> density;
    }

    public class VolumetricLightingSystem
    {
        public enum VolumetricLightingPreset
        {
            Off,
            Medium,
            High,
            Count
        } // enum VolumetricLightingPreset

        public struct VBufferParameters
        {
            public Vector4 viewportResolution;
            public Vector2 viewportSliceCount;
            public Vector4 depthEncodingParams;
            public Vector4 depthDecodingParams;

            public VBufferParameters(Vector3Int viewportResolution, Vector2 depthRange, float depthDistributionUniformity)
            {
                int w = viewportResolution.x;
                int h = viewportResolution.y;
                int d = viewportResolution.z;


                this.viewportResolution = new Vector4(w, h, 1.0f / w, 1.0f / h);
                this.viewportSliceCount = new Vector2(d, 1.0f / d);

                float n = depthRange.x;
                float f = depthRange.y;
                float c = 2 - 2 * depthDistributionUniformity; // remap [0, 1] -> [2, 0]

                depthEncodingParams = ComputeLogarithmicDepthEncodingParams(n, f, c);
                depthDecodingParams = ComputeLogarithmicDepthDecodingParams(n, f, c);
            }

            public Vector4 ComputeUvScaleAndLimit(Vector2Int bufferSize)
            {
                // The depth is fixed for now.
                // vp_scale = vp_dim / tex_dim.
                Vector2 uvScale = new Vector2(viewportResolution.x / bufferSize.x,
                                              viewportResolution.y / bufferSize.y);

                // clamp to (vp_dim - 0.5) / tex_dim.
                Vector2 uvLimit = new Vector2((viewportResolution.x - 0.5f) / bufferSize.x,
                                              (viewportResolution.y - 0.5f) / bufferSize.y);

                return new Vector4(uvScale.x, uvScale.y, uvLimit.x, uvLimit.y);
            }

        } // struct Parameters

        public VolumetricLightingPreset preset = VolumetricLightingPreset.Off;

        static ComputeShader          m_VolumeVoxelizationCS      = null;
        static ComputeShader          m_VolumetricLightingCS      = null;

        List<OrientedBBox>            m_VisibleVolumeBounds       = null;
        List<DensityVolumeEngineData> m_VisibleVolumeData         = null;
        public const int              k_MaxVisibleVolumeCount     = 512;

        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        static ComputeBuffer          s_VisibleVolumeBoundsBuffer = null;
        static ComputeBuffer          s_VisibleVolumeDataBuffer   = null;

        // These two buffers do not depend on the frameID and are therefore shared by all views.
        RTHandleSystem.RTHandle       m_DensityBufferHandle;
        RTHandleSystem.RTHandle       m_LightingBufferHandle;

        // Is the feature globally disabled?
        bool m_SupportVolumetrics = false;

        // The history buffer starts in the uninitialized state after being created or resized,
        // and contains arbitrary data. Therefore, we must initialize it before use.
        Vector3Int m_PreviousResolutionOfHistoryBuffer = Vector3Int.zero;

        Vector4[] m_PackedCoeffs;
        ZonalHarmonicsL2 m_PhaseZH;
        Vector2[] m_xySeq;
        Vector4 m_xySeqOffset;
        // This is a sequence of 7 equidistant numbers from 1/14 to 13/14.
        // Each of them is the centroid of the interval of length 2/14.
        // They've been rearranged in a sequence of pairs {small, large}, s.t. (small + large) = 1.
        // That way, the running average position is close to 0.5.
        // | 6 | 2 | 4 | 1 | 5 | 3 | 7 |
        // |   |   |   | o |   |   |   |
        // |   | o |   | x |   |   |   |
        // |   | x |   | x |   | o |   |
        // |   | x | o | x |   | x |   |
        // |   | x | x | x | o | x |   |
        // | o | x | x | x | x | x |   |
        // | x | x | x | x | x | x | o |
        // | x | x | x | x | x | x | x |
        float[] m_zSeq = { 7.0f / 14.0f, 3.0f / 14.0f, 11.0f / 14.0f, 5.0f / 14.0f, 9.0f / 14.0f, 1.0f / 14.0f, 13.0f / 14.0f };


        public void Build(HDRenderPipelineAsset asset)
        {
            m_SupportVolumetrics = asset.renderPipelineSettings.supportVolumetrics;

            if (!m_SupportVolumetrics)
                return;

            preset = asset.renderPipelineSettings.increaseResolutionOfVolumetrics ? VolumetricLightingPreset.High :
                                                                                    VolumetricLightingPreset.Medium;

            m_VolumeVoxelizationCS = asset.renderPipelineResources.shaders.volumeVoxelizationCS;
            m_VolumetricLightingCS = asset.renderPipelineResources.shaders.volumetricLightingCS;

            m_PackedCoeffs = new Vector4[7];
            m_PhaseZH = new ZonalHarmonicsL2();
            m_PhaseZH.coeffs = new float[3];

            m_xySeq = new Vector2[7];
            m_xySeqOffset = new Vector4();

            CreateBuffers();
        }

        // RTHandleSystem API expects a function which computes the resolution. We define it here.
        Vector2Int ComputeVBufferResolutionXY(Vector2Int screenSize)
        {
            Vector3Int resolution = ComputeVBufferResolution(preset, screenSize.x, screenSize.y);

            return new Vector2Int(resolution.x, resolution.y);
        }

        // RTHandleSystem API expects a function which computes the resolution. We define it here.
        Vector2Int ComputeHistoryVBufferResolutionXY(Vector2Int screenSize)
        {
            Vector2Int resolution = ComputeVBufferResolutionXY(screenSize);

            // Since the buffers owned by the VolumetricLightingSystem may have different lifetimes compared
            // to those owned by the HDCamera, we need to make sure that the buffer resolution is the same
            // (in order to share the UV scale and the UV limit).
            if (m_LightingBufferHandle != null)
            {
                resolution.x = Math.Max(resolution.x, m_LightingBufferHandle.rt.width);
                resolution.y = Math.Max(resolution.y, m_LightingBufferHandle.rt.height);
            }

            return resolution;
        }

        // BufferedRTHandleSystem API expects an allocator function. We define it here.
        RTHandleSystem.RTHandle HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            frameIndex &= 1;

            int d = ComputeVBufferSliceCount(preset);

            return rtHandleSystem.Alloc(scaleFunc:         ComputeHistoryVBufferResolutionXY,
                slices:            d,
                dimension:         TextureDimension.Tex3D,
                colorFormat:       RenderTextureFormat.ARGBHalf,
                sRGB:              false,
                enableRandomWrite: true,
                enableMSAA:        false,
                /* useDynamicScale: true, // <- TODO */
                name: string.Format("{0}_VBufferHistory{1}", viewName, frameIndex)
                );
        }

        void CreateBuffers()
        {
            Debug.Assert(m_VolumetricLightingCS != null);

            m_VisibleVolumeBounds       = new List<OrientedBBox>();
            m_VisibleVolumeData         = new List<DensityVolumeEngineData>();
            s_VisibleVolumeBoundsBuffer = new ComputeBuffer(k_MaxVisibleVolumeCount, Marshal.SizeOf(typeof(OrientedBBox)));
            s_VisibleVolumeDataBuffer   = new ComputeBuffer(k_MaxVisibleVolumeCount, Marshal.SizeOf(typeof(DensityVolumeEngineData)));

            int d = ComputeVBufferSliceCount(preset);

            m_DensityBufferHandle = RTHandles.Alloc(scaleFunc:         ComputeVBufferResolutionXY,
                    slices:            d,
                    dimension:         TextureDimension.Tex3D,
                    colorFormat:       RenderTextureFormat.ARGBHalf,
                    sRGB:              false,
                    enableRandomWrite: true,
                    enableMSAA:        false,
                    /* useDynamicScale: true, // <- TODO */
                    name:              "VBufferDensity");

            m_LightingBufferHandle = RTHandles.Alloc(scaleFunc:         ComputeVBufferResolutionXY,
                    slices:            d,
                    dimension:         TextureDimension.Tex3D,
                    colorFormat:       RenderTextureFormat.ARGBHalf,
                    sRGB:              false,
                    enableRandomWrite: true,
                    enableMSAA:        false,
                    /* useDynamicScale: true, // <- TODO */
                    name:              "VBufferIntegral");
        }

        // For the initial allocation, no suballocation happens (the texture is full size).
        VBufferParameters ComputeVBufferParameters(HDCamera hdCamera)
        {
            Vector3Int viewportResolution = ComputeVBufferResolution(preset, hdCamera.camera.pixelWidth, hdCamera.camera.pixelHeight);

            var controller = VolumeManager.instance.stack.GetComponent<VolumetricLightingController>();

            // We must not allow the V-Buffer to extend outside of the camera's frustum.
            float n = hdCamera.camera.nearClipPlane;
            float f = hdCamera.camera.farClipPlane;

            Vector2 vBufferDepthRange = controller.depthRange.value;
            vBufferDepthRange.y = Mathf.Clamp(vBufferDepthRange.y, n, f);               // far
            vBufferDepthRange.x = Mathf.Clamp(vBufferDepthRange.x, n, vBufferDepthRange.y); // near
            float vBufferDepthDistributionUniformity = controller.depthDistributionUniformity.value;

            return new VBufferParameters(viewportResolution, vBufferDepthRange, vBufferDepthDistributionUniformity);
        }

        public void InitializePerCameraData(HDCamera hdCamera)
        {
            // Note: Here we can't test framesettings as they are not initialize yet
            // TODO: Here we allocate history even for camera that may not use volumetric
            if (!m_SupportVolumetrics)
                return;

            // Start with the same parameters for both frames. Then update them one by one every frame.
            var parameters            = ComputeVBufferParameters(hdCamera);
            hdCamera.vBufferParams    = new VBufferParameters[2];
            hdCamera.vBufferParams[0] = parameters;
            hdCamera.vBufferParams[1] = parameters;

            if (hdCamera.camera.cameraType == CameraType.Game ||
                hdCamera.camera.cameraType == CameraType.SceneView)
            {
                // We don't need reprojection for other view types, such as reflection and preview.
                hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting, HistoryBufferAllocatorFunction);
            }
        }

        // This function relies on being called once per camera per frame.
        // The results are undefined otherwise.
        public void UpdatePerCameraData(HDCamera hdCamera)
        {
            if (!hdCamera.frameSettings.enableVolumetrics)
                return;

            var parameters = ComputeVBufferParameters(hdCamera);

            // Double-buffer. I assume the cost of copying is negligible (don't want to use the frame index).
            hdCamera.vBufferParams[1] = hdCamera.vBufferParams[0];
            hdCamera.vBufferParams[0] = parameters;

            // Note: resizing of history buffer is automatic (handled by the BufferedRTHandleSystem).
        }

        void DestroyBuffers()
        {
            if (m_DensityBufferHandle != null)
                RTHandles.Release(m_DensityBufferHandle);
            if (m_LightingBufferHandle != null)
                RTHandles.Release(m_LightingBufferHandle);

            CoreUtils.SafeRelease(s_VisibleVolumeBoundsBuffer);
            CoreUtils.SafeRelease(s_VisibleVolumeDataBuffer);

            m_VisibleVolumeBounds = null;
            m_VisibleVolumeData   = null;
        }

        public void Cleanup()
        {
            // Note: No need to test for support volumetric here, we do saferelease and null assignation
            DestroyBuffers();

            m_VolumeVoxelizationCS = null;
            m_VolumetricLightingCS = null;
        }

        static int ComputeVBufferTileSize(VolumetricLightingPreset preset)
        {
            switch (preset)
            {
                case VolumetricLightingPreset.Medium:
                    return 8;
                case VolumetricLightingPreset.High:
                    return 4;
                case VolumetricLightingPreset.Off:
                    return 0;
                default:
                    Debug.Assert(false, "Encountered an unexpected VolumetricLightingPreset.");
                    return 0;
            }
        }

        static int ComputeVBufferSliceCount(VolumetricLightingPreset preset)
        {
            switch (preset)
            {
                case VolumetricLightingPreset.Medium:
                    return 64;
                case VolumetricLightingPreset.High:
                    return 128;
                case VolumetricLightingPreset.Off:
                    return 0;
                default:
                    Debug.Assert(false, "Encountered an unexpected VolumetricLightingPreset.");
                    return 0;
            }
        }

        static Vector3Int ComputeVBufferResolution(VolumetricLightingPreset preset,
            int screenWidth, int screenHeight)
        {
            int t = ComputeVBufferTileSize(preset);

            // ceil(ScreenSize / TileSize).
            int w = (screenWidth  + (t - 1)) / t;
            int h = (screenHeight + (t - 1)) / t;
            int d = ComputeVBufferSliceCount(preset);

            return new Vector3Int(w, h, d);
        }

        // See EncodeLogarithmicDepthGeneralized().
        static Vector4 ComputeLogarithmicDepthEncodingParams(float nearPlane, float farPlane, float c)
        {
            Vector4 depthParams = new Vector4();

            float n = nearPlane;
            float f = farPlane;

            c = Mathf.Max(c, 0.001f); // Avoid NaNs

            depthParams.y = 1.0f / Mathf.Log(c * (f - n) + 1, 2);
            depthParams.x = Mathf.Log(c, 2) * depthParams.y;
            depthParams.z = n - 1.0f / c; // Same
            depthParams.w = 0.0f;

            return depthParams;
        }

        // See DecodeLogarithmicDepthGeneralized().
        static Vector4 ComputeLogarithmicDepthDecodingParams(float nearPlane, float farPlane, float c)
        {
            Vector4 depthParams = new Vector4();

            float n = nearPlane;
            float f = farPlane;

            c = Mathf.Max(c, 0.001f); // Avoid NaNs

            depthParams.x = 1.0f / c;
            depthParams.y = Mathf.Log(c * (f - n) + 1, 2);
            depthParams.z = n - 1.0f / c; // Same
            depthParams.w = 0.0f;

            return depthParams;
        }

        void SetPreconvolvedAmbientLightProbe(CommandBuffer cmd, float dimmer, float anisotropy)
        {
            SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(RenderSettings.ambientProbe);
                                 probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, dimmer);
            ZonalHarmonicsL2.GetCornetteShanksPhaseFunction(m_PhaseZH, anisotropy);
            SphericalHarmonicsL2 finalSH = SphericalHarmonicMath.PremultiplyCoefficients(SphericalHarmonicMath.Convolve(probeSH, m_PhaseZH));

            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffs, finalSH);
            cmd.SetGlobalVectorArray(HDShaderIDs._AmbientProbeCoeffs, m_PackedCoeffs);
        }

        float CornetteShanksPhasePartConstant(float anisotropy)
        {
            float g = anisotropy;

            return (1.0f / (4.0f * Mathf.PI)) * 1.5f * (1.0f - g * g) / (2.0f + g * g);
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd, uint frameIndex)
        {
            var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();

            // VisualEnvironment sets global fog parameters: _GlobalAnisotropy, _GlobalScattering, _GlobalExtinction.

            if (!hdCamera.frameSettings.enableVolumetrics || visualEnvironment.fogType.value != FogType.Volumetric)
            {
                // Set the neutral black texture.
                cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting, CoreUtils.blackVolumeTexture);
                return;
            }

            // Get the interpolated anisotropy value.
            var fog = VolumeManager.instance.stack.GetComponent<VolumetricFog>();

            SetPreconvolvedAmbientLightProbe(cmd, fog.globalLightProbeDimmer, fog.anisotropy);

            var currFrameParams = hdCamera.vBufferParams[0];
            var prevFrameParams = hdCamera.vBufferParams[1];

            // All buffers are of the same size, and are resized at the same time, at the beginning of the frame.
            Vector2Int bufferSize = new Vector2Int(m_LightingBufferHandle.rt.width, m_LightingBufferHandle.rt.height);

            cmd.SetGlobalVector(HDShaderIDs._VBufferResolution,              currFrameParams.viewportResolution);
            cmd.SetGlobalVector(HDShaderIDs._VBufferSliceCount,              currFrameParams.viewportSliceCount);
            cmd.SetGlobalVector(HDShaderIDs._VBufferUvScaleAndLimit,         currFrameParams.ComputeUvScaleAndLimit(bufferSize));
            cmd.SetGlobalVector(HDShaderIDs._VBufferDepthEncodingParams,     currFrameParams.depthEncodingParams);
            cmd.SetGlobalVector(HDShaderIDs._VBufferDepthDecodingParams,     currFrameParams.depthDecodingParams);

            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevResolution,          prevFrameParams.viewportResolution);
            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevSliceCount,          prevFrameParams.viewportSliceCount);
            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevUvScaleAndLimit,     prevFrameParams.ComputeUvScaleAndLimit(bufferSize));
            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevDepthEncodingParams, prevFrameParams.depthEncodingParams);
            cmd.SetGlobalVector(HDShaderIDs._VBufferPrevDepthDecodingParams, prevFrameParams.depthDecodingParams);

            cmd.SetGlobalTexture(HDShaderIDs._VBufferLighting,                m_LightingBufferHandle);
        }

        public DensityVolumeList PrepareVisibleDensityVolumeList(HDCamera hdCamera, CommandBuffer cmd, float time)
        {
            DensityVolumeList densityVolumes = new DensityVolumeList();

            if (!hdCamera.frameSettings.enableVolumetrics)
                return densityVolumes;

            var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
            if (visualEnvironment.fogType.value != FogType.Volumetric)
                return densityVolumes;

            using (new ProfilingSample(cmd, "Prepare Visible Density Volume List"))
            {
                Vector3 camPosition = hdCamera.camera.transform.position;
                Vector3 camOffset   = Vector3.zero;// World-origin-relative

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    camOffset = camPosition; // Camera-relative
                }

                m_VisibleVolumeBounds.Clear();
                m_VisibleVolumeData.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                DensityVolume[] volumes = DensityVolumeManager.manager.PrepareDensityVolumeData(cmd, hdCamera.camera, time);

                for (int i = 0; i < Math.Min(volumes.Length, k_MaxVisibleVolumeCount); i++)
                {
                    DensityVolume volume = volumes[i];

                    // TODO: cache these?
                    var obb = OrientedBBox.Create(volume.transform);

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    // TODO: account for custom near and far planes of the V-Buffer's frustum.
                    // It's typically much shorter (along the Z axis) than the camera's frustum.
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum, 6, 8))
                    {
                        // TODO: cache these?
                        var data = volume.parameters.ConvertToEngineData();

                        m_VisibleVolumeBounds.Add(obb);
                        m_VisibleVolumeData.Add(data);
                    }
                }

                s_VisibleVolumeBoundsBuffer.SetData(m_VisibleVolumeBounds);
                s_VisibleVolumeDataBuffer.SetData(m_VisibleVolumeData);

                // Fill the struct with pointers in order to share the data with the light loop.
                densityVolumes.bounds  = m_VisibleVolumeBounds;
                densityVolumes.density = m_VisibleVolumeData;

                return densityVolumes;
            }
        }

        public void VolumeVoxelizationPass(HDCamera hdCamera, CommandBuffer cmd, uint frameIndex, DensityVolumeList densityVolumes)
        {
            if (!hdCamera.frameSettings.enableVolumetrics)
                return;

            var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
            if (visualEnvironment.fogType.value != FogType.Volumetric)
                return;

            using (new ProfilingSample(cmd, "Volume Voxelization"))
            {
                int numVisibleVolumes = m_VisibleVolumeBounds.Count;

                bool highQuality     = preset == VolumetricLightingPreset.High;
                bool enableClustered = hdCamera.frameSettings.lightLoopSettings.enableTileAndCluster;

                int kernel;

                if (highQuality)
                {
                    kernel = m_VolumeVoxelizationCS.FindKernel(enableClustered ? "VolumeVoxelizationClusteredHQ"
                                                                               : "VolumeVoxelizationBruteforceHQ");
                }
                else
                {
                    kernel = m_VolumeVoxelizationCS.FindKernel(enableClustered ? "VolumeVoxelizationClusteredMQ"
                                                                               : "VolumeVoxelizationBruteforceMQ");
                }

                var     frameParams = hdCamera.vBufferParams[0];
                Vector4 resolution  = frameParams.viewportResolution;
                float   vFoV        = hdCamera.camera.fieldOfView * Mathf.Deg2Rad;

                // Compose the matrix which allows us to compute the world space view direction.
                Matrix4x4 transform   = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(vFoV, resolution, hdCamera.viewMatrix, false);

                Texture3D volumeAtlas = DensityVolumeManager.manager.volumeAtlas.GetAtlas();
                Vector4 volumeAtlasDimensions = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

                if (volumeAtlas != null)
                {
                    volumeAtlasDimensions.x = (float)volumeAtlas.width / volumeAtlas.depth; // 1 / number of textures
                    volumeAtlasDimensions.y = volumeAtlas.width;
                    volumeAtlasDimensions.z = volumeAtlas.depth;
                    volumeAtlasDimensions.w = Mathf.Log(volumeAtlas.width, 2);              // Max LoD
                }
                else
                {
                    volumeAtlas = CoreUtils.blackVolumeTexture;
                }

                cmd.SetComputeTextureParam(m_VolumeVoxelizationCS, kernel, HDShaderIDs._VBufferDensity, m_DensityBufferHandle);
                cmd.SetComputeBufferParam(m_VolumeVoxelizationCS, kernel, HDShaderIDs._VolumeBounds,   s_VisibleVolumeBoundsBuffer);
                cmd.SetComputeBufferParam(m_VolumeVoxelizationCS, kernel, HDShaderIDs._VolumeData,     s_VisibleVolumeDataBuffer);
                cmd.SetComputeTextureParam(m_VolumeVoxelizationCS, kernel, HDShaderIDs._VolumeMaskAtlas, volumeAtlas);

                // TODO: set the constant buffer data only once.
                cmd.SetComputeMatrixParam(m_VolumeVoxelizationCS, HDShaderIDs._VBufferCoordToViewDirWS,  transform);
                cmd.SetComputeIntParam(m_VolumeVoxelizationCS, HDShaderIDs._NumVisibleDensityVolumes, numVisibleVolumes);
                cmd.SetComputeVectorParam(m_VolumeVoxelizationCS, HDShaderIDs._VolumeMaskDimensions, volumeAtlasDimensions);

                int w = (int)resolution.x;
                int h = (int)resolution.y;

                // The shader defines GROUP_SIZE_1D = 8.
                cmd.DispatchCompute(m_VolumeVoxelizationCS, kernel, (w + 7) / 8, (h + 7) / 8, 1);
            }
        }

        // Ref: https://en.wikipedia.org/wiki/Close-packing_of_equal_spheres
        // The returned {x, y} coordinates (and all spheres) are all within the (-0.5, 0.5)^2 range.
        // The pattern has been rotated by 15 degrees to maximize the resolution along X and Y:
        // https://www.desmos.com/calculator/kcpfvltz7c
        static void GetHexagonalClosePackedSpheres7(Vector2[] coords)
        {

            float r = 0.17054068870105443882f;
            float d = 2 * r;
            float s = r * Mathf.Sqrt(3);

            // Try to keep the weighted average as close to the center (0.5) as possible.
            //  (7)(5)    ( )( )    ( )( )    ( )( )    ( )( )    ( )(o)    ( )(x)    (o)(x)    (x)(x)
            // (2)(1)(3) ( )(o)( ) (o)(x)( ) (x)(x)(o) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x)
            //  (4)(6)    ( )( )    ( )( )    ( )( )    (o)( )    (x)( )    (x)(o)    (x)(x)    (x)(x)
            coords[0] = new Vector2(0,  0);
            coords[1] = new Vector2(-d,  0);
            coords[2] = new Vector2(d,  0);
            coords[3] = new Vector2(-r, -s);
            coords[4] = new Vector2(r,  s);
            coords[5] = new Vector2(r, -s);
            coords[6] = new Vector2(-r,  s);

            // Rotate the sampling pattern by 15 degrees.
            const float cos15 = 0.96592582628906828675f;
            const float sin15 = 0.25881904510252076235f;

            for (int i = 0; i < 7; i++)
            {
                Vector2 coord = coords[i];

                coords[i].x = coord.x * cos15 - coord.y * sin15;
                coords[i].y = coord.x * sin15 + coord.y * cos15;
            }
        }

        public void VolumetricLightingPass(HDCamera hdCamera, CommandBuffer cmd, uint frameIndex)
        {
            if (!hdCamera.frameSettings.enableVolumetrics)
                return;

            var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
            if (visualEnvironment.fogType.value != FogType.Volumetric)
                return;

            using (new ProfilingSample(cmd, "Volumetric Lighting"))
            {
                // Only available in the Play Mode because all the frame counters in the Edit Mode are broken.
                bool highQuality        = preset == VolumetricLightingPreset.High;
                bool enableClustered    = hdCamera.frameSettings.lightLoopSettings.enableTileAndCluster;
                bool enableReprojection = Application.isPlaying && hdCamera.camera.cameraType == CameraType.Game &&
                                          hdCamera.frameSettings.enableReprojectionForVolumetrics;

                int kernel;

                if (highQuality)
                {
                    if (enableReprojection)
                    {
                        kernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClusteredReprojHQ"
                                                                                   : "VolumetricLightingBruteforceReprojHQ");
                    }
                    else
                    {
                        kernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClusteredHQ"
                                                                                   : "VolumetricLightingBruteforceHQ");
                    }
                }
                else
                {
                    if (enableReprojection)
                    {
                        kernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClusteredReprojMQ"
                                                                                   : "VolumetricLightingBruteforceReprojMQ");
                    }
                    else
                    {
                        kernel = m_VolumetricLightingCS.FindKernel(enableClustered ? "VolumetricLightingClusteredMQ"
                                                                                   : "VolumetricLightingBruteforceMQ");
                    }
                }

                var       frameParams = hdCamera.vBufferParams[0];
                Vector4   resolution  = frameParams.viewportResolution;
                float     vFoV        = hdCamera.camera.fieldOfView * Mathf.Deg2Rad;
                // Compose the matrix which allows us to compute the world space view direction.
                Matrix4x4 transform   = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(vFoV, resolution, hdCamera.viewMatrix, false);

                GetHexagonalClosePackedSpheres7(m_xySeq);

                int sampleIndex = (int)frameIndex % 7;

                // TODO: should we somehow reorder offsets in Z based on the offset in XY? S.t. the samples more evenly cover the domain.
                // Currently, we assume that they are completely uncorrelated, but maybe we should correlate them somehow.
                m_xySeqOffset.Set(m_xySeq[sampleIndex].x, m_xySeq[sampleIndex].y, m_zSeq[sampleIndex], frameIndex);


                // Get the interpolated anisotropy value.
                var fog = VolumeManager.instance.stack.GetComponent<VolumetricFog>();

                // TODO: set 'm_VolumetricLightingPreset'.
                // TODO: set the constant buffer data only once.
                cmd.SetComputeMatrixParam( m_VolumetricLightingCS,         HDShaderIDs._VBufferCoordToViewDirWS, transform);
                cmd.SetComputeVectorParam( m_VolumetricLightingCS,         HDShaderIDs._VBufferSampleOffset,     m_xySeqOffset);
                cmd.SetComputeFloatParam(  m_VolumetricLightingCS,         HDShaderIDs._CornetteShanksConstant,  CornetteShanksPhasePartConstant(fog.anisotropy));
                cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferDensity,          m_DensityBufferHandle);  // Read
                cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingIntegral, m_LightingBufferHandle); // Write
                if (enableReprojection)
                {
                    var historyRT  = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting);
                    var feedbackRT = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting);

                    // Detect if the history buffer has been recreated or resized.
                    Vector3Int currentResolutionOfHistoryBuffer = new Vector3Int();
                    currentResolutionOfHistoryBuffer.x = historyRT.rt.width;
                    currentResolutionOfHistoryBuffer.y = historyRT.rt.height;
                    currentResolutionOfHistoryBuffer.z = historyRT.rt.volumeDepth;

                    // We allow downsizing, as this does not cause a reallocation.
                    bool validHistory = (currentResolutionOfHistoryBuffer.x <= m_PreviousResolutionOfHistoryBuffer.x &&
                                         currentResolutionOfHistoryBuffer.y <= m_PreviousResolutionOfHistoryBuffer.y &&
                                         currentResolutionOfHistoryBuffer.z <= m_PreviousResolutionOfHistoryBuffer.z);

                    cmd.SetComputeIntParam(    m_VolumetricLightingCS,         HDShaderIDs._VBufferLightingHistoryIsValid, validHistory ? 1 : 0);
                    cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingHistory,        historyRT);  // Read
                    cmd.SetComputeTextureParam(m_VolumetricLightingCS, kernel, HDShaderIDs._VBufferLightingFeedback,       feedbackRT); // Write

                    m_PreviousResolutionOfHistoryBuffer = currentResolutionOfHistoryBuffer;
                }

                int w = (int)resolution.x;
                int h = (int)resolution.y;

                // The shader defines GROUP_SIZE_1D = 8.
                cmd.DispatchCompute(m_VolumetricLightingCS, kernel, (w + 7) / 8, (h + 7) / 8, 1);
            }
        }
    } // class VolumetricLightingModule
} // namespace UnityEngine.Experimental.Rendering.HDPipeline
