using System;
using UnityEditor.Experimental.Recorder;
using UnityEditor.Experimental.Recorder.Input;
using UnityEditor.Recorder;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UnityEditor.Experimental.FrameRecorder
{
    [Serializable]
    [CustomEditor(typeof(AnimationRecorderSettings))]
    public class AnimationRecorderSettingsEditor: RecorderEditor
    {
        private bool recorderSettings = false;
        
        [MenuItem("Tools/Recorder/Animation Clips")]
        private static void ShowRecorderWindow()
        {
            RecorderWindow.ShowAndPreselectCategory("Animation Clips");
        }

        protected override void OnInputGui()
        {
            var aRecorderSettings = target as AnimationRecorderSettings;
            var inputs = aRecorderSettings.inputsSettings;

            for (int i = 0; i < inputs.Count; i++)
            {
                                
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                var input = inputs[i] as AnimationInputSettings;
                
                
                EditorGUILayout.BeginHorizontal();
                var r = EditorGUILayout.GetControlRect();
                var rFold = r;
                rFold.width = 20;
                input.fold = EditorGUI.Foldout(rFold,input.fold,"");
                r.xMin += 15;
                input.enabled = EditorGUI.ToggleLeft(r,"Object Recorder",input.enabled);
               
                var gearStyle = new GUIStyle("Icon.Options");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(gearStyle.normal.background,new GUIStyle("IconButton")))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Remove"),false, data =>
                        {
                            var ais = data as AnimationInputSettings;
                            aRecorderSettings.inputsSettings.Remove(ais);
                        },
                        inputs[i]);
                            
                    menu.ShowAsContext();
                }               
                
                using (new EditorGUI.IndentLevelScope(1))
                {
                   EditorGUILayout.EndHorizontal();
                    if (input.fold)
                    {
                        OnInputGui(i);

                    }
                }

            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Object To Record", GUILayout.Width(200)))
            {
                var newSettings = aRecorderSettings.NewInputSettingsObj<AnimationInputSettings>("Animation");
                aRecorderSettings.inputsSettings.Add(newSettings);
            }  
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        protected override void OnOutputGui()
        {
            var aRecorderSettings = target as AnimationRecorderSettings;
            aRecorderSettings.outputPath = EditorGUILayout.TextField("Output Path", aRecorderSettings.outputPath);
        }

        protected override void OnEncodingGroupGui()
        {
        }

        protected override void OnGroupGui()
        {
            recorderSettings = EditorGUILayout.Foldout(recorderSettings,"Recorder Settings");
            if (recorderSettings)
            {
                using (new EditorGUI.IndentLevelScope(1))
                {
                    OnOutputGroupGui();
                    OnEncodingGroupGui();
                    OnFrameRateGroupGui();
                    OnBoundsGroupGui();
                }
            }
            
            OnInputGui();
        }
        
    }
}