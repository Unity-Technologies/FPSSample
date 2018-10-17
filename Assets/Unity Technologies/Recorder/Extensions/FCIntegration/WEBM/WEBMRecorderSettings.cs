using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UTJ.FrameCapturer.Recorders
{
    [ExecuteInEditMode]
    public class WEBMRecorderSettings : BaseFCRecorderSettings
    {
        public fcAPI.fcWebMConfig m_WebmEncoderSettings = fcAPI.fcWebMConfig.default_value;
        public bool m_AutoSelectBR;

        WEBMRecorderSettings()
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
                (obj as CBRenderTextureInputSettings).m_ForceEvenSize = true;

            return obj ;
        }

    }
}
