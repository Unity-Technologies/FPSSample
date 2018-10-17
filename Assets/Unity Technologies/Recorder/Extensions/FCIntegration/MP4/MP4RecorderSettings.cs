using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UTJ.FrameCapturer.Recorders
{
    [ExecuteInEditMode]
    public class MP4RecorderSettings : BaseFCRecorderSettings
    {
        public fcAPI.fcMP4Config m_MP4EncoderSettings = fcAPI.fcMP4Config.default_value;
        public bool m_AutoSelectBR;

        MP4RecorderSettings()
        {
            m_BaseFileName.pattern = "movie.<ext>";
            m_AutoSelectBR = true;
        }

        public override List<RecorderInputSetting> GetDefaultInputSettings()
        {
            return new List<RecorderInputSetting>()
            {
                NewInputSettingsObj<CBRenderTextureInputSettings>("Pixels") 
            };
        }

        public override RecorderInputSetting NewInputSettingsObj(Type type, string title )
        {
            var obj = base.NewInputSettingsObj(type, title);
            if (type == typeof(CBRenderTextureInputSettings))
            {
                var settings = (CBRenderTextureInputSettings)obj;
                settings.m_ForceEvenSize = true;
                settings.m_FlipFinalOutput = true;
            }

            return obj ;
        }

        public override bool isPlatformSupported
        {
            get
            {
                return Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer;
            }
        }

        public override bool SelfAdjustSettings()
        {
            if (inputsSettings.Count == 0 )
                return false;

            var adjusted = false;

            if (inputsSettings[0] is ImageInputSettings)
            {
                var iis = (ImageInputSettings)inputsSettings[0];
                if (iis.maxSupportedSize != EImageDimension.x2160p_4K)
                {
                    iis.maxSupportedSize = EImageDimension.x2160p_4K;
                    adjusted = true;
                }
            }
            return adjusted;
        }

    }
}
