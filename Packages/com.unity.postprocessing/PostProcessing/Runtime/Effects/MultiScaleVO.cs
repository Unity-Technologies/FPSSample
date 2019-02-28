using System;

namespace UnityEngine.Rendering.PostProcessing
{
    // Multi-scale volumetric obscurance
    // TODO: Fix VR support

#if UNITY_2017_1_OR_NEWER
    [Serializable]
    internal sealed class MultiScaleVO : IAmbientOcclusionMethod
    {
        internal enum MipLevel { Original, L1, L2, L3, L4, L5, L6 }

        enum Pass
        {
            DepthCopy,
            CompositionDeferred,
            CompositionForward,
            DebugOverlay
        }

        // The arrays below are reused between frames to reduce GC allocation.
        readonly float[] m_SampleThickness =
        {
            Mathf.Sqrt(1f - 0.2f * 0.2f),
            Mathf.Sqrt(1f - 0.4f * 0.4f),
            Mathf.Sqrt(1f - 0.6f * 0.6f),
            Mathf.Sqrt(1f - 0.8f * 0.8f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.2f * 0.2f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.4f * 0.4f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.6f * 0.6f),
            Mathf.Sqrt(1f - 0.2f * 0.2f - 0.8f * 0.8f),
            Mathf.Sqrt(1f - 0.4f * 0.4f - 0.4f * 0.4f),
            Mathf.Sqrt(1f - 0.4f * 0.4f - 0.6f * 0.6f),
            Mathf.Sqrt(1f - 0.4f * 0.4f - 0.8f * 0.8f),
            Mathf.Sqrt(1f - 0.6f * 0.6f - 0.6f * 0.6f)
        };

        readonly float[] m_InvThicknessTable = new float[12];
        readonly float[] m_SampleWeightTable = new float[12];

        readonly int[] m_Widths = new int[7];
        readonly int[] m_Heights = new int[7];

        AmbientOcclusion m_Settings;
        PropertySheet m_PropertySheet;
        PostProcessResources m_Resources;

        // Can't use a temporary because we need to share it between cmdbuffers - also fixes a weird
        // command buffer warning
        RenderTexture m_AmbientOnlyAO;

        readonly RenderTargetIdentifier[] m_MRT =
        {
            BuiltinRenderTextureType.GBuffer0,    // Albedo, Occ
            BuiltinRenderTextureType.CameraTarget // Ambient
        };

        public MultiScaleVO(AmbientOcclusion settings)
        {
            m_Settings = settings;
        }

        public DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        // Special case for AO [because SRPs], please don't do this in other effects, it's bad
        // practice in this framework
        public void SetResources(PostProcessResources resources)
        {
            m_Resources = resources;
        }

        void Alloc(CommandBuffer cmd, int id, MipLevel size, RenderTextureFormat format, bool uav)
        {
            int sizeId = (int)size;
            cmd.GetTemporaryRT(id, new RenderTextureDescriptor
            {
                width = m_Widths[sizeId],
                height = m_Heights[sizeId],
                colorFormat = format,
                depthBufferBits = 0,
                volumeDepth = 1,
                autoGenerateMips = false,
                msaaSamples = 1,
                enableRandomWrite = uav,
                dimension = TextureDimension.Tex2D,
                sRGB = false
            }, FilterMode.Point);
        }

        void AllocArray(CommandBuffer cmd, int id, MipLevel size, RenderTextureFormat format, bool uav)
        {
            int sizeId = (int)size;
            cmd.GetTemporaryRT(id, new RenderTextureDescriptor
            {
                width = m_Widths[sizeId],
                height = m_Heights[sizeId],
                colorFormat = format,
                depthBufferBits = 0,
                volumeDepth = 16,
                autoGenerateMips = false,
                msaaSamples = 1,
                enableRandomWrite = uav,
                dimension = TextureDimension.Tex2DArray,
                sRGB = false
            }, FilterMode.Point);
        }

        void Release(CommandBuffer cmd, int id)
        {
            cmd.ReleaseTemporaryRT(id);
        }

