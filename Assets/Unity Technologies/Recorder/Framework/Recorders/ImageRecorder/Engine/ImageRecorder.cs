using System;
using System.IO;
using UnityEngine.Recorder.Input;

namespace UnityEngine.Recorder
{
    [Recorder(typeof(ImageRecorderSettings),"Video", "Unity/Image sequence" )]
    public class ImageRecorder : GenericRecorder<ImageRecorderSettings>
    {

        public override bool BeginRecording(RecordingSession session)
        {
            if (!base.BeginRecording(session)) { return false; }

            m_Settings.m_DestinationPath.CreateDirectory();

            return true;
        }

        public override void RecordFrame(RecordingSession session)
        {
            if (m_Inputs.Count != 1)
                throw new Exception("Unsupported number of sources");

            Texture2D tex = null;
#if UNITY_2017_3_OR_NEWER
            if (m_Inputs[0] is ScreenCaptureInput)
            {
                tex = ((ScreenCaptureInput)m_Inputs[0]).image;
                if (m_Settings.m_OutputFormat == PNGRecordeOutputFormat.EXR)
                {
                    var textx = new Texture2D(tex.width, tex.height, TextureFormat.RGBAFloat, false);
                    textx.SetPixels(tex.GetPixels());
                    tex = textx;
                }
                else if (m_Settings.m_OutputFormat == PNGRecordeOutputFormat.PNG)
                {
                    var textx = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
                    textx.SetPixels(tex.GetPixels());
                    tex = textx;
                }
            }
            else
#endif
            {
                var input = (BaseRenderTextureInput)m_Inputs[0];
                var width = input.outputRT.width;
                var height = input.outputRT.height;
                tex = new Texture2D(width, height, m_Settings.m_OutputFormat != PNGRecordeOutputFormat.EXR ? TextureFormat.RGBA32 : TextureFormat.RGBAFloat, false);
                var backupActive = RenderTexture.active;
                RenderTexture.active = input.outputRT;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                tex.Apply();
                RenderTexture.active = backupActive;
            }

            byte[] bytes;
            string ext;
            switch (m_Settings.m_OutputFormat)
            {
                case PNGRecordeOutputFormat.PNG:
                    bytes = tex.EncodeToPNG();
                    ext = "png";
                    break;
                case PNGRecordeOutputFormat.JPEG:
                    bytes = tex.EncodeToJPG();
                    ext = "jpg";
                    break;
                case PNGRecordeOutputFormat.EXR:
                    bytes = tex.EncodeToEXR();
                    ext = "exr";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if(m_Inputs[0] is BaseRenderTextureInput || m_Settings.m_OutputFormat != PNGRecordeOutputFormat.JPEG)
                UnityHelpers.Destroy(tex);

            var fileName = m_Settings.m_BaseFileName.BuildFileName( session, recordedFramesCount, tex.width, tex.height, ext);
            var path = Path.Combine( m_Settings.m_DestinationPath.GetFullPath(), fileName);

            File.WriteAllBytes( path, bytes);
        }
    }
}
