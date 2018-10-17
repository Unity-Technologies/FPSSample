using System;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Timeline;
using UnityEngine.Timeline;

namespace UnityEditor.Recorder.Timeline
{
    [CustomEditor(typeof(RecorderClip), true)]
    public class RecorderClipEditor : Editor
    {
        RecorderEditor m_Editor;
        TimelineAsset m_Timeline;
        RecorderSelector m_recorderSelector;

        public void OnEnable()
        {
            m_recorderSelector = null;
        }

        public override void OnInspectorGUI()
        {
            try
            {
                if (target == null)
                    return;

                // Bug? work arround: on Stop play, Enable is not called.
                if (m_Editor != null && m_Editor.target == null)
                {
                    UnityHelpers.Destroy(m_Editor);
                    m_Editor = null;
                    m_recorderSelector = null;
                }

                if (m_recorderSelector == null)
                {
                    m_recorderSelector = new RecorderSelector(OnRecorderSelected, false);
                    m_recorderSelector.Init((target as RecorderClip).m_Settings);
                }

                m_recorderSelector.OnGui();

                if (m_Editor != null)
                {
                    m_Editor.showBounds = false;
                    m_Timeline = FindTimelineAsset();

                    PushTimelineIntoRecorder();

                    using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                    {
                        EditorGUILayout.Separator();

                        m_Editor.OnInspectorGUI();

                        EditorGUILayout.Separator();

                        PushRecorderIntoTimeline();

                        serializedObject.Update();
                    }
                }
            }
            catch (ExitGUIException)
            {
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox("An exception was raised while editing the settings. This can be indicative of corrupted settings.", MessageType.Warning);

                if (GUILayout.Button("Reset settings to default"))
                    ResetSettings();

                Debug.LogException(ex);
            }
        }

        void ResetSettings()
        {
            UnityHelpers.Destroy(m_Editor);
            m_Editor = null;
            m_recorderSelector = null;
            UnityHelpers.Destroy((target as RecorderClip).m_Settings, true);
        }

        public void OnRecorderSelected()
        {
            var clip = this.target as RecorderClip;

            if (m_Editor != null)
            {
                UnityHelpers.Destroy(m_Editor);
                m_Editor = null;
            }

            if (m_recorderSelector.selectedRecorder == null)
                return;

            if (clip.m_Settings != null && RecordersInventory.GetRecorderInfo(m_recorderSelector.selectedRecorder).settingsClass != clip.m_Settings.GetType())
            {
                UnityHelpers.Destroy(clip.m_Settings, true);
                clip.m_Settings = null;
            }

            if(clip.m_Settings == null)
                clip.m_Settings = RecordersInventory.GenerateRecorderInitialSettings(clip, m_recorderSelector.selectedRecorder );
            m_Editor = Editor.CreateEditor(clip.m_Settings) as RecorderEditor;
            AssetDatabase.Refresh();
        }

        TimelineAsset FindTimelineAsset()
        {
            if (!AssetDatabase.Contains(target))
                return null;

            var path = AssetDatabase.GetAssetPath(target);
            var objs = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (var obj in objs)
            {
                if (obj != null && AssetDatabase.IsMainAsset(obj))
                    return obj as TimelineAsset;
            }
            return null;
        }

        void PushTimelineIntoRecorder()
        {
            if (m_Timeline == null)
                return;

            var settings = m_Editor.target as RecorderSettings;
            settings.m_DurationMode = DurationMode.Manual;

            // Time
            settings.m_FrameRate = m_Timeline.editorSettings.fps;
        }

        void PushRecorderIntoTimeline()
        {
            if (m_Timeline == null)
                return;

            var settings = m_Editor.target as RecorderSettings;
            settings.m_DurationMode = DurationMode.Manual;

            // Time
            m_Timeline.editorSettings.fps = (float)settings.m_FrameRate;
        }
    }
}