        // Calculate values in _ZBuferParams (built-in shader variable)
        // We can't use _ZBufferParams in compute shaders, so this function is
        // used to give the values in it to compute shaders.
        Vector4 CalculateZBufferParams(Camera camera)
        {
            float fpn = camera.farClipPlane / camera.nearClipPlane;

            if (SystemInfo.usesReversedZBuffer)
                return new Vector4(fpn - 1f, 1f, 0f, 0f);

            return new Vector4(1f - fpn, fpn, 0f, 0f);
        }

        float CalculateTanHalfFovHeight(Camera camera)
        {
            return 1f / camera.projectionMatrix[0, 0];
        }

        Vector2 GetSize(MipLevel mip)
        {
            return new Vector2(m_Widths[(int)mip], m_Heights[(int)mip]);
        }

        Vector3 GetSizeArray(MipLevel mip)
        {
            return new Vector3(m_Widths[(int)mip], m_Heights[(int)mip], 16);
        }

        public void GenerateAOMap(CommandBuffer cmd, Camera camera, RenderTargetIdentifier destination, RenderTargetIdentifier? depthMap, bool invert, bool isMSAA)
        {
            // Base size
            m_Widths[0] = camera.pixelWidth * (RuntimeUtilities.isSinglePassStereoEnabled ? 2 : 1);
            m_Heights[0] = camera.pixelHeight;

            // L1 -> L6 sizes
            for (int i = 1; i < 7; i++)
            {
                int div = 1 << i;
                m_Widths[i]  = (m_Widths[0]  + (div - 1)) / div;
                m_Heights[i] = (m_Heights[0] + (div - 1)) / div;
            }

            // Allocate temporary textures
            PushAllocCommands(cmd, isMSAA);

            // Render logic
            PushDownsampleCommands(cmd, camera, depthMap, isMSAA);

            float tanHalfFovH = CalculateTanHalfFovHeight(camera);
            PushRenderCommands(cmd, ShaderIDs.TiledDepth1, ShaderIDs.Occlusion1, GetSizeArray(MipLevel.L3), tanHalfFovH, isMSAA);
            PushRenderCommands(cmd, ShaderIDs.TiledDepth2, ShaderIDs.Occlusion2, GetSizeArray(MipLevel.L4), tanHalfFovH, isMSAA);
            PushRenderCommands(cmd, ShaderIDs.TiledDepth3, ShaderIDs.Occlusion3, GetSizeArray(MipLevel.L5), tanHalfFovH, isMSAA);
            PushRenderCommands(cmd, ShaderIDs.TiledDepth4, ShaderIDs.Occlusion4, GetSizeArray(MipLevel.L6), tanHalfFovH, isMSAA);

            PushUpsampleCommands(cmd, ShaderIDs.LowDepth4, ShaderIDs.Occlusion4, ShaderIDs.LowDepth3,   ShaderIDs.Occlusion3, ShaderIDs.Combined3, GetSize(MipLevel.L4), GetSize(MipLevel.L3), isMSAA);
            PushUpsampleCommands(cmd, ShaderIDs.LowDepth3, ShaderIDs.Combined3,  ShaderIDs.LowDepth2,   ShaderIDs.Occlusion2, ShaderIDs.Combined2, GetSize(MipLevel.L3), GetSize(MipLevel.L2), isMSAA);
            PushUpsampleCommands(cmd, ShaderIDs.LowDepth2, ShaderIDs.Combined2,  ShaderIDs.LowDepth1,   ShaderIDs.Occlusion1, ShaderIDs.Combined1, GetSize(MipLevel.L2), GetSize(MipLevel.L1), isMSAA);
            PushUpsampleCommands(cmd, ShaderIDs.LowDepth1, ShaderIDs.Combined1,  ShaderIDs.LinearDepth, null,                 destination,         GetSize(MipLevel.L1), GetSize(MipLevel.Original), isMSAA, invert);

            // Cleanup
            PushReleaseCommands(cmd);
        }

