using System;
using System.Collections;

namespace UnityEngine.Recorder
{

    /// <summary>
    /// What is this: 
    /// Motivation  : 
    /// Notes: 
    /// </summary>    
    [ExecuteInEditMode]
    public class RecorderComponent : MonoBehaviour
    {
        public bool autoExitPlayMode { get; set; }
        public RecordingSession session { get; set; }

        public void Update()
        {
            if (session != null && session.recording)
                session.PrepareNewFrame();
        }

        IEnumerator RecordFrame()
        {
            yield return new WaitForEndOfFrame();
            if (session != null && session.recording)
            {
                session.RecordFrame();

                switch (session.m_Recorder.settings.m_DurationMode)
                {
                    case DurationMode.Manual:
                        break;
                    case DurationMode.SingleFrame:
                    {
                        if (session.m_Recorder.recordedFramesCount == 1)
                            enabled = false;
                        break;
                    }
                    case DurationMode.FrameInterval:
                    {
                        if (session.frameIndex > session.settings.m_EndFrame)
                            enabled = false;
                        break;
                    }
                    case DurationMode.TimeInterval:
                    {
                        if (session.settings.m_FrameRateMode == FrameRateMode.Variable)
                        {
                            if (session.m_CurrentFrameStartTS >= session.settings.m_EndTime)
                                enabled = false;
                        }
                        else
                        {
                            var expectedFrames = (session.settings.m_EndTime - session.settings.m_StartTime) * session.settings.m_FrameRate;
                            if (session.RecordedFrameSpan >= expectedFrames)
                                enabled = false;
                        }
                        break;
                    }
                }
            }
        }

        public void LateUpdate()
        {
            if (session != null && session.recording)
                StartCoroutine(RecordFrame());
        }

        public void OnDisable()
        {
            if (session != null)
            {
                session.Dispose();
                session = null;

#if UNITY_EDITOR
                if (autoExitPlayMode)
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        }

        public void OnDestroy()
        {
            if (session != null)
                session.Dispose();
        }
    }
}
