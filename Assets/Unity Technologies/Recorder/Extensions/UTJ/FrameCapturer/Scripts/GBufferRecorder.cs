using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace UTJ.FrameCapturer
{

    [AddComponentMenu("UTJ/FrameCapturer/GBuffer Recorder")]
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class GBufferRecorder : RecorderBase
    {
        #region inner_types
        [Serializable]
        public struct FrameBufferConponents
        {
            public bool frameBuffer;
            public bool fbColor;
            public bool fbAlpha;

            public bool GBuffer;
            public bool gbAlbedo;
            public bool gbOcclusion;
            public bool gbSpecular;
            public bool gbSmoothness;
            public bool gbNormal;
            public bool gbEmission;
            public bool gbDepth;
            public bool gbVelocity;

            public static FrameBufferConponents defaultValue
            {
                get
                {
                    var ret = new FrameBufferConponents
                    {
                        frameBuffer = true,
                        fbColor = true,
                        fbAlpha = true,
                        GBuffer = true,
                        gbAlbedo = true,
                        gbOcclusion = true,
                        gbSpecular = true,
                        gbSmoothness = true,
                        gbNormal = true,
                        gbEmission = true,
                        gbDepth = true,
                        gbVelocity = true,
                    };
                    return ret;
                }
            }
        }

        class BufferRecorder
        {
            RenderTexture m_rt;
            int m_channels;
            int m_targetFramerate = 30;
            string m_name;
            MovieEncoder m_encoder;

            public BufferRecorder(RenderTexture rt, int ch, string name, int tf)
            {
                m_rt = rt;
                m_channels = ch;
                m_name = name;
            }

            public bool Initialize(MovieEncoderConfigs c, DataPath p)
            {
                string path = p.GetFullPath() + "/" + m_name;
                c.Setup(m_rt.width, m_rt.height, m_channels, m_targetFramerate);
                m_encoder = MovieEncoder.Create(c, path);
                return m_encoder != null && m_encoder.IsValid();
            }

            public void Release()
            {
                if(m_encoder != null)
                {
                    m_encoder.Release();
                    m_encoder = null;
                }
            }

            public void Update(double time)
            {
                if (m_encoder != null)
                {
                    fcAPI.fcLock(m_rt, (data, fmt) =>
                    {
                        m_encoder.AddVideoFrame(data, fmt, time);
                    });
                }
            }
        }

        #endregion


        #region fields
        [SerializeField] MovieEncoderConfigs m_encoderConfigs = new MovieEncoderConfigs(MovieEncoder.Type.Exr);
        [SerializeField] FrameBufferConponents m_fbComponents = FrameBufferConponents.defaultValue;

        [SerializeField] Shader m_shCopy;
        Material m_matCopy;
        Mesh m_quad;
        CommandBuffer m_cbCopyFB;
        CommandBuffer m_cbCopyGB;
        CommandBuffer m_cbClearGB;
        CommandBuffer m_cbCopyVelocity;
        RenderTexture[] m_rtFB;
        RenderTexture[] m_rtGB;
        List<BufferRecorder> m_recorders = new List<BufferRecorder>();
        #endregion


        #region properties
        public FrameBufferConponents fbComponents
        {
            get { return m_fbComponents; }
            set { m_fbComponents = value; }
        }

        public MovieEncoderConfigs encoderConfigs { get { return m_encoderConfigs; } }
        #endregion



        public override bool BeginRecording()
        {
            if (m_recording) { return false; }
            if (m_shCopy == null)
            {
                Debug.LogError("GBufferRecorder: copy shader is missing!");
                return false;
            }

            m_outputDir.CreateDirectory();
            if (m_quad == null) m_quad = fcAPI.CreateFullscreenQuad();
            if (m_matCopy == null) m_matCopy = new Material(m_shCopy);

            var cam = GetComponent<Camera>();
            if (cam.targetTexture != null)
            {
                m_matCopy.EnableKeyword("OFFSCREEN");
            }
            else
            {
                m_matCopy.DisableKeyword("OFFSCREEN");
            }

            int captureWidth = cam.pixelWidth;
            int captureHeight = cam.pixelHeight;
            GetCaptureResolution(ref captureWidth, ref captureHeight);
            if (m_encoderConfigs.format == MovieEncoder.Type.MP4 ||
                m_encoderConfigs.format == MovieEncoder.Type.WebM)
            {
                captureWidth = (captureWidth + 1) & ~1;
                captureHeight = (captureHeight + 1) & ~1;
            }

            if (m_fbComponents.frameBuffer)
            {
                m_rtFB = new RenderTexture[2];
                for (int i = 0; i < m_rtFB.Length; ++i)
                {
                    m_rtFB[i] = new RenderTexture(captureWidth, captureHeight, 0, RenderTextureFormat.ARGBHalf);
                    m_rtFB[i].filterMode = FilterMode.Point;
                    m_rtFB[i].Create();
                }

                int tid = Shader.PropertyToID("_TmpFrameBuffer");
                m_cbCopyFB = new CommandBuffer();
                m_cbCopyFB.name = "GBufferRecorder: Copy FrameBuffer";
                m_cbCopyFB.GetTemporaryRT(tid, -1, -1, 0, FilterMode.Point);
                m_cbCopyFB.Blit(BuiltinRenderTextureType.CurrentActive, tid);
                m_cbCopyFB.SetRenderTarget(new RenderTargetIdentifier[] { m_rtFB[0], m_rtFB[1] }, m_rtFB[0]);
                m_cbCopyFB.DrawMesh(m_quad, Matrix4x4.identity, m_matCopy, 0, 0);
                m_cbCopyFB.ReleaseTemporaryRT(tid);
                cam.AddCommandBuffer(CameraEvent.AfterEverything, m_cbCopyFB);
            }
            if (m_fbComponents.GBuffer)
            {
                m_rtGB = new RenderTexture[8];
                for (int i = 0; i < m_rtGB.Length; ++i)
                {
                    m_rtGB[i] = new RenderTexture(captureWidth, captureHeight, 0, RenderTextureFormat.ARGBHalf);
                    m_rtGB[i].filterMode = FilterMode.Point;
                    m_rtGB[i].Create();
                }

                // clear gbuffer (Unity doesn't clear emission buffer - it is not needed usually)
                m_cbClearGB = new CommandBuffer();
                m_cbClearGB.name = "GBufferRecorder: Cleanup GBuffer";
                if (cam.allowHDR)
                {
                    m_cbClearGB.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                }
                else
                {
                    m_cbClearGB.SetRenderTarget(BuiltinRenderTextureType.GBuffer3);
                }
                m_cbClearGB.DrawMesh(m_quad, Matrix4x4.identity, m_matCopy, 0, 3);
                m_matCopy.SetColor("_ClearColor", cam.backgroundColor);

                // copy gbuffer
                m_cbCopyGB = new CommandBuffer();
                m_cbCopyGB.name = "GBufferRecorder: Copy GBuffer";
                m_cbCopyGB.SetRenderTarget(new RenderTargetIdentifier[] {
                    m_rtGB[0], m_rtGB[1], m_rtGB[2], m_rtGB[3], m_rtGB[4], m_rtGB[5], m_rtGB[6]
                }, m_rtGB[0]);
                m_cbCopyGB.DrawMesh(m_quad, Matrix4x4.identity, m_matCopy, 0, 2);
                cam.AddCommandBuffer(CameraEvent.BeforeGBuffer, m_cbClearGB);
                cam.AddCommandBuffer(CameraEvent.BeforeLighting, m_cbCopyGB);

                if (m_fbComponents.gbVelocity)
                {
                    m_cbCopyVelocity = new CommandBuffer();
                    m_cbCopyVelocity.name = "GBufferRecorder: Copy Velocity";
                    m_cbCopyVelocity.SetRenderTarget(m_rtGB[7]);
                    m_cbCopyVelocity.DrawMesh(m_quad, Matrix4x4.identity, m_matCopy, 0, 4);
                    cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_cbCopyVelocity);
                    cam.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
                }
            }

            int framerate = m_targetFramerate;
            if (m_fbComponents.frameBuffer) {
                if (m_fbComponents.fbColor) m_recorders.Add(new BufferRecorder(m_rtFB[0], 4, "FrameBuffer", framerate));
                if (m_fbComponents.fbAlpha) m_recorders.Add(new BufferRecorder(m_rtFB[1], 1, "Alpha", framerate));
            }
            if (m_fbComponents.GBuffer)
            {
                if (m_fbComponents.gbAlbedo)      { m_recorders.Add(new BufferRecorder(m_rtGB[0], 3, "Albedo", framerate)); }
                if (m_fbComponents.gbOcclusion)   { m_recorders.Add(new BufferRecorder(m_rtGB[1], 1, "Occlusion", framerate)); }
                if (m_fbComponents.gbSpecular)    { m_recorders.Add(new BufferRecorder(m_rtGB[2], 3, "Specular", framerate)); }
                if (m_fbComponents.gbSmoothness)  { m_recorders.Add(new BufferRecorder(m_rtGB[3], 1, "Smoothness", framerate)); }
                if (m_fbComponents.gbNormal)      { m_recorders.Add(new BufferRecorder(m_rtGB[4], 3, "Normal", framerate)); }
                if (m_fbComponents.gbEmission)    { m_recorders.Add(new BufferRecorder(m_rtGB[5], 3, "Emission", framerate)); }
                if (m_fbComponents.gbDepth)       { m_recorders.Add(new BufferRecorder(m_rtGB[6], 1, "Depth", framerate)); }
                if (m_fbComponents.gbVelocity)    { m_recorders.Add(new BufferRecorder(m_rtGB[7], 2, "Velocity", framerate)); }
            }
            foreach (var rec in m_recorders)
            {
                if (!rec.Initialize(m_encoderConfigs, m_outputDir))
                {
                    EndRecording();
                    return false;
                }
            }

            base.BeginRecording();

            Debug.Log("GBufferRecorder: BeginRecording()");
            return true;
        }

        public override void EndRecording()
        {
            foreach (var rec in m_recorders) { rec.Release(); }
            m_recorders.Clear();

            var cam = GetComponent<Camera>();
            if (m_cbCopyFB != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.AfterEverything, m_cbCopyFB);
                m_cbCopyFB.Release();
                m_cbCopyFB = null;
            }
            if (m_cbClearGB != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, m_cbClearGB);
                m_cbClearGB.Release();
                m_cbClearGB = null;
            }
            if (m_cbCopyGB != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeLighting, m_cbCopyGB);
                m_cbCopyGB.Release();
                m_cbCopyGB = null;
            }
            if (m_cbCopyVelocity != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, m_cbCopyVelocity);
                m_cbCopyVelocity.Release();
                m_cbCopyVelocity = null;
            }

            if (m_rtFB != null)
            {
                foreach (var rt in m_rtFB) { rt.Release(); }
                m_rtFB = null;
            }
            if (m_rtGB != null)
            {
                foreach (var rt in m_rtGB) { rt.Release(); }
                m_rtGB = null;
            }

            if (m_recording)
            {
                Debug.Log("GBufferRecorder: EndRecording()");
            }
            base.EndRecording();
        }


        #region impl
#if UNITY_EDITOR
        void Reset()
        {
            m_shCopy = fcAPI.GetFrameBufferCopyShader();
        }
#endif // UNITY_EDITOR

        IEnumerator OnPostRender()
        {
            if (m_recording)
            {
                yield return new WaitForEndOfFrame();

                //double timestamp = Time.unscaledTime - m_initialTime;
                double timestamp = 1.0 / m_targetFramerate * m_recordedFrames;
                foreach (var rec in m_recorders) { rec.Update(timestamp); }

                ++m_recordedFrames;
            }
            m_frame++;
        }
        #endregion
    }

}