        void PushAllocCommands(CommandBuffer cmd, bool isMSAA)
        {
            if(isMSAA)
            {
                Alloc(cmd, ShaderIDs.LinearDepth, MipLevel.Original, RenderTextureFormat.RGHalf, true);

                Alloc(cmd, ShaderIDs.LowDepth1, MipLevel.L1, RenderTextureFormat.RGFloat, true);
                Alloc(cmd, ShaderIDs.LowDepth2, MipLevel.L2, RenderTextureFormat.RGFloat, true);
                Alloc(cmd, ShaderIDs.LowDepth3, MipLevel.L3, RenderTextureFormat.RGFloat, true);
                Alloc(cmd, ShaderIDs.LowDepth4, MipLevel.L4, RenderTextureFormat.RGFloat, true);

                AllocArray(cmd, ShaderIDs.TiledDepth1, MipLevel.L3, RenderTextureFormat.RGHalf, true);
                AllocArray(cmd, ShaderIDs.TiledDepth2, MipLevel.L4, RenderTextureFormat.RGHalf, true);
                AllocArray(cmd, ShaderIDs.TiledDepth3, MipLevel.L5, RenderTextureFormat.RGHalf, true);
                AllocArray(cmd, ShaderIDs.TiledDepth4, MipLevel.L6, RenderTextureFormat.RGHalf, true);

                Alloc(cmd, ShaderIDs.Occlusion1, MipLevel.L1, RenderTextureFormat.RG16, true);
                Alloc(cmd, ShaderIDs.Occlusion2, MipLevel.L2, RenderTextureFormat.RG16, true);
                Alloc(cmd, ShaderIDs.Occlusion3, MipLevel.L3, RenderTextureFormat.RG16, true);
                Alloc(cmd, ShaderIDs.Occlusion4, MipLevel.L4, RenderTextureFormat.RG16, true);

                Alloc(cmd, ShaderIDs.Combined1, MipLevel.L1, RenderTextureFormat.RG16, true);
                Alloc(cmd, ShaderIDs.Combined2, MipLevel.L2, RenderTextureFormat.RG16, true);
                Alloc(cmd, ShaderIDs.Combined3, MipLevel.L3, RenderTextureFormat.RG16, true);
            }
            else
            {
                Alloc(cmd, ShaderIDs.LinearDepth, MipLevel.Original, RenderTextureFormat.RHalf, true);

                Alloc(cmd, ShaderIDs.LowDepth1, MipLevel.L1, RenderTextureFormat.RFloat, true);
                Alloc(cmd, ShaderIDs.LowDepth2, MipLevel.L2, RenderTextureFormat.RFloat, true);
                Alloc(cmd, ShaderIDs.LowDepth3, MipLevel.L3, RenderTextureFormat.RFloat, true);
                Alloc(cmd, ShaderIDs.LowDepth4, MipLevel.L4, RenderTextureFormat.RFloat, true);

                AllocArray(cmd, ShaderIDs.TiledDepth1, MipLevel.L3, RenderTextureFormat.RHalf, true);
                AllocArray(cmd, ShaderIDs.TiledDepth2, MipLevel.L4, RenderTextureFormat.RHalf, true);
                AllocArray(cmd, ShaderIDs.TiledDepth3, MipLevel.L5, RenderTextureFormat.RHalf, true);
                AllocArray(cmd, ShaderIDs.TiledDepth4, MipLevel.L6, RenderTextureFormat.RHalf, true);

                Alloc(cmd, ShaderIDs.Occlusion1, MipLevel.L1, RenderTextureFormat.R8, true);
                Alloc(cmd, ShaderIDs.Occlusion2, MipLevel.L2, RenderTextureFormat.R8, true);
                Alloc(cmd, ShaderIDs.Occlusion3, MipLevel.L3, RenderTextureFormat.R8, true);
                Alloc(cmd, ShaderIDs.Occlusion4, MipLevel.L4, RenderTextureFormat.R8, true);

                Alloc(cmd, ShaderIDs.Combined1, MipLevel.L1, RenderTextureFormat.R8, true);
                Alloc(cmd, ShaderIDs.Combined2, MipLevel.L2, RenderTextureFormat.R8, true);
                Alloc(cmd, ShaderIDs.Combined3, MipLevel.L3, RenderTextureFormat.R8, true);
            }
        }

