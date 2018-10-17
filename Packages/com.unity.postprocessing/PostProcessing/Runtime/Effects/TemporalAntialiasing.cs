using System;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    public sealed class TemporalAntialiasing
    {
        [Tooltip("The diameter (in texels) inside which jitter samples are spread. Smaller values result in crisper but more aliased output, while larger values result in more stable but blurrier output.")]
        [Range(0.1f, 1f)]
        public float jitterSpread = 0.75f;

        [Tooltip("Controls the amount of sharpening applied to the color buffer. High values may introduce dark-border artifacts.")]
        [Range(0f, 3f)]
        public float sharpness = 0.25f;

        [Tooltip("The blend coefficient for a stationary fragment. Controls the percentage of history sample blended into the final color.")]
        [Range(0f, 0.99f)]
        public float stationaryBlending = 0.95f;

        [Tooltip("The blend coefficient for a fragment with significant motion. Controls the percentage of history sample blended into the final color.")]
        [Range(0f, 0.99f)]
        public float motionBlending = 0.85f;

        // For custom jittered matrices - use at your own risks
        public Func<Camera, Vector2, Matrix4x4> jitteredMatrixFunc;

        public Vector2 jitter { get; private set; }

        enum Pass
        {
            SolverDilate,
            SolverNoDilate
        }

        readonly RenderTargetIdentifier[] m_Mrt = new RenderTargetIdentifier[2];
        bool m_ResetHistory = true;

        const int k_SampleCount = 8;
        public int sampleIndex { get; private set; }

        // Ping-pong between two history textures as we can't read & write the same target in the
        // same pass
        const int k_NumEyes = 2;
        const int k_NumHistoryTextures = 2;
        readonly RenderTexture[][] m_HistoryTextures = new RenderTexture[k_NumEyes][];

        int[] m_HistoryPingPong = new int [k_NumEyes];

        public bool IsSupported()
        {
            return SystemInfo.supportedRenderTargetCount >= 2
                && SystemInfo.supportsMotionVectors
#if !UNITY_2017_3_OR_NEWER
                && !RuntimeUtilities.isVREnabled
#endif
                && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;
        }

        internal DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        }

        internal void ResetHistory()
        {
            m_ResetHistory = true;
        }

        Vector2 GenerateRandomOffset()
        {
            // The variance between 0 and the actual halton sequence values reveals noticeable instability
            // in Unity's shadow maps, so we avoid index 0.
            var offset = new Vector2(
                    HaltonSeq.Get((sampleIndex & 1023) + 1, 2) - 0.5f,
                    HaltonSeq.Get((sampleIndex & 1023) + 1, 3) - 0.5f
                );

            if (++sampleIndex >= k_SampleCount)
                sampleIndex = 0;

            return offset;
        }

        public Matrix4x4 GetJitteredProjectionMatrix(Camera camera)
        {
            Matrix4x4 cameraProj;
            jitter = GenerateRandomOffset();
            jitter *= jitterSpread;

            if (jitteredMatrixFunc != null)
            {
                cameraProj = jitteredMatrixFunc(camera, jitter);
            }
            else
            {
                cameraProj = camera.orthographic
                    ? RuntimeUtilities.GetJitteredOrthographicProjectionMatrix(camera, jitter)
                    : RuntimeUtilities.GetJitteredPerspectiveProjectionMatrix(camera, jitter);
            }

            jitter = new Vector2(jitter.x / camera.pixelWidth, jitter.y / camera.pixelHeight);
            return cameraProj;
        }

        public void ConfigureJitteredProjectionMatrix(PostProcessRenderContext context)
        {
            var camera = context.camera;
            camera.nonJitteredProjectionMatrix = camera.projectionMatrix;
            camera.projectionMatrix = GetJitteredProjectionMatrix(camera);
            camera.useJitteredProjectionMatrixForTransparentRendering = false;
        }

        // TODO: We'll probably need to isolate most of this for SRPs
        public void ConfigureStereoJitteredProjectionMatrices(PostProcessRenderContext context)
        {
#if  UNITY_2017_3_OR_NEWER
            var camera = context.camera;
            jitter = GenerateRandomOffset();
            jitter *= jitterSpread;

            for (var eye = Camera.StereoscopicEye.Left; eye <= Camera.StereoscopicEye.Right; eye++)
            {
                // This saves off the device generated projection matrices as non-jittered
                context.camera.CopyStereoDeviceProjectionMatrixToNonJittered(eye);
                var originalProj = context.camera.GetStereoNonJitteredProjectionMatrix(eye);

                // Currently no support for custom jitter func, as VR devices would need to provide
                // original projection matrix as input along with jitter 
                var jitteredMatrix = RuntimeUtilities.GenerateJitteredProjectionMatrixFromOriginal(context, originalProj, jitter);
                context.camera.SetStereoProjectionMatrix(eye, jitteredMatrix);
            }

            // jitter has to be scaled for the actual eye texture size, not just the intermediate texture size
            // which could be double-wide in certain stereo rendering scenarios
            jitter = new Vector2(jitter.x / context.screenWidth, jitter.y / context.screenHeight);
            camera.useJitteredProjectionMatrixForTransparentRendering = false;
#endif
        }

        void GenerateHistoryName(RenderTexture rt, int id, PostProcessRenderContext context)
        {
            rt.name = "Temporal Anti-aliasing History id #" + id;

            if (context.stereoActive)
                rt.name += " for eye " + context.xrActiveEye;
        }

        RenderTexture CheckHistory(int id, PostProcessRenderContext context)
        {
            int activeEye = context.xrActiveEye;

            if (m_HistoryTextures[activeEye] == null)
                m_HistoryTextures[activeEye] = new RenderTexture[k_NumHistoryTextures];

            var rt = m_HistoryTextures[activeEye][id];

            if (m_ResetHistory || rt == null || !rt.IsCreated())
            {
                RenderTexture.ReleaseTemporary(rt);

                rt = context.GetScreenSpaceTemporaryRT(0, context.sourceFormat);
                GenerateHistoryName(rt, id, context);

                rt.filterMode = FilterMode.Bilinear;
                m_HistoryTextures[activeEye][id] = rt;

                context.command.BlitFullscreenTriangle(context.source, rt);
            }
            else if (rt.width != context.width || rt.height != context.height)
            {
                // On size change, simply copy the old history to the new one. This looks better
                // than completely discarding the history and seeing a few aliased frames.
                var rt2 = context.GetScreenSpaceTemporaryRT(0, context.sourceFormat);
                GenerateHistoryName(rt2, id, context);

                rt2.filterMode = FilterMode.Bilinear;
                m_HistoryTextures[activeEye][id] = rt2;

                context.command.BlitFullscreenTriangle(rt, rt2);
                RenderTexture.ReleaseTemporary(rt);
            }

            return m_HistoryTextures[activeEye][id];
        }

        internal void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(context.resources.shaders.temporalAntialiasing);

            var cmd = context.command;
            cmd.BeginSample("TemporalAntialiasing");

            int pp = m_HistoryPingPong[context.xrActiveEye];
            var historyRead = CheckHistory(++pp % 2, context);
            var historyWrite = CheckHistory(++pp % 2, context);
            m_HistoryPingPong[context.xrActiveEye] = ++pp % 2;

            const float kMotionAmplification = 100f * 60f;
            sheet.properties.SetVector(ShaderIDs.Jitter, jitter);
            sheet.properties.SetFloat(ShaderIDs.Sharpness, sharpness);
            sheet.properties.SetVector(ShaderIDs.FinalBlendParameters, new Vector4(stationaryBlending, motionBlending, kMotionAmplification, 0f));
            sheet.properties.SetTexture(ShaderIDs.HistoryTex, historyRead);

            // TODO: Account for different possible RenderViewportScale value from previous frame...

            int pass = context.camera.orthographic ? (int)Pass.SolverNoDilate : (int)Pass.SolverDilate;
            m_Mrt[0] = context.destination;
            m_Mrt[1] = historyWrite;

            cmd.BlitFullscreenTriangle(context.source, m_Mrt, context.source, sheet, pass);
            cmd.EndSample("TemporalAntialiasing");

            m_ResetHistory = false;
        }

        internal void Release()
        {
            if (m_HistoryTextures != null)
            {
                for (int i = 0; i < m_HistoryTextures.Length; i++)
                {
                    if (m_HistoryTextures[i] == null)
                        continue;
                    
                    for (int j = 0; j < m_HistoryTextures[i].Length; j++)
                    {
                        RenderTexture.ReleaseTemporary(m_HistoryTextures[i][j]);
                        m_HistoryTextures[i][j] = null;
                    }

                    m_HistoryTextures[i] = null;
                }
            }

            sampleIndex = 0;
            m_HistoryPingPong[0] = 0;
            m_HistoryPingPong[1] = 0;
            
            ResetHistory();
        }
    }
}
