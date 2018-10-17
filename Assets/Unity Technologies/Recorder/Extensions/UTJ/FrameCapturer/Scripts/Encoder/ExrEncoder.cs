using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace UTJ.FrameCapturer
{
    public class ExrEncoder : MovieEncoder
    {
        static readonly string[] s_channelNames = { "R", "G", "B", "A" };
        fcAPI.fcExrContext m_ctx;
        fcAPI.fcExrConfig m_config;
        string m_outPath;
        int m_frame;

        public override void Release() { m_ctx.Release(); }
        public override bool IsValid() { return m_ctx; }
        public override Type type { get { return Type.Exr; } }

        public override void Initialize(object config, string outPath)
        {
            if (!fcAPI.fcExrIsSupported())
            {
                Debug.LogError("Exr encoder is not available on this platform.");
                return;
            }

            m_config = (fcAPI.fcExrConfig)config;
            m_ctx = fcAPI.fcExrCreateContext(ref m_config);
            m_outPath = outPath;
            m_frame = 0;
        }

        public override void AddVideoFrame(byte[] frame, fcAPI.fcPixelFormat format, double timestamp = -1.0)
        {
            if (m_ctx)
            {
                string path = m_outPath + "_" + m_frame.ToString("0000") + ".exr";
                int channels = System.Math.Min(m_config.channels, (int)format & 7);

                fcAPI.fcExrBeginImage(m_ctx, path, m_config.width, m_config.height);
                for (int i = 0; i < channels; ++i)
                {
                    fcAPI.fcExrAddLayerPixels(m_ctx, frame, format, i, s_channelNames[i]);
                }
                fcAPI.fcExrEndImage(m_ctx);
            }
            ++m_frame;
        }

        public override void AddAudioSamples(float[] samples)
        {
            // not supported
        }

    }
}