        void PushDownsampleCommands(CommandBuffer cmd, Camera camera, RenderTargetIdentifier? depthMap, bool isMSAA)
        {
            RenderTargetIdentifier depthMapId;
            bool needDepthMapRelease = false;

            if (depthMap != null)
            {
                depthMapId = depthMap.Value;
            }
            else
            {
                // Make a copy of the depth texture, or reuse the resolved depth
                // buffer (it's only available in some specific situations).
                if (!RuntimeUtilities.IsResolvedDepthAvailable(camera))
                {
                    Alloc(cmd, ShaderIDs.DepthCopy, MipLevel.Original, RenderTextureFormat.RFloat, false);
                    depthMapId = new RenderTargetIdentifier(ShaderIDs.DepthCopy);
                    cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, depthMapId, m_PropertySheet, (int)Pass.DepthCopy);
                    needDepthMapRelease = true;
                }
                else
                {
                    depthMapId = BuiltinRenderTextureType.ResolvedDepth;
                }
            }

            // 1st downsampling pass.
            var cs = m_Resources.computeShaders.multiScaleAODownsample1;
            int kernel = cs.FindKernel(isMSAA ? "MultiScaleVODownsample1_MSAA" : "MultiScaleVODownsample1");

            cmd.SetComputeTextureParam(cs, kernel, "LinearZ", ShaderIDs.LinearDepth);
            cmd.SetComputeTextureParam(cs, kernel, "DS2x", ShaderIDs.LowDepth1);
            cmd.SetComputeTextureParam(cs, kernel, "DS4x", ShaderIDs.LowDepth2);
            cmd.SetComputeTextureParam(cs, kernel, "DS2xAtlas", ShaderIDs.TiledDepth1);
            cmd.SetComputeTextureParam(cs, kernel, "DS4xAtlas", ShaderIDs.TiledDepth2);
            cmd.SetComputeVectorParam(cs, "ZBufferParams", CalculateZBufferParams(camera));
            cmd.SetComputeTextureParam(cs, kernel, "Depth", depthMapId);

            cmd.DispatchCompute(cs, kernel, m_Widths[(int)MipLevel.L4], m_Heights[(int)MipLevel.L4], 1);

            if (needDepthMapRelease)
                Release(cmd, ShaderIDs.DepthCopy);

            // 2nd downsampling pass.
            cs = m_Resources.computeShaders.multiScaleAODownsample2;
            kernel = isMSAA ? cs.FindKernel("MultiScaleVODownsample2_MSAA") : cs.FindKernel("MultiScaleVODownsample2");

            cmd.SetComputeTextureParam(cs, kernel, "DS4x", ShaderIDs.LowDepth2);
            cmd.SetComputeTextureParam(cs, kernel, "DS8x", ShaderIDs.LowDepth3);
            cmd.SetComputeTextureParam(cs, kernel, "DS16x", ShaderIDs.LowDepth4);
            cmd.SetComputeTextureParam(cs, kernel, "DS8xAtlas", ShaderIDs.TiledDepth3);
            cmd.SetComputeTextureParam(cs, kernel, "DS16xAtlas", ShaderIDs.TiledDepth4);

            cmd.DispatchCompute(cs, kernel, m_Widths[(int)MipLevel.L6], m_Heights[(int)MipLevel.L6], 1);
        }

