using System;
using UnityEngine;


namespace UTJ.FrameCapturer
{
    public class MP4Encoder : MovieEncoder
    {
        fcAPI.fcMP4Context m_ctx;
        fcAPI.fcMP4Config m_config;

        public override void Release() { m_ctx.Release(); }
        public override bool IsValid() { return m_ctx; }
        public override Type type { get { return Type.MP4; } }

        public override void Initialize(object config, string outPath)
        {
            if (!fcAPI.fcMP4OSIsSupported())
            {
                Debug.LogError("MP4 encoder is not available on this platform.");
                return;
            }

            m_config = (fcAPI.fcMP4Config)config;
            m_config.audioSampleRate = AudioSettings.outputSampleRate;
            m_config.audioNumChannels = fcAPI.fcGetNumAudioChannels();

            var path = outPath + ".mp4";
            m_ctx = fcAPI.fcMP4OSCreateContext(ref m_config, path);
        }

        public override void AddVideoFrame(byte[] frame, fcAPI.fcPixelFormat format, double timestamp)
        {
            if (m_ctx && m_config.video)
            {
                fcAPI.fcMP4AddVideoFramePixels(m_ctx, frame, format, timestamp);
            }
        }

        public override void AddAudioSamples(float[] samples)
        {
            if (m_ctx && m_config.audio)
            {
                fcAPI.fcMP4AddAudioSamples(m_ctx, samples, samples.Length);
            }
        }
    }
}
