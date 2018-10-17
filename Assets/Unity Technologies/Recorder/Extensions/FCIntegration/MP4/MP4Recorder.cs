using System;
using System.IO;
using UnityEngine;
using UnityEngine.Recorder;

namespace UTJ.FrameCapturer.Recorders
{
#if UNITY_2017_3_OR_NEWER
    [Obsolete("'UTJ/MP4' is obsolete, concider using 'Unity/Movie' instead", false)]
    [Recorder(typeof(MP4RecorderSettings),"Video", "UTJ/Legacy/MP4" )]
#else
    [Recorder(typeof(MP4RecorderSettings),"Video", "UTJ/MP4" )]
#endif
    public class MP4Recorder : GenericRecorder<MP4RecorderSettings>
    {
        fcAPI.fcMP4Context m_ctx;

        public override bool BeginRecording(RecordingSession session)
        {
            if (!base.BeginRecording(session)) { return false; }

            m_Settings.m_DestinationPath.CreateDirectory();

            var input = (BaseRenderTextureInput)m_Inputs[0];
            if (input.outputWidth > 4096 || input.outputHeight > 2160 )
            {
                Debug.LogError("Mp4 format does not support requested resolution.");
            }

            return true;
        }

        public override void EndRecording(RecordingSession session)
        {
            m_ctx.Release();
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
                var settings = m_Settings.m_MP4EncoderSettings;
                settings.video = true;
                settings.audio = false;
                settings.videoWidth = frame.width;
                settings.videoHeight = frame.height;
                settings.videoTargetFramerate = (int)Math.Ceiling(m_Settings.m_FrameRate);
                if (m_Settings.m_AutoSelectBR)
                {
                    settings.videoTargetBitrate = (int)(( (frame.width * frame.height/1000.0) / 245 + 1.16) * (settings.videoTargetFramerate / 48.0 + 0.5) * 1000000);
                }
                var fileName = m_Settings.m_BaseFileName.BuildFileName( session, recordedFramesCount, frame.width, frame.height, "mp4");
                var path = Path.Combine( m_Settings.m_DestinationPath.GetFullPath(), fileName);
                m_ctx = fcAPI.fcMP4OSCreateContext(ref settings, path);
            }

            fcAPI.fcLock(frame, TextureFormat.RGB24, (data, fmt) =>
            {
                fcAPI.fcMP4AddVideoFramePixels(m_ctx, data, fmt, session.recorderTime);
            });
        }

    }
}
