using System;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Recorder;

namespace UnityEditor.Experimental.Recorder.Input
{
    public class AnimationInput : RecorderInput
    {
        public GameObjectRecorder m_gameObjectRecorder;
        private float m_time;

        public override void BeginRecording(RecordingSession session)
        {
            var aniSettings = (settings as AnimationInputSettings);

            if (!aniSettings.enabled)
                return;

            var srcGO = aniSettings.gameObject;
#if UNITY_2018_1_OR_NEWER
            m_gameObjectRecorder = new GameObjectRecorder(srcGO);
#else
            m_gameObjectRecorder = new GameObjectRecorder {root = srcGO};
#endif
            foreach (var binding in aniSettings.bindingType)
            {
                m_gameObjectRecorder.BindComponentsOfType(srcGO, binding, aniSettings.recursive); 
            }
            m_time = session.recorderTime;
        }

        public override void NewFrameReady(RecordingSession session)
        {
            if (session.recording && (settings as AnimationInputSettings).enabled )
            {
                m_gameObjectRecorder.TakeSnapshot(session.recorderTime - m_time);
                m_time = session.recorderTime;
            }
        }

        
    }
}