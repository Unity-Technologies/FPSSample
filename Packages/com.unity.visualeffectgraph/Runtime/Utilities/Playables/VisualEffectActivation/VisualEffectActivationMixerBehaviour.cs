using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Experimental.VFX;

public class VisualEffectActivationMixerBehaviour : PlayableBehaviour
{
    bool[] enabledStates;

    // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        VisualEffect vfxComponent = playerData as VisualEffect;

        if (!vfxComponent)
            return;

        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            bool newEnabledState = inputWeight != 0.0f;

            var inputPlayable = (ScriptPlayable<VisualEffectActivationBehaviour>)playable.GetInput(i);
            var input = inputPlayable.GetBehaviour();

            if (enabledStates[i] != newEnabledState)
            {
                if (newEnabledState)
                    input.SendEventEnter(vfxComponent);
                else
                    input.SendEventExit(vfxComponent);

                enabledStates[i] = newEnabledState;
            }
        }
    }

    public override void OnPlayableCreate(Playable playable)
    {
        enabledStates = new bool[playable.GetInputCount()];
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        enabledStates = null;
    }
}
