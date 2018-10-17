using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class SoundTimelineClip : PlayableAsset, ITimelineClipAsset
{
    SoundTimelineBehaviour template = new SoundTimelineBehaviour();
    public SoundDef sound;
    public SoundTimelineBehaviour.SoundPosition position = SoundTimelineBehaviour.SoundPosition.None;

    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<SoundTimelineBehaviour>.Create(graph, template);
        SoundTimelineBehaviour clone = playable.GetBehaviour();
        clone.sound = sound;
        clone.position = position;
        return playable;
    }
}
