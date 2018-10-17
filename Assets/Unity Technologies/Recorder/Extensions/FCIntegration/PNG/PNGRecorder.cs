using System;
using System.IO;
using UnityEngine;
using UnityEngine.Recorder;

namespace UTJ.FrameCapturer.Recorders
{
#if UNITY_2017_3_OR_NEWER
    [Obsolete("'UTJ/PNG' is obsolete, concider using 'Unity/Image Sequence' instead", false)]
    [Recorder(typeof(PNGRecorderSettings),"Video", "UTJ/Legacy/PNG" )]
#else
    [Recorder(typeof(PNGRecorderSettings),"Video", "UTJ/PNG" )]
#endif
    
    public class PNGRecorder : GenericRecorder<PNGRecorderSettings>
    {
        fcAPI.fcPngContext m_ctx;

        public override bool BeginRecording(RecordingSession session)
        {
            if (!base.BeginRecording(session)) { return false; }

            m_Settings.m_DestinationPath.CreateDirectory();

            m_ctx = fcAPI.fcPngCreateContext(ref m_Settings.m_PngEncoderSettings);
            return m_ctx;
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
            var fileName = m_Settings.m_BaseFileName.BuildFileName( session, recordedFramesCount, frame.width, frame.height, "png");
            var path = Path.Combine(m_Settings.m_DestinationPath.GetFullPath(), fileName);

            fcAPI.fcLock(frame, (data, fmt) =>
            {
                fcAPI.fcPngExportPixels(m_ctx, path, data, frame.width, frame.height, fmt, 0);
            });
        }

    }
}
