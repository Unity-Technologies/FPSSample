using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UTJ.FrameCapturer.Recorders
{
    public abstract class BaseFCRecorderSettings : RecorderSettings
    {
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

        public override bool isPlatformSupported
        {
            get
            {
                return Application.platform == RuntimePlatform.WindowsEditor || 
                        Application.platform == RuntimePlatform.WindowsPlayer ||
                        Application.platform == RuntimePlatform.OSXEditor ||
                        Application.platform == RuntimePlatform.OSXPlayer ||
                        Application.platform == RuntimePlatform.LinuxEditor ||
                        Application.platform == RuntimePlatform.LinuxPlayer;
            }
        }

        public override RecorderInputSetting NewInputSettingsObj(Type type, string title )
        {
            var obj = base.NewInputSettingsObj(type, title);
            if (type == typeof(CBRenderTextureInputSettings))
            {
                var settings = (CBRenderTextureInputSettings)obj;
                settings.m_FlipFinalOutput = true;
            }
            else if (type == typeof(RenderTextureSamplerSettings))
            {
                var settings = (RenderTextureSamplerSettings)obj;
                settings.m_FlipFinalOutput = true;
            }

            return obj ;
        }

        public override List<InputGroupFilter> GetInputGroups()
        {
            return new List<InputGroupFilter>()
            {
                new InputGroupFilter()
                {
                    title = "Pixels", typesFilter = new List<InputFilter>()
                    {
                        new TInputFilter<CBRenderTextureInputSettings>("Camera(s)"),
                        new TInputFilter<RenderTextureSamplerSettings>("Sampling"),
                        new TInputFilter<RenderTextureInputSettings>("Render Texture"),
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
                if (iis.maxSupportedSize != EImageDimension.x4320p_8K)
                {
                    iis.maxSupportedSize = EImageDimension.x4320p_8K;
                    adjusted = true;
                }
            }
            return adjusted;
        }


    }
}
