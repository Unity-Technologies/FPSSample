using System;

namespace UnityEngine.Recorder
{

    /// <summary>
    /// What is this: 
    /// Motivation  : 
    /// Notes: 
    /// </summary>    
    public class RecorderInput : IDisposable
    {
        public int SourceID { get; set; }
        public RecorderInputSetting settings { get; set; }

        ~RecorderInput()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            GC.SuppressFinalize(this);
        }

        public virtual void SessionCreated(RecordingSession session) {}

        public virtual void BeginRecording(RecordingSession session) {}

        public virtual void NewFrameStarting(RecordingSession session) {}

        public virtual void NewFrameReady(RecordingSession session) {}

        public virtual void FrameDone(RecordingSession session) {}

        public virtual void EndRecording(RecordingSession session) {}
    }
}
