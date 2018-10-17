using UnityEngine.Timeline;

namespace UnityEngine.Recorder.Timeline
{

    /// <summary>
    /// What is it: Declares a type of Timeline Track that can host Recording clips
    /// Motivation: Allow Timeline to trigger recordings
    /// 
    /// Note: Instances of this call Own their associated Settings asset's lifetime.
    /// </summary>
    [System.Serializable]
    [TrackClipType(typeof(RecorderClip))]
    //[TrackMediaType(TimelineAsset.MediaType.Script)]
    [TrackColor(0f, 0.53f, 0.08f)]
    public class RecorderTrack : TrackAsset
    {


    }

}
