using System;
using System.Collections.Generic;

namespace UnityEngine.Recorder
{
    public enum ERecordingSessionStage
    {
        BeginRecording,
        NewFrameStarting,
        NewFrameReady,
        FrameDone,
        EndRecording,
        SessionCreated
    }

    /// <summary>
    /// What is this: 
    /// Motivation  : 
    /// Notes: 
    /// </summary>    
    public abstract class Recorder : ScriptableObject
    {
        static int sm_CaptureFrameRateCount;
        bool m_ModifiedCaptureFR;

        public int recordedFramesCount { get; set; }
        
        protected List<RecorderInput> m_Inputs;

        public virtual void Awake()
        {
            sm_CaptureFrameRateCount = 0;
        }

        public virtual void Reset()
        {
            recordedFramesCount = 0;
            recording = false;
        }

        protected virtual void OnDestroy()
        {
            if (m_ModifiedCaptureFR )
            {
                sm_CaptureFrameRateCount--;
                if (sm_CaptureFrameRateCount == 0)
                {
                    Time.captureFramerate = 0;
                    if (Verbose.enabled)
                        Debug.Log("Recorder resetting 'CaptureFrameRate' to zero");
                }
            }
        }

        public abstract RecorderSettings settings { get; set; }

        public virtual void SessionCreated(RecordingSession session)
        {
            if (Verbose.enabled)
                Debug.Log(string.Format("Recorder {0} session created", GetType().Name));

            settings.SelfAdjustSettings(); // ignore return value.

            var fixedRate = settings.m_FrameRateMode == FrameRateMode.Constant ? (int)settings.m_FrameRate : 0;
            if (fixedRate > 0)
            {
                if (Time.captureFramerate != 0 && fixedRate != Time.captureFramerate )
                    Debug.LogError(string.Format("Recorder {0} is set to record at a fixed rate and another component has already set a conflicting value for [Time.captureFramerate], new value being applied : {1}!", GetType().Name, fixedRate));
                else if( Time.captureFramerate == 0 && Verbose.enabled )
                    Debug.Log("Frame recorder set fixed frame rate to " + fixedRate);

                Time.captureFramerate = (int)fixedRate;

                sm_CaptureFrameRateCount++;
                m_ModifiedCaptureFR = true;
            }

            m_Inputs = new List<RecorderInput>();
            foreach (var inputSettings in settings.inputsSettings)
            {
                var input = Activator.CreateInstance(inputSettings.inputType) as RecorderInput;
                input.settings = inputSettings;
                m_Inputs.Add(input);
                SignalInputsOfStage(ERecordingSessionStage.SessionCreated, session);
            }
        }

        public virtual bool BeginRecording(RecordingSession session)
        {
            if (recording)
                throw new Exception("Already recording!");

            if (Verbose.enabled)
                Debug.Log(string.Format("Recorder {0} starting to record", GetType().Name));
         
            return recording = true;
        }

        public virtual void EndRecording(RecordingSession ctx)
        {
            if (!recording)
                return;
            recording = false;

            if (m_ModifiedCaptureFR )
            {
                m_ModifiedCaptureFR = false;
                sm_CaptureFrameRateCount--;
                if (sm_CaptureFrameRateCount == 0)
                {
                    Time.captureFramerate = 0;
                    if (Verbose.enabled)
                        Debug.Log("Recorder resetting 'CaptureFrameRate' to zero");
                }
            }

            foreach (var input in m_Inputs)
            {
                if (input is IDisposable)
                    (input as IDisposable).Dispose();
            }

            if(Verbose.enabled)
                Debug.Log(string.Format("{0} recording stopped, total frame count: {1}", GetType().Name, recordedFramesCount));
        }
        public abstract void RecordFrame(RecordingSession ctx);
        public virtual void PrepareNewFrame(RecordingSession ctx)
        {
        }

        public virtual bool SkipFrame(RecordingSession ctx)
        {
            return !recording 
                || (ctx.frameIndex % settings.m_CaptureEveryNthFrame) != 0 
                || ( settings.m_DurationMode == DurationMode.TimeInterval && ctx.m_CurrentFrameStartTS < settings.m_StartTime )
                || ( settings.m_DurationMode == DurationMode.FrameInterval && ctx.frameIndex < settings.m_StartFrame )
                || ( settings.m_DurationMode == DurationMode.SingleFrame && ctx.frameIndex < settings.m_StartFrame );
        }

        public bool recording { get; protected set; }

        public void SignalInputsOfStage(ERecordingSessionStage stage, RecordingSession session)
        {
            if (m_Inputs == null)
                return;

            switch (stage)
            {
                case ERecordingSessionStage.SessionCreated:
                    foreach( var input in m_Inputs )
                        input.SessionCreated(session);
                    break;
                case ERecordingSessionStage.BeginRecording:
                    foreach( var input in m_Inputs )
                        input.BeginRecording(session);
                    break;
                case ERecordingSessionStage.NewFrameStarting:
                    foreach( var input in m_Inputs )
                        input.NewFrameStarting(session);
                    break;
                case ERecordingSessionStage.NewFrameReady:
                    foreach( var input in m_Inputs )
                        input.NewFrameReady(session);
                    break;
                case ERecordingSessionStage.FrameDone:
                    foreach( var input in m_Inputs )
                        input.FrameDone(session);
                    break;
                case ERecordingSessionStage.EndRecording:
                    foreach( var input in m_Inputs )
                        input.EndRecording(session);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("stage", stage, null);
            }
        }
    }
}
