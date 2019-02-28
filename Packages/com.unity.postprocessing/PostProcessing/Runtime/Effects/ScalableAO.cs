using System;

namespace UnityEngine.Rendering.PostProcessing
{
    // Scalable ambient obscurance
    [Serializable]
    internal sealed class ScalableAO : IAmbientOcclusionMethod
    {
        RenderTexture m_Result;
        PropertySheet m_PropertySheet;
        AmbientOcclusion m_Settings;

        readonly RenderTargetIdentifier[] m_MRT =
        {
            BuiltinRenderTextureType.GBuffer0, // Albedo, Occ
            BuiltinRenderTextureType.CameraTarget // Ambient
        };

        readonly int[] m_SampleCount = { 4, 6, 10, 8, 12 };

        enum Pass
        {
            OcclusionEstimationForward,
            OcclusionEstimationDeferred,
            HorizontalBlurForward,
            HorizontalBlurDeferred,
            VerticalBlur,
            CompositionForward,
            CompositionDeferred,
            DebugOverlay
        }

        public ScalableAO(AmbientOcclusion settings)
        {
            m_Settings = settings;
        }

        public DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
        }

        void DoLazyInitialization(PostProcessRenderContext context)
        {
            m_PropertySheet = context.propertySheets.Get(context.resources.shaders.scalableAO);

            bool reset = false;

            if (m_Result == null || !m_Result.IsCreated())
            {
                // Initial allocation
                m_Result = context.GetScreenSpaceTemporaryRT(0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                m_Result.hideFlags = HideFlags.DontSave;
                m_Result.filterMode = FilterMode.Bilinear;

                reset = true;
            }
            else if (m_Result.width != context.width || m_Result.height != context.height)
            {
                // Release and reallocate
                m_Result.Release();
                m_Result.width = context.width;
                m_Result.height = context.height;
                reset = true;
            }

            if (reset)
                m_Result.Create();
        }

        void Render(PostProcessRenderContext context, CommandBuffer cmd, int occlusionSource)
        {
            DoLazyInitialization(context);
            m_Settings.radius.value = Mathf.Max(m_Settings.radius.value, 1e-4f);

            // Material setup
            // Always use a quater-res AO buffer unless High/Ultra quality is set.
            bool downsampling = (int)m_Settings.quality.value < (int)AmbientOcclusionQuality.High;
            float px = m_Settings.intensity.value;
            float py = m_Settings.radius.value;
            float pz = downsampling ? 0.5f : 1f;
            float pw = m_SampleCount[(int)m_Settings.quality.value];

            var sheet = m_PropertySheet;
            sheet.ClearKeywords();
            sheet.properties.SetVector(ShaderIDs.AOParams, new Vector4(px, py, pz, pw));
            sheet.properties.SetVector(ShaderIDs.AOColor, Color.white - m_Settings.color.value);

            // In forward fog is applied at the object level in the grometry pass so we need to
            // apply it to AO as well or it'll drawn on top of the fog effect.
            // Not needed in Deferred.
            if (context.camera.actualRenderingPath == RenderingPath.Forward && RenderSettings.fog)
            {
                sheet.EnableKeyword("APPLY_FORWARD_FOG");
                sheet.properties.SetVector(
                    ShaderIDs.FogParams,
                    new Vector3(RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance)
                );
            }

            // Texture setup
            int ts = downsampling ? 2 : 1;
            const RenderTextureFormat kFormat = RenderTextureFormat.ARGB32;
            const RenderTextureReadWrite kRWMode = RenderTextureReadWrite.Linear;
            const FilterMode kFilter = FilterMode.Bilinear;

            // AO buffer
            var rtMask = ShaderIDs.OcclusionTexture1;
            int scaledWidth = context.width / ts;
            int scaledHeight = context.height / ts;
            context.GetScreenSpaceTemporaryRT(cmd, rtMask, 0, kFormat, kRWMode, kFilter, scaledWidth, scaledHeight);

            // AO estimation
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, rtMask, sheet, (int)Pass.OcclusionEstimationForward + occlusionSource);

            // Blur buffer
            var rtBlur = ShaderIDs.OcclusionTexture2;
            context.GetScreenSpaceTemporaryRT(cmd, rtBlur, 0, kFormat, kRWMode, kFilter);

            // Separable blur (horizontal pass)
            cmd.BlitFullscreenTriangle(rtMask, rtBlur, sheet, (int)Pass.HorizontalBlurForward + occlusionSource);
            cmd.ReleaseTemporaryRT(rtMask);

            // Separable blur (vertical pass)
            cmd.BlitFullscreenTriangle(rtBlur, m_Result, sheet, (int)Pass.VerticalBlur);
            cmd.ReleaseTemporaryRT(rtBlur);

            if (context.IsDebugOverlayEnabled(DebugOverlay.AmbientOcclusion))
                context.PushDebugOverlay(cmd, m_Result, sheet, (int)Pass.DebugOverlay);
        }

        public void RenderAfterOpaque(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Ambient Occlusion");
            Render(context, cmd, 0);
            cmd.SetGlobalTexture(ShaderIDs.SAOcclusionTexture, m_Result);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, BuiltinRenderTextureType.CameraTarget, m_PropertySheet, (int)Pass.CompositionForward, RenderBufferLoadAction.Load);
            cmd.EndSample("Ambient Occlusion");
        }

        public void RenderAmbientOnly(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Ambient Occlusion Render");
            Render(context, cmd, 1);
            cmd.EndSample("Ambient Occlusion Render");
        }

        public void CompositeAmbientOnly(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Ambient Occlusion Composite");
            cmd.SetGlobalTexture(ShaderIDs.SAOcclusionTexture, m_Result);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, m_MRT, BuiltinRenderTextureType.CameraTarget, m_PropertySheet, (int)Pass.CompositionDeferred);
            cmd.EndSample("Ambient Occlusion Composite");
        }

        public void Release()
        {
            RuntimeUtilities.Destroy(m_Result);
            m_Result = null;
        }
    }
}

