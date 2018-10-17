using UnityEngine;


namespace UTJ.FrameCapturer
{
    public class PngEncoder : MovieEncoder
    {
        fcAPI.fcPngContext m_ctx;
        fcAPI.fcPngConfig m_config;
        string m_outPath;
        int m_frame;


        public override void Release() { m_ctx.Release(); }
        public override bool IsValid() { return m_ctx; }
        public override Type type { get { return Type.Png; } }

        public override void Initialize(object config, string outPath)
        {
            if (!fcAPI.fcPngIsSupported())
            {
                Debug.LogError("Png encoder is not available on this platform.");
                return;
            }

            m_config = (fcAPI.fcPngConfig)config;
            m_ctx = fcAPI.fcPngCreateContext(ref m_config);
            m_outPath = outPath;
            m_frame = 0;
        }

        public override void AddVideoFrame(byte[] frame, fcAPI.fcPixelFormat format, double timestamp = -1.0)
        {
            if (m_ctx)
            {
                string path = m_outPath + "_" + m_frame.ToString("0000") + ".png";
                int channels = System.Math.Min(m_config.channels, (int)format & 7);
                fcAPI.fcPngExportPixels(m_ctx, path, frame, m_config.width, m_config.height, format, channels);
            }
            ++m_frame;
        }

        public override void AddAudioSamples(float[] samples)
        {
            // not supported
        }

    }
}