        void PushRenderCommands(CommandBuffer cmd, int source, int destination, Vector3 sourceSize, float tanHalfFovH, bool isMSAA)
        {
            // Here we compute multipliers that convert the center depth value into (the reciprocal
            // of) sphere thicknesses at each sample location. This assumes a maximum sample radius
            // of 5 units, but since a sphere has no thickness at its extent, we don't need to
            // sample that far out. Only samples whole integer offsets with distance less than 25
            // are used. This means that there is no sample at (3, 4) because its distance is
            // exactly 25 (and has a thickness of 0.)

            // The shaders are set up to sample a circular region within a 5-pixel radius.
            const float kScreenspaceDiameter = 10f;

            // SphereDiameter = CenterDepth * ThicknessMultiplier. This will compute the thickness
            // of a sphere centered at a specific depth. The ellipsoid scale can stretch a sphere
            // into an ellipsoid, which changes the characteristics of the AO.
            // TanHalfFovH: Radius of sphere in depth units if its center lies at Z = 1
            // ScreenspaceDiameter: Diameter of sample sphere in pixel units
            // ScreenspaceDiameter / BufferWidth: Ratio of the screen width that the sphere actually covers
            float thicknessMultiplier = 2f * tanHalfFovH * kScreenspaceDiameter / sourceSize.x;
            if (RuntimeUtilities.isSinglePassStereoEnabled)
                thicknessMultiplier *= 2f;

            // This will transform a depth value from [0, thickness] to [0, 1].
            float inverseRangeFactor = 1f / thicknessMultiplier;

            // The thicknesses are smaller for all off-center samples of the sphere. Compute
            // thicknesses relative to the center sample.
            for (int i = 0; i < 12; i++)
                m_InvThicknessTable[i] = inverseRangeFactor / m_SampleThickness[i];

            // These are the weights that are multiplied against the samples because not all samples
            // are equally important. The farther the sample is from the center location, the less
            // they matter. We use the thickness of the sphere to determine the weight.  The scalars
            // in front are the number of samples with this weight because we sum the samples
            // together before multiplying by the weight, so as an aggregate all of those samples
            // matter more. After generating this table, the weights are normalized.
            m_SampleWeightTable[ 0] = 4 * m_SampleThickness[ 0];    // Axial
            m_SampleWeightTable[ 1] = 4 * m_SampleThickness[ 1];    // Axial
            m_SampleWeightTable[ 2] = 4 * m_SampleThickness[ 2];    // Axial
            m_SampleWeightTable[ 3] = 4 * m_SampleThickness[ 3];    // Axial
            m_SampleWeightTable[ 4] = 4 * m_SampleThickness[ 4];    // Diagonal
            m_SampleWeightTable[ 5] = 8 * m_SampleThickness[ 5];    // L-shaped
            m_SampleWeightTable[ 6] = 8 * m_SampleThickness[ 6];    // L-shaped
            m_SampleWeightTable[ 7] = 8 * m_SampleThickness[ 7];    // L-shaped
            m_SampleWeightTable[ 8] = 4 * m_SampleThickness[ 8];    // Diagonal
            m_SampleWeightTable[ 9] = 8 * m_SampleThickness[ 9];    // L-shaped
            m_SampleWeightTable[10] = 8 * m_SampleThickness[10];    // L-shaped
            m_SampleWeightTable[11] = 4 * m_SampleThickness[11];    // Diagonal

            // Zero out the unused samples.
            // FIXME: should we support SAMPLE_EXHAUSTIVELY mode?
            m_SampleWeightTable[0] = 0;
            m_SampleWeightTable[2] = 0;
            m_SampleWeightTable[5] = 0;
            m_SampleWeightTable[7] = 0;
            m_SampleWeightTable[9] = 0;

            // Normalize the weights by dividing by the sum of all weights
            var totalWeight = 0f;

            foreach (float w in m_SampleWeightTable)
                totalWeight += w;

            for (int i = 0; i < m_SampleWeightTable.Length; i++)
                m_SampleWeightTable[i] /= totalWeight;

            // Set the arguments for the render kernel.
            var cs = m_Resources.computeShaders.multiScaleAORender;
            int kernel = isMSAA ? cs.FindKernel("MultiScaleVORender_MSAA_interleaved") : cs.FindKernel("MultiScaleVORender_interleaved");

            cmd.SetComputeFloatParams(cs, "gInvThicknessTable", m_InvThicknessTable);
            cmd.SetComputeFloatParams(cs, "gSampleWeightTable", m_SampleWeightTable);
            cmd.SetComputeVectorParam(cs, "gInvSliceDimension", new Vector2(1f / sourceSize.x, 1f / sourceSize.y));
            cmd.SetComputeVectorParam(cs, "AdditionalParams", new Vector2(-1f / m_Settings.thicknessModifier.value, m_Settings.intensity.value));
            cmd.SetComputeTextureParam(cs, kernel, "DepthTex", source);
            cmd.SetComputeTextureParam(cs, kernel, "Occlusion", destination);

            // Calculate the thread group count and add a dispatch command with them.
            uint xsize, ysize, zsize;
            cs.GetKernelThreadGroupSizes(kernel, out xsize, out ysize, out zsize);

            cmd.DispatchCompute(
                cs, kernel,
                ((int)sourceSize.x + (int)xsize - 1) / (int)xsize,
                ((int)sourceSize.y + (int)ysize - 1) / (int)ysize,
                ((int)sourceSize.z + (int)zsize - 1) / (int)zsize
            );
        }

