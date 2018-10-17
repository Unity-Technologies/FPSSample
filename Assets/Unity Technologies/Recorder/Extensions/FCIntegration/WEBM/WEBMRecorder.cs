using System;
using System.IO;
using UnityEngine;
using UnityEngine.Recorder;

namespace UTJ.FrameCapturer.Recorders
{
#if UNITY_2017_3_OR_NEWER
    [Obsolete("'UTJ/WEBM' is obsolete, concider using 'Unity/Movie' instead", false)]
    [Recorder(typeof(WEBMRecorderSettings),"Video", "UTJ/Legacy/WebM" )]
#else
    [Recorder(typeof(WEBMRecorderSettings),"Video", "UTJ/WebM" )]
#endif
    public class WEBMRecorder : GenericRecorder<WEBMRecorderSettings>
    {
        fcAPI.fcWebMContext m_ctx;
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

            if (!m_ctx)
            {
                var settings = m_Settings.m_WebmEncoderSettings;
                settings.video = true;
                settings.audio = false;
                settings.videoWidth = frame.width;
                settings.videoHeight = frame.height;
                if (m_Settings.m_AutoSelectBR)
                {
                    settings.videoTargetBitrate = (int)(( (frame.width * frame.height/1000.0) / 245 + 1.16) * (settings.videoTargetFramerate / 48.0 + 0.5) * 1000000);
                }

                settings.videoTargetFramerate = (int)Math.Ceiling(m_Settings.m_FrameRate);
                m_ctx = fcAPI.fcWebMCreateContext(ref settings);
                var fileName = m_Settings.m_BaseFileName.BuildFileName( session, recordedFramesCount, settings.videoWidth, settings.videoHeight, "webm");
                var path = Path.Combine( m_Settings.m_DestinationPath.GetFullPath(), fileName);
                m_stream = fcAPI.fcCreateFileStream(path);
                fcAPI.fcWebMAddOutputStream(m_ctx, m_stream);
            }

            fcAPI.fcLock(frame, TextureFormat.RGB24, (data, fmt) =>
            {
                fcAPI.fcWebMAddVideoFramePixels(m_ctx, data, fmt, session.recorderTime);
            });
        }

    }
}
