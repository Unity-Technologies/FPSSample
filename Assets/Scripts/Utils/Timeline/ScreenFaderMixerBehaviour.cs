using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class ScreenFaderMixerBehaviour : PlayableBehaviour
{
    bool m_FirstFrameHappened;

    AutoExposure m_Exposure;
    PostProcessVolume m_FadeVolume;

    public override void OnPlayableCreate(Playable playable)
    {
        base.OnPlayableCreate(playable);

        var layer = LayerMask.NameToLayer("PostProcess Volumes");
        if (layer == -1)
            GameDebug.LogWarning("Unable to find layer mask for camera fader");

        m_Exposure = ScriptableObject.CreateInstance<AutoExposure>();
        m_Exposure.enabled.Override(true);
        m_Exposure.keyValue.Override(0);

        m_FadeVolume = PostProcessManager.instance.QuickVolume(layer, 100.0f, m_Exposure);
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (!m_FirstFrameHappened)
        {
            m_FirstFrameHappened = true;
        }

        int inputCount = playable.GetInputCount ();

        float blendedExposure = 0.0f;
        float totalWeight = 0f;
        float greatestWeight = 0f;
        int currentInputs = 0;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<ScreenFaderBehaviour> inputPlayable = (ScriptPlayable<ScreenFaderBehaviour>)playable.GetInput(i);
            ScreenFaderBehaviour input = inputPlayable.GetBehaviour ();
            
            blendedExposure += input.exposure * inputWeight;
            totalWeight += inputWeight;

            if (inputWeight > greatestWeight)
            {
                greatestWeight = inputWeight;
            }

            if (!Mathf.Approximately (inputWeight, 0f))
                currentInputs++;
        }

        m_Exposure.keyValue.Override(blendedExposure + 0.5f * (1.0f - totalWeight));
    }

    public override void OnPlayableDestroy (Playable playable)
    {
        m_FirstFrameHappened = false;

        m_FadeVolume.enabled = false;
        GameObject.DestroyImmediate(m_FadeVolume.gameObject);
        m_FadeVolume = null;

        GameObject.DestroyImmediate(m_Exposure);
        m_Exposure = null;
    }
}
