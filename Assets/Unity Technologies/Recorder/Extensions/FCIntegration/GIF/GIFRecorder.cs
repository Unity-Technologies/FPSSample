using System;
using System.IO;
using UnityEngine;
using UnityEngine.Recorder;

namespace UTJ.FrameCapturer.Recorders
{
    [Recorder(typeof(GIFRecorderSettings),"Video", "UTJ/GIF" )]
    public class GIFRecorder : GenericRecorder<GIFRecorderSettings>
    {
        fcAPI.fcGifContext m_ctx;
        fcAPI.fcStream m_stream;

        public override bool BeginRecording(RecordingSession session)
        {
            if (!base.BeginRecording(session)) { return false; }
            m_Settings.m_DestinationPath.CreateDirectory();

            return true;
        }

        public override void EndRecording(RecordingSession session)
        {
            m_ctx.Release();
            m_stream.Release();
            base.EndRecording(session);
        }

        public override void RecordFrame(RecordingSession session)
        {
            if (m_Inputs.Count != 1)
                throw new Exception("Unsupported number of sources");

            var input = (BaseRenderTextureInput)m_Inputs[0];
            var frame = input.outputRT;

            if(!m_ctx)
            {
                var settings = m_Settings.m_GifEncoderSettings;
                settings.width = frame.width;
                settings.height = frame.height;
                m_ctx = fcAPI.fcGifCreateContext(ref settings);
                var fileName = m_Settings.m_BaseFileName.BuildFileName( session, recordedFramesCount, frame.width, frame.height, "gif");
                var path = Path.Combine( m_Settings.m_DestinationPath.GetFullPath(), fileName);
                m_stream = fcAPI.fcCreateFileStream(path);
                fcAPI.fcGifAddOutputStream(m_ctx, m_stream);
            }

            fcAPI.fcLock(frame, TextureFormat.RGB24, (data, fmt) =>
            {
                fcAPI.fcGifAddFramePixels(m_ctx, data, fmt, session.recorderTime);
            });
        }

    }
}
