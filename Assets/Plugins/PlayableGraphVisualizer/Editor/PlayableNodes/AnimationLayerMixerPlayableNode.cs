using System.Text;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GraphVisualizer
{
    public class AnimationLayerMixerPlayableNode : PlayableNode
    {
        public AnimationLayerMixerPlayableNode(Playable content, float weight = 1.0f)
            : base(content, weight)
        {
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(base.ToString());

            var p = (Playable) content;
            if (p.IsValid())
            {
                var almp = (AnimationLayerMixerPlayable) p;
                for (uint i = 0; i < almp.GetInputCount(); ++i)
                    sb.AppendLine(InfoString(string.Format("IsLayerAdditive #{0}", i + 1), almp.IsLayerAdditive(i)));
            }

            return sb.ToString();
        }
    }
}