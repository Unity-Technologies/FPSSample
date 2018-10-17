using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[TrackColor(0.5f,0.8f,0.5f)]
[TrackClipType(typeof(SoundTimelineClip))]
[TrackBindingType(typeof(Transform))]
public class SoundTimelineTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<SoundTimelineMixerBehaviour>.Create(graph, inputCount);
    }
}
