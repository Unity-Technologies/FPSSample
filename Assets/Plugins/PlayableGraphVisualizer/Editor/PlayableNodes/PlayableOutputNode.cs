using System;
using System.Text;
using UnityEngine.Playables;

namespace GraphVisualizer
{
    public class PlayableOutputNode : SharedPlayableNode
    {
        public PlayableOutputNode(PlayableOutput content)
            : base(content, content.GetWeight(), true)
        {
        }

        public override Type GetContentType()
        {
            PlayableOutput po = PlayableOutput.Null;
            try
            {
                po = (PlayableOutput) content;
            }
            catch
            {
                // Ignore.
            }

            return po.IsOutputValid() ? po.GetPlayableOutputType() : null;
        }

        public override string GetContentTypeShortName()
        {
            // Remove the extra Playable at the end of the Playable types.
            string shortName = base.GetContentTypeShortName();
            string cleanName = RemoveFromEnd(shortName, "PlayableOutput") + "Output";
            return string.IsNullOrEmpty(cleanName) ? shortName : cleanName;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(InfoString("Handle", GetContentTypeShortName()));

            var po = (PlayableOutput) content;
            if (po.IsOutputValid())
            {
                sb.AppendLine(InfoString("IsValid", po.IsOutputValid()));
                sb.AppendLine(InfoString("Weight", po.GetWeight()));
                sb.AppendLine(InfoString("SourceOutputPort", po.GetSourceOutputPort()));
            }

            return sb.ToString();
        }
    }
}