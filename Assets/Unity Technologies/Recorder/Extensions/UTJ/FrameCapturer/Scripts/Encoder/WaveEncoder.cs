using System;
using UnityEngine;


namespace UTJ.FrameCapturer
{
    public class WaveEncoder : AudioEncoder
    {
        fcAPI.fcWaveContext m_ctx;
        fcAPI.fcWaveConfig m_config;

        public override void Release() { m_ctx.Release(); }
        public override bool IsValid() { return m_ctx; }
        public override Type type { get { return Type.Wave; } }

        public override void Initialize(object config, string outPath)
        {
            if (!fcAPI.fcWaveIsSupported())
            {
                Debug.LogError("Wave encoder is not available on this platform.");
                return;
            }

            m_config = (fcAPI.fcWaveConfig)config;
            m_config.sampleRate = AudioSettings.outputSampleRate;
            m_config.numChannels = fcAPI.fcGetNumAudioChannels();
            m_ctx = fcAPI.fcWaveCreateContext(ref m_config);

            var path = outPath + ".wave";
            var stream = fcAPI.fcCreateFileStream(path);
            fcAPI.fcWaveAddOutputStream(m_ctx, stream);
            stream.Release();
        }

        public override void AddAudioSamples(float[] samples)
        {
            if(m_ctx)
            {
                fcAPI.fcWaveAddAudioSamples(m_ctx, samples, samples.Length);
            }
        }
    }
}
