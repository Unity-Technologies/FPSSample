using System;
using UnityEngine;


namespace UTJ.FrameCapturer
{
    public class OggEncoder : AudioEncoder
    {
        fcAPI.fcOggContext m_ctx;
        fcAPI.fcOggConfig m_config;

        public override void Release() { m_ctx.Release(); }
        public override bool IsValid() { return m_ctx; }
        public override Type type { get { return Type.Ogg; } }

        public override void Initialize(object config, string outPath)
        {
            if (!fcAPI.fcOggIsSupported())
            {
                Debug.LogError("Ogg encoder is not available on this platform.");
                return;
            }

            m_config = (fcAPI.fcOggConfig)config;
            m_config.sampleRate = AudioSettings.outputSampleRate;
            m_config.numChannels = fcAPI.fcGetNumAudioChannels();
            m_ctx = fcAPI.fcOggCreateContext(ref m_config);

            var path = outPath + ".ogg";
            var stream = fcAPI.fcCreateFileStream(path);
            fcAPI.fcOggAddOutputStream(m_ctx, stream);
            stream.Release();
        }

        public override void AddAudioSamples(float[] samples)
        {
            if(m_ctx)
            {
                fcAPI.fcOggAddAudioSamples(m_ctx, samples, samples.Length);
            }
        }
    }
}
