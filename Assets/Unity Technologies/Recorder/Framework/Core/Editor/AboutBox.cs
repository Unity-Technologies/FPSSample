using System;
using UnityEngine;

namespace UnityEditor.Recorder
{
    public class AboutBox : EditorWindow
    {
        [MenuItem("Tools/Recorder/About...", false, Int32.MaxValue)]
        public static void ShowAboutBox()
        {
            EditorWindow.GetWindowWithRect<AboutBox>(new Rect(100, 100, 550, 330), true, "About Recorder");
        }

        GUIContent s_Header;

        void OnEnable()
        {
            s_Header = EditorGUIUtility.IconContent("AboutWindow.MainHeader");
        }

        public void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginVertical();
            GUILayout.Label(s_Header, GUIStyle.none);

            GUILayout.BeginHorizontal();
            GUILayout.Space(52f);
            GUILayout.Label("Recorder " + RecorderVersion.Stage, EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(52f);
            GUILayout.Label(string.Format("Version {0}", RecorderVersion.Tag));
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            var text = "The Unity Recorder package is a collection of Recorders that allow in-game capturing of data and saving it. For example; generate an mp4 file from a game session.\r\n\r\nIn support to the recorders, it provides a graphical interface that is used to manually trigger recording sessions, which take care of: entering play mode, recording requested data and exiting play mode when done. It also supports triggering recording sessions from user scripts and timeline tracks.\r\n\r\nThe Recorder is aimed at extensibility and is implemented as a plugin system, where anyone can create new recorders and have them seamlessly integrate into the Unity Recorder ecosystem, while maximizing code reuse.";

            float textWidth = position.width - 10;
            float textHeight = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(text), textWidth);
            Rect creditsNamesRect = new Rect(5, 120, textWidth, textHeight);
            GUI.Label(creditsNamesRect, text, EditorStyles.wordWrappedLabel);
            GUILayout.Space(25);
            GUILayout.Space(textHeight);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("View user manual"))
            {
                var file = FRPackagerPaths.GetFrameRecorderPath() + "/Recorder_install.pdf";
                Debug.Log(file);
                Application.OpenURL(file);
                this.Close();
            }
            GUILayout.Space(25);
            if (GUILayout.Button("Want to write a recorder?"))
            {
                Application.OpenURL("https://github.com/Unity-Technologies/GenericFrameRecorder/blob/master/README.md");
                this.Close();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unity's User forum"))
            {
                Application.OpenURL("https://forum.unity.com/threads/unity-recorder-update.509458/");
                this.Close();
            }
            GUILayout.EndHorizontal();

        }
    }
}