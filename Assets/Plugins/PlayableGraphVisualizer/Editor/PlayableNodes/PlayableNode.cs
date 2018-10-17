using System;
using System.Text;
using UnityEngine.Playables;

namespace GraphVisualizer
{
    public class PlayableNode : SharedPlayableNode
    {
        public PlayableNode(Playable content, float weight = 1.0f)
            : base(content, weight, content.GetPlayState() == PlayState.Playing)
        {
        }

        public override Type GetContentType()
        {
            Playable p = Playable.Null;
            try
            {
                p = (Playable) content;
            }
            catch
            {
                // Ignore.
            }

            return p.IsValid() ? p.GetPlayableType() : null;
        }

        public override string GetContentTypeShortName()
        {
            // Remove the extra Playable at the end of the Playable types.
            string shortName = base.GetContentTypeShortName();
            string cleanName = RemoveFromEnd(shortName, "Playable");
            return string.IsNullOrEmpty(cleanName) ? shortName : cleanName;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(InfoString("Handle", GetContentTypeShortName()));

            var p = (Playable) content;
            sb.AppendLine(InfoString("IsValid", p.IsValid()));
            if (p.IsValid())
            {
                sb.AppendLine(InfoString("IsDone", p.IsDone()));
                sb.AppendLine(InfoString("InputCount", p.GetInputCount()));
                sb.AppendLine(InfoString("OutputCount", p.GetOutputCount()));
                sb.AppendLine(InfoString("PlayState", p.GetPlayState()));
                sb.AppendLine(InfoString("Speed", p.GetSpeed()));
                sb.AppendLine(InfoString("Duration", p.GetDuration()));
                sb.AppendLine(InfoString("Time", p.GetTime()));
            }

            return sb.ToString();
        }
    }
}