#if UNITY_2017_3_OR_NEWER

using System;
using System.Collections.Generic;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;
namespace UnityEditor.Recorder
{

    public enum MediaRecorderOutputFormat
    {
        MP4,
        WEBM
    }

    [ExecuteInEditMode]
    public class MediaRecorderSettings : RecorderSettings
    {
        public MediaRecorderOutputFormat m_OutputFormat = MediaRecorderOutputFormat.MP4;
#if UNITY_2018_1_OR_NEWER
        public UnityEditor.VideoBitrateMode m_VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
#endif
        public bool m_AppendSuffix = false;

        MediaRecorderSettings()
        {
            m_BaseFileName.pattern = "movie.<ext>";
        }

        public override List<RecorderInputSetting> GetDefaultInputSettings()
        {
            return new List<RecorderInputSetting>()
            {
                NewInputSettingsObj<CBRenderTextureInputSettings>("Pixels"),
                NewInputSettingsObj<AudioInputSettings>("Audio")
            };
        }

        public override bool ValidityCheck( List<string> errors )
        {
            var ok = base.ValidityCheck(errors);

            if( string.IsNullOrEmpty(m_DestinationPath.GetFullPath() ))
            {
                ok = false;
                errors.Add("Missing destination path.");
            } 
            if(  string.IsNullOrEmpty(m_BaseFileName.pattern))
            {
                ok = false;
                errors.Add("missing file name");
            }

            return ok;
        }

        public override RecorderInputSetting NewInputSettingsObj(Type type, string title)
        {
            var obj = base.NewInputSettingsObj(type, title);
            if (type == typeof(CBRenderTextureInputSettings))
            {
                (obj as CBRenderTextureInputSettings).m_ForceEvenSize = true;
                (obj as CBRenderTextureInputSettings).m_FlipFinalOutput = Application.platform == RuntimePlatform.OSXEditor;
            }
            if (type == typeof(RenderTextureSamplerSettings))
            {
                (obj as RenderTextureSamplerSettings).m_ForceEvenSize = true;
            }
            if (type == typeof(ScreenCaptureInputSettings))
            {
                (obj as ScreenCaptureInputSettings).m_ForceEvenSize = true;
            }

            return obj ;
        }

        public override List<InputGroupFilter> GetInputGroups()
        {
            return new List<InputGroupFilter>()
            {
                new InputGroupFilter()
                {
                    title = "Pixels",
                    typesFilter = new List<InputFilter>()
                    {
                        new TInputFilter<ScreenCaptureInputSettings>("Game View"),
                        new TInputFilter<CBRenderTextureInputSettings>("Specific Camera(s)"),
#if UNITY_2018_1_OR_NEWER
                        new TInputFilter<Camera360InputSettings>("360 View (feature preview)"),
#endif
                        new TInputFilter<RenderTextureSamplerSettings>("Sampling (off screen)"),
                        new TInputFilter<RenderTextureInputSettings>("Render Texture Asset"),
                    }
                },
                new InputGroupFilter()
                {
                    title = "Sound",
                    typesFilter = new List<InputFilter>()
                    {
                        new TInputFilter<AudioInputSettings>("Audio"),
                    }
                }
            };
        }

        public override bool SelfAdjustSettings()
        {
            if (inputsSettings.Count == 0 )
                return false;

            var adjusted = false;

            if (inputsSettings[0] is ImageInputSettings)
            {
                var iis = (ImageInputSettings)inputsSettings[0];
                var maxRes = m_OutputFormat == MediaRecorderOutputFormat.MP4 ? EImageDimension.x2160p_4K : EImageDimension.x4320p_8K;
                if (iis.maxSupportedSize != maxRes)
                {
                    iis.maxSupportedSize = maxRes;
                    adjusted = true;
                }
            }

            return adjusted;
        }
       
    }
}

#endif