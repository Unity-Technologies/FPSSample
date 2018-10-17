using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class SoundTimelineBehaviour : PlayableBehaviour
{
    public enum SoundPosition
    {
        None,
        Position,
        Follow,
    }

    public SoundDef sound;
    public SoundPosition position = SoundPosition.None;
    public bool triggered;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        triggered = true;
    }
}
