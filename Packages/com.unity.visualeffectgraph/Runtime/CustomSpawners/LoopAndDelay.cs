using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [UnityEngine.Scripting.Preserve]
    public class LoopAndDelay : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
            [Tooltip("Number of Loops (< 0 for infinite), evaluated when Context Start is hit")]
            public int LoopCount = 1;
            [Tooltip("Duration of one loop, evaluated every loop")]
            public float LoopDuration = 4.0f;
            [Tooltip("Duration of in-between delay (after each loop), evaluated every loop")]
            public float Delay = 1.0f;
        }

        int m_LoopMaxCount;
        int m_LoopCurrentIndex;
        float m_WaitingForTotalTime;

        static private readonly int loopCountPropertyID = Shader.PropertyToID("LoopCount");
        static private readonly int loopDurationPropertyID = Shader.PropertyToID("LoopDuration");
        static private readonly int delayPropertyID = Shader.PropertyToID("Delay");

        public sealed override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            // Evaluate Loop Count only when hitting start;
            // LoopCount < 0 means infinite mode
            // LoopCount == 0 means no spawn
            m_LoopMaxCount = vfxValues.GetInt(loopCountPropertyID);
            m_WaitingForTotalTime = vfxValues.GetFloat(loopDurationPropertyID);
            m_LoopCurrentIndex = 0;
            if (m_LoopMaxCount == m_LoopCurrentIndex)
            {
                state.playing = false;
            }
        }

        public sealed override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            if (m_LoopCurrentIndex != m_LoopMaxCount && state.totalTime > m_WaitingForTotalTime)
            {
                if (state.playing)
                {
                    m_WaitingForTotalTime = state.totalTime + vfxValues.GetFloat(delayPropertyID); 
                    state.playing = false; //We are in playing state, if m_LoopCurrentIndex + 1 == m_LoopMaxCount, we have finished here
                    m_LoopCurrentIndex = m_LoopCurrentIndex + 1 > 0 ? m_LoopCurrentIndex + 1 : 0; //It's possible to count to infinite if m_LoopMaxCount < 0, this ternary avoid stop going back to zero
                }
                else
                {
                    m_WaitingForTotalTime = vfxValues.GetFloat(loopDurationPropertyID);
                    state.totalTime = 0.0f;
                    state.playing = true; //We are in not playing state, we was waiting for the moment when restart will be launch
                }
            }
        }

        public sealed override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            m_LoopCurrentIndex = m_LoopMaxCount;
        }
    }
}
