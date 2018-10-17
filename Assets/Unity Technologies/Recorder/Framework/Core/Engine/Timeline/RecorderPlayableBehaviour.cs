using System;
using UnityEngine.Playables;

namespace UnityEngine.Recorder.Timeline
{
    /// <summary>
    /// What is it: Implements a playable that records something triggered by a Timeline Recorder Clip.
    /// Motivation: Allow Timeline to trigger recordings
    /// 
    /// Notes: 
    ///     - Totally ignores the time info comming from the playable infrastructure. Only conciders scaled time.
    ///     - Does not support multiple OnGraphStart...
    ///     - It relies on WaitForEndOfFrameComponent to inform the Session object that it's time to record to frame.
    /// </summary>    
    public class RecorderPlayableBehaviour : PlayableBehaviour
    {
        PlayState m_PlayState = PlayState.Paused;
        public RecordingSession session { get; set; }
        WaitForEndOfFrameComponent endOfFrameComp;

        public Action OnEnd;

        public override void OnGraphStart(Playable playable)
        {
            if (session != null)
            {
                // does not support multiple starts...
                session.SessionCreated();
                m_PlayState = PlayState.Paused;
            }
        }

        public override void OnGraphStop(Playable playable)
        {
            if (session != null && session.recording)
            {
                session.EndRecording();
                session.Dispose();
                session = null;

                if (OnEnd != null)
                    OnEnd();
            }
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if (session != null && session.recording)
            {
                session.PrepareNewFrame();
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (session != null)
            {
                if (endOfFrameComp == null)
                {
                    endOfFrameComp = session.m_RecorderGO.AddComponent<WaitForEndOfFrameComponent>();
                    endOfFrameComp.m_playable = this;
                }
            }
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (session == null)
                return;

            // Assumption: OnPlayStateChanged( PlayState.Playing ) ONLY EVER CALLED ONCE for this type of playable.
            m_PlayState = PlayState.Playing;
            session.BeginRecording();                
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (session == null)
                return;

            if (session.recording && m_PlayState == PlayState.Playing)
            {
                session.EndRecording();
                session.Dispose();
                session = null;

                if (OnEnd != null)
                    OnEnd();
            }

            m_PlayState = PlayState.Paused;
        }

        public void FrameEnded()
        {
            if (session != null && session.recording)
                session.RecordFrame();
        }
    }
}
