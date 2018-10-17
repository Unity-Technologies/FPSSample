using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

[Serializable]
public class CommandBehaviour : PlayableBehaviour
{
    public string commandAtStart;

    bool triggered = false;

    public override void OnGraphStart(Playable playable)
    {
        triggered = false;
        base.OnGraphStart(playable);
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        base.OnBehaviourPlay(playable, info);
        if(!triggered)
        {
            triggered = true;
            Console.EnqueueCommandNoHistory(commandAtStart);
        }
    }
}
