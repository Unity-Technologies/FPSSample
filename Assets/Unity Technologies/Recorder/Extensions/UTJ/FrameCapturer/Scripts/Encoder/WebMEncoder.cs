using System;
using UnityEngine;


namespace UTJ.FrameCapturer
{
    public class WebMEncoder : MovieEncoder
    {
        fcAPI.fcWebMContext m_ctx;
        fcAPI.fcWebMConfig m_config;

        public override void Release() { m_ctx.Release(); }
        public override bool IsValid() { return m_ctx; }
        public override Type type { get { return Type.WebM; } }

        public override void Initialize(object config, string outPath)
        {
            if (!fcAPI.fcWebMIsSupported())
            {
                Debug.LogError("WebM encoder is not available on this platform.");
                return;
            }

            m_config = (fcAPI.fcWebMConfig)config;
            if (m_config.audio && m_config.audioEncoder == fcAPI.fcWebMAudioEncoder.Opus)
            {
                var sampleRate = AudioSettings.outputSampleRate;
                if (sampleRate != 8000 && sampleRate != 12000 && sampleRate != 16000 && sampleRate != 24000 && sampleRate != 48000)
                {
                    Debug.LogError("Current output sample rate is " + sampleRate + ". It must be 8000, 12000, 16000, 24000 or 48000 to use Opus audio encoder. Fallback to Vorbis.");
                    m_config.audioEncoder = fcAPI.fcWebMAudioEncoder.Vorbis;
                }
            }

            m_config.audioSampleRate = AudioSettings.outputSampleRate;
            m_config.audioNumChannels = fcAPI.fcGetNumAudioChannels();
            m_ctx = fcAPI.fcWebMCreateContext(ref m_config);

            var path = outPath + ".webm";
            var stream = fcAPI.fcCreateFileStream(path);
            fcAPI.fcWebMAddOutputStream(m_ctx, stream);
            stream.Release();
        }

        public override void AddVideoFrame(byte[] frame, fcAPI.fcPixelFormat format, double timestamp)
        {
            if (m_ctx && m_config.video)
            {
                fcAPI.fcWebMAddVideoFramePixels(m_ctx, frame, format, timestamp);
            }
        }

        public override void AddAudioSamples(float[] samples)
        {
            if (m_ctx && m_config.audio)
            {
                fcAPI.fcWebMAddAudioSamples(m_ctx, samples, samples.Length);
            }
        }
    }
}
