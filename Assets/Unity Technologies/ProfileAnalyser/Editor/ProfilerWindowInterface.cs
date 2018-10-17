using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace ProfileAnalyser
{
    public class ProfilerWindowInterface
    {
        private Type m_profilerWindowType;
        private EditorWindow m_profilerWindow;
        private FieldInfo m_currentFrameFieldInfo;
        private FieldInfo m_timeLineGUIFieldInfo;
        private FieldInfo m_selectedEntryFieldInfo;
        private FieldInfo m_selectedNameFieldInfo;
        private FieldInfo m_selectedTimeFieldInfo;
        private FieldInfo m_selectedDurationFieldInfo;
        private FieldInfo m_selectedInstanceIdFieldInfo;
        private FieldInfo m_selectedInstanceCountFieldInfo;
        private FieldInfo m_selectedFrameIdFieldInfo;
        private FieldInfo m_selectedThreadIdFieldInfo;
        private FieldInfo m_selectedNativeIndexFieldInfo;

        public ProfilerWindowInterface()
        {
            Assembly assem = typeof(Editor).Assembly;
            m_profilerWindowType = assem.GetType("UnityEditor.ProfilerWindow");
            m_currentFrameFieldInfo = m_profilerWindowType.GetField("m_CurrentFrame", BindingFlags.NonPublic | BindingFlags.Instance);

            m_timeLineGUIFieldInfo = m_profilerWindowType.GetField("m_CPUTimelineGUI", BindingFlags.NonPublic | BindingFlags.Instance);
            if (m_timeLineGUIFieldInfo != null)
                m_selectedEntryFieldInfo = m_timeLineGUIFieldInfo.FieldType.GetField("m_SelectedEntry", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (m_selectedEntryFieldInfo != null)
            {
                m_selectedNameFieldInfo = m_selectedEntryFieldInfo.FieldType.GetField("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_selectedTimeFieldInfo = m_selectedEntryFieldInfo.FieldType.GetField("time", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_selectedDurationFieldInfo = m_selectedEntryFieldInfo.FieldType.GetField("duration", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_selectedInstanceIdFieldInfo = m_selectedEntryFieldInfo.FieldType.GetField("instanceId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_selectedInstanceCountFieldInfo = m_selectedEntryFieldInfo.FieldType.GetField("instanceCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_selectedFrameIdFieldInfo = m_selectedEntryFieldInfo.FieldType.GetField("frameId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_selectedThreadIdFieldInfo = m_selectedEntryFieldInfo.FieldType.GetField("threadId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m_selectedNativeIndexFieldInfo = m_selectedEntryFieldInfo.FieldType.GetField("nativeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }

        /*
        public EditorWindow GetProfileWindow()
        {
            return m_profilerWindow;
        }
        */

        public bool IsReady()
        {
            if (m_profilerWindow != null)
                return true;

            return false;
        }

        public bool IsProfilerWindowOpen()
        {
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(m_profilerWindowType);
            if (windows != null && windows.Length > 0)
                return true;

            return false;
        }

        public void OpenProfilerOrUseExisting()
        {
            m_profilerWindow = EditorWindow.GetWindow(m_profilerWindowType);
        }

        public bool GetFrameRangeFromProfiler(out int first, out int last)
        {
            if (m_profilerWindow)
            //if (ProfilerDriver.enabled)
            {
                first = 1 + ProfilerDriver.firstFrameIndex;
                last = 1 + ProfilerDriver.lastFrameIndex;
                return true;
            }

            first = 1;
            last = 1;
            return false;
        }

        public void CloseProfiler()
        {
            if (m_profilerWindow)
                m_profilerWindow.Close();
        }

        public string GetProfilerWindowMarkerName()
        {
            var timeLineGUI = m_timeLineGUIFieldInfo.GetValue(m_profilerWindow);
            if (timeLineGUI != null && m_selectedEntryFieldInfo != null)
            {
                var selectedEntry = m_selectedEntryFieldInfo.GetValue(timeLineGUI);
                if (selectedEntry != null && m_selectedNameFieldInfo != null)
                {
                    return m_selectedNameFieldInfo.GetValue(selectedEntry).ToString();
                }
            }

            return null;
        }

        public float GetFrameTime(int frameIndex)
        {
            ProfilerFrameDataIterator frameData = new ProfilerFrameDataIterator();

            frameData.SetRoot(frameIndex, 0);
            float ms = frameData.frameTimeMS;
            frameData.Dispose();

            return ms;
        }

        private bool GetMarkerInfo(string markerName, int frameIndex, string threadFilter, out int outThreadIndex, out float time, out float duration, out int instanceId)
        {
            ProfilerFrameDataIterator frameData = new ProfilerFrameDataIterator();

            outThreadIndex = 0;
            time = 0.0f;
            duration = 0.0f;
            instanceId = 0;
            bool found = false;

            int threadCount = frameData.GetThreadCount(frameIndex);
            Dictionary<string, int> threadNameCount = new Dictionary<string, int>();
            for (int threadIndex = 0; threadIndex < threadCount; ++threadIndex)
            {
                frameData.SetRoot(frameIndex, threadIndex);

                var threadName = frameData.GetThreadName();
                if (!threadNameCount.ContainsKey(threadName))
                    threadNameCount.Add(threadName, 1);
                else
                    threadNameCount[threadName] += 1;
                var threadNameWithIndex = ProfileData.ThreadNameWithIndex(threadNameCount[threadName], threadName);

                if (threadFilter == "All" || threadNameWithIndex == threadFilter)
                {
                    const bool enterChildren = true;
                    while (frameData.Next(enterChildren))
                    {
                        if (frameData.name == markerName)
                        {
                            time = frameData.startTimeMS;
                            duration = frameData.durationMS;
                            instanceId = frameData.instanceId;
                            outThreadIndex = threadIndex;
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                    break;
            }

            frameData.Dispose();
            return found;
        }

        public void SetProfilerWindowMarkerName(string markerName, string threadFilter)
        {
            if (m_profilerWindow == null)
                return;
            
            var timeLineGUI = m_timeLineGUIFieldInfo.GetValue(m_profilerWindow);
            if (timeLineGUI != null && m_selectedEntryFieldInfo != null)
            {
                var selectedEntry = m_selectedEntryFieldInfo.GetValue(timeLineGUI);
                if (selectedEntry != null)
                {
                    // Read profiler data direct from profile to find time/duration
                    int currentFrameIndex = (int)m_currentFrameFieldInfo.GetValue(m_profilerWindow);
                    float time;
                    float duration;
                    int instanceId;
                    int threadIndex;
                    if (GetMarkerInfo(markerName, currentFrameIndex, threadFilter, out threadIndex, out time, out duration, out instanceId))
                    {
                        /*
                        Debug.Log(string.Format("Setting profiler to {0} on {1} at frame {2} at {3}ms for {4}ms ({5})", 
                                                markerName, currentFrameIndex, threadFilter, time, duration, instanceId));
                         */
                        
                        if (m_selectedNameFieldInfo != null)
                            m_selectedNameFieldInfo.SetValue(selectedEntry, markerName);
                        if (m_selectedTimeFieldInfo != null)
                            m_selectedTimeFieldInfo.SetValue(selectedEntry, time);
                        if (m_selectedDurationFieldInfo != null)
                            m_selectedDurationFieldInfo.SetValue(selectedEntry, duration);
                        if (m_selectedInstanceIdFieldInfo != null)
                            m_selectedInstanceIdFieldInfo.SetValue(selectedEntry, instanceId);
                        if (m_selectedFrameIdFieldInfo != null)
                            m_selectedFrameIdFieldInfo.SetValue(selectedEntry, currentFrameIndex);
                        if (m_selectedThreadIdFieldInfo != null)
                            m_selectedThreadIdFieldInfo.SetValue(selectedEntry, threadIndex);
                        
                        // TODO : Update to fill in the total and number of instances.
                        // For now we force Instance count to 1 to avoid the incorrect info showing.
                        if (m_selectedInstanceCountFieldInfo != null)
                            m_selectedInstanceCountFieldInfo.SetValue(selectedEntry, 1);

                        // Set other values to non negative values so selection appears
                        if (m_selectedNativeIndexFieldInfo != null)
                            m_selectedNativeIndexFieldInfo.SetValue(selectedEntry, currentFrameIndex);

                        m_profilerWindow.Repaint();
                    }
                }
            }
        }

        public bool JumpToFrame(int index)
        {
            //if (!ProfilerDriver.enabled)
            //    return;

            if (!m_profilerWindow)
                return false;
            
            m_currentFrameFieldInfo.SetValue(m_profilerWindow, index - 1);
            m_profilerWindow.Repaint();
            return true;
        }
    }
}
