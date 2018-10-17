using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.Recorder.Input;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UnityEditor.Experimental.Recorder
{
    [ExecuteInEditMode]
    [Serializable]
    public class AnimationRecorderSettings : RecorderSettings
    {
        public string outputPath = "AnimRecorder"+takeToken+"/"+goToken+"_"+inputToken;
        public int take = 1;
        public static string goToken = "<goName>";
        public static string takeToken = "<take>";
        public static string inputToken = "<input>";
        public override List<RecorderInputSetting> GetDefaultInputSettings()
        {
            return new List<RecorderInputSetting>()
            {
                NewInputSettingsObj<AnimationInputSettings>("Animation") 
            };
        }
        
        public override bool isPlatformSupported
        {
            get
            {
                return Application.platform == RuntimePlatform.LinuxEditor ||
                       Application.platform == RuntimePlatform.OSXEditor ||
                       Application.platform == RuntimePlatform.WindowsEditor;
            }
        }

        public override List<InputGroupFilter> GetInputGroups()
        {
            return new List<InputGroupFilter>()
            {
                new InputGroupFilter()
                {
                    title = "Animation",
                    typesFilter = new List<InputFilter>()
                    {
                        new TInputFilter<AnimationClipSettings>("GameObject Recorder"),
                    }
                }
            };
        }

        public override bool ValidityCheck( List<string> errors )
        {
            var ok = base.ValidityCheck(errors);

            if (inputsSettings == null)
            {
                ok = false;
                errors.Add("Invalid state!");
            }
            if (!inputsSettings.Cast<AnimationInputSettings>().Any(x => x != null && x.enabled && x.gameObject != null ))
            {
                ok = false;
                errors.Add("No input object set/enabled.");
            }

            if ( string.IsNullOrEmpty(outputPath))
            {
                ok = false;
                errors.Add("Invalid output path.");
            }

            return ok; 
        }
    }
}