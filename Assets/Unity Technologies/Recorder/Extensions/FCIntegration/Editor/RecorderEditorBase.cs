using System;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;

namespace UTJ.FrameCapturer.Recorders
{
    public class RecorderEditorBase: RecorderEditor
    {
        public string m_BaseFileName;
        public string m_DestinationPath;

        [MenuItem("Tools/Recorder/Video")]
        static void ShowRecorderWindow()
        {
            RecorderWindow.ShowAndPreselectCategory("Video");
        }
    }
}
