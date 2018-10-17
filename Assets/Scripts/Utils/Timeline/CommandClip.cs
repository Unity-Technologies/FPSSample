using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CommandClip : PlayableAsset, ITimelineClipAsset
{
    public CommandBehaviour template = new CommandBehaviour ();

    public ClipCaps clipCaps
    {
        get { return ClipCaps.Blending; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<CommandBehaviour>.Create (graph, template);
        return playable;    }
}