        void PushUpsampleCommands(CommandBuffer cmd, int lowResDepth, int interleavedAO, int highResDepth, int? highResAO, RenderTargetIdentifier dest, Vector3 lowResDepthSize, Vector2 highResDepthSize, bool isMSAA, bool invert = false)
        {
            var cs = m_Resources.computeShaders.multiScaleAOUpsample;
            int kernel = 0;
            if (!isMSAA)
            {
                kernel = cs.FindKernel(highResAO == null ? invert
                    ? "MultiScaleVOUpSample_invert"
                    : "MultiScaleVOUpSample"
                    : "MultiScaleVOUpSample_blendout");
            }
            else
            {
                kernel = cs.FindKernel(highResAO == null ? invert
                ? "MultiScaleVOUpSample_MSAA_invert"
                : "MultiScaleVOUpSample_MSAA"
                : "MultiScaleVOUpSample_MSAA_blendout");
            }


            float stepSize = 1920f / lowResDepthSize.x;
            float bTolerance = 1f - Mathf.Pow(10f, m_Settings.blurTolerance.value) * stepSize;
            bTolerance *= bTolerance;
            float uTolerance = Mathf.Pow(10f, m_Settings.upsampleTolerance.value);
            float noiseFilterWeight = 1f / (Mathf.Pow(10f, m_Settings.noiseFilterTolerance.value) + uTolerance);

            cmd.SetComputeVectorParam(cs, "InvLowResolution", new Vector2(1f / lowResDepthSize.x, 1f / lowResDepthSize.y));
            cmd.SetComputeVectorParam(cs, "InvHighResolution", new Vector2(1f / highResDepthSize.x, 1f / highResDepthSize.y));
            cmd.SetComputeVectorParam(cs, "AdditionalParams", new Vector4(noiseFilterWeight, stepSize, bTolerance, uTolerance));

            cmd.SetComputeTextureParam(cs, kernel, "LoResDB", lowResDepth);
            cmd.SetComputeTextureParam(cs, kernel, "HiResDB", highResDepth);
            cmd.SetComputeTextureParam(cs, kernel, "LoResAO1", interleavedAO);

            if (highResAO != null)
                cmd.SetComputeTextureParam(cs, kernel, "HiResAO", highResAO.Value);

            cmd.SetComputeTextureParam(cs, kernel, "AoResult", dest);

            int xcount = ((int)highResDepthSize.x + 17) / 16;
            int ycount = ((int)highResDepthSize.y + 17) / 16;
            cmd.DispatchCompute(cs, kernel, xcount, ycount, 1);
        }

        void PushReleaseCommands(CommandBuffer cmd)
        {
            Release(cmd, ShaderIDs.LinearDepth);

            Release(cmd, ShaderIDs.LowDepth1);
            Release(cmd, ShaderIDs.LowDepth2);
            Release(cmd, ShaderIDs.LowDepth3);
            Release(cmd, ShaderIDs.LowDepth4);

            Release(cmd, ShaderIDs.TiledDepth1);
            Release(cmd, ShaderIDs.TiledDepth2);
            Release(cmd, ShaderIDs.TiledDepth3);
            Release(cmd, ShaderIDs.TiledDepth4);

            Release(cmd, ShaderIDs.Occlusion1);
            Release(cmd, ShaderIDs.Occlusion2);
            Release(cmd, ShaderIDs.Occlusion3);
            Release(cmd, ShaderIDs.Occlusion4);

            Release(cmd, ShaderIDs.Combined1);
            Release(cmd, ShaderIDs.Combined2);
            Release(cmd, ShaderIDs.Combined3);
        }

