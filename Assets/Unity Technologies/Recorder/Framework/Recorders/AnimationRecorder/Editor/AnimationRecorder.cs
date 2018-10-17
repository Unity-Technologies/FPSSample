using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.Recorder.Input;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UnityEditor.Experimental.Recorder
{
    [Recorder(typeof(AnimationRecorderSettings), "Animation Clips", "Unity/Animation Recording")]
    public class AnimationRecorder : GenericRecorder<AnimationRecorderSettings>
    {
        public override void RecordFrame(RecordingSession session)
        {
        }


        public override void EndRecording(RecordingSession ctx)
        {
            var ars = ctx.settings as AnimationRecorderSettings;

            for (int i = 0; i < m_Inputs.Count; ++i)
            {
                var set = (settings.inputsSettings[i] as AnimationInputSettings);
                if (set.enabled)
                {                  
                    var dir = "Assets/" + ars.outputPath;
                    var idx = dir.LastIndexOf('/');
                    if (idx > -1)
                    {
                        dir = dir.Substring(0,idx);
                    }
                    dir = ReplaceTokens(dir, ars, set);
                    Directory.CreateDirectory(dir);
                    
                    var aInput = m_Inputs[i] as AnimationInput;
                    AnimationClip clip = new AnimationClip();
                    var clipName = ReplaceTokens(("Assets/" + ars.outputPath),ars, set)+".anim";
                    clipName = AssetDatabase.GenerateUniqueAssetPath(clipName);
                    AssetDatabase.CreateAsset(clip, clipName);
                    aInput.m_gameObjectRecorder.SaveToClip(clip);
                    aInput.m_gameObjectRecorder.ResetRecording();
                }
            }

            ars.take++;
            base.EndRecording(ctx);
        }

        private string ReplaceTokens(string input, AnimationRecorderSettings ars, AnimationInputSettings  ais)
        {
            var idx = m_Inputs.Select(x => x.settings).ToList().IndexOf(ais);
            return input.Replace(AnimationRecorderSettings.goToken, ais.gameObject.name)
                       .Replace(AnimationRecorderSettings.inputToken,(idx+1).ToString("00"))
                       .Replace(AnimationRecorderSettings.takeToken, ars.take.ToString("000"));
        }
    }
}