        void PreparePropertySheet(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(m_Resources.shaders.multiScaleAO);
            sheet.ClearKeywords();
            sheet.properties.SetVector(ShaderIDs.AOColor, Color.white - m_Settings.color.value);
            m_PropertySheet = sheet;
        }

        void CheckAOTexture(PostProcessRenderContext context)
        {
            if (m_AmbientOnlyAO == null || !m_AmbientOnlyAO.IsCreated() || m_AmbientOnlyAO.width != context.width || m_AmbientOnlyAO.height != context.height)
            {
                RuntimeUtilities.Destroy(m_AmbientOnlyAO);

                m_AmbientOnlyAO = new RenderTexture(context.width, context.height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear)
                {
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Point,
                    enableRandomWrite = true
                };
                m_AmbientOnlyAO.Create();
            }
        }

        void PushDebug(PostProcessRenderContext context)
        {
            if (context.IsDebugOverlayEnabled(DebugOverlay.AmbientOcclusion))
                context.PushDebugOverlay(context.command, m_AmbientOnlyAO, m_PropertySheet, (int)Pass.DebugOverlay);
        }

        public void RenderAfterOpaque(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Ambient Occlusion");
            SetResources(context.resources);
            PreparePropertySheet(context);
            CheckAOTexture(context);

            // In Forward mode, fog is applied at the object level in the grometry pass so we need
            // to apply it to AO as well or it'll drawn on top of the fog effect.
            if (context.camera.actualRenderingPath == RenderingPath.Forward && RenderSettings.fog)
            {
                m_PropertySheet.EnableKeyword("APPLY_FORWARD_FOG");
                m_PropertySheet.properties.SetVector(
                    ShaderIDs.FogParams,
                    new Vector3(RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance)
                );
            }

            GenerateAOMap(cmd, context.camera, m_AmbientOnlyAO, null, false, false);
            PushDebug(context);
            cmd.SetGlobalTexture(ShaderIDs.MSVOcclusionTexture, m_AmbientOnlyAO);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, BuiltinRenderTextureType.CameraTarget, m_PropertySheet, (int)Pass.CompositionForward, RenderBufferLoadAction.Load);
            cmd.EndSample("Ambient Occlusion");
        }

        public void RenderAmbientOnly(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Ambient Occlusion Render");
            SetResources(context.resources);
            PreparePropertySheet(context);
            CheckAOTexture(context);
            GenerateAOMap(cmd, context.camera, m_AmbientOnlyAO, null, false, false);
            PushDebug(context);
            cmd.EndSample("Ambient Occlusion Render");
        }

        public void CompositeAmbientOnly(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Ambient Occlusion Composite");
            cmd.SetGlobalTexture(ShaderIDs.MSVOcclusionTexture, m_AmbientOnlyAO);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, m_MRT, BuiltinRenderTextureType.CameraTarget, m_PropertySheet, (int)Pass.CompositionDeferred);
            cmd.EndSample("Ambient Occlusion Composite");
        }

        public void Release()
        {
            RuntimeUtilities.Destroy(m_AmbientOnlyAO);
            m_AmbientOnlyAO = null;
        }
    }
#else
    [Serializable]
    public sealed class MultiScaleVO : IAmbientOcclusionMethod
    {
        public MultiScaleVO(AmbientOcclusion settings)
        {
        }

        public void SetResources(PostProcessResources resources)
        {
        }

        public DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.None;
        }

        public void GenerateAOMap(CommandBuffer cmd, Camera camera, RenderTargetIdentifier destination, RenderTargetIdentifier? depthMap, bool invert, bool isMSAA)
        {
        }

        public void RenderAfterOpaque(PostProcessRenderContext context)
        {
        }

        public void RenderAmbientOnly(PostProcessRenderContext context)
        {
        }

        public void CompositeAmbientOnly(PostProcessRenderContext context)
        {
        }

        public void Release()
        {
        }
    }
#endif
}
