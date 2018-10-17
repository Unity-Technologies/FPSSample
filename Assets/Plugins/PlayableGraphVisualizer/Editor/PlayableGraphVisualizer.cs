using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GraphVisualizer
{
    public class PlayableGraphVisualizer : Graph
    {
        private PlayableGraph m_PlayableGraph;

        public PlayableGraphVisualizer(PlayableGraph playableGraph)
        {
            m_PlayableGraph = playableGraph;
        }

        protected override void Populate()
        {
            if (!m_PlayableGraph.IsValid())
                return;

            int outputs = m_PlayableGraph.GetOutputCount();
            for (int i = 0; i < outputs; i++)
            {
                var output = m_PlayableGraph.GetOutput(i);
                if(output.IsOutputValid())
                {
                    AddNodeHierarchy(CreateNodeFromPlayableOutput(output));
                }
            }
        }

        protected override IEnumerable<Node> GetChildren(Node node)
        {
            // Children are the Playable Inputs.
            if(node is PlayableNode)
                return GetInputsFromPlayableNode((Playable)node.content);
            if(node is PlayableOutputNode)
                return GetInputsFromPlayableOutputNode((PlayableOutput)node.content);

            return new List<Node>();     
        }

        private List<Node> GetInputsFromPlayableNode(Playable h)
        {
            var inputs = new List<Node>();
            if (h.IsValid())
            {
                for (int port = 0; port < h.GetInputCount(); ++port)
                {
                    Playable playable = h.GetInput(port);
                    if (playable.IsValid())
                    {
                        float weight = h.GetInputWeight(port);
                        Node node = CreateNodeFromPlayable(playable, weight);
                        inputs.Add(node);
                    }
                }
            }
            return inputs;
        }

        private List<Node> GetInputsFromPlayableOutputNode(PlayableOutput h)
        {
            var inputs = new List<Node>();
            if (h.IsOutputValid())
            {            
                Playable playable = h.GetSourcePlayable();
                if (playable.IsValid())
                {
                    Node node = CreateNodeFromPlayable(playable, 1);
                    inputs.Add(node);
                }
            }
            return inputs;
        }

        private PlayableNode CreateNodeFromPlayable(Playable h, float weight)
        {
            var type = h.GetPlayableType();
            if (type == typeof(AnimationClipPlayable))
                return new AnimationClipPlayableNode(h, weight);
            if (type == typeof(AnimationLayerMixerPlayable))
                return new AnimationLayerMixerPlayableNode(h, weight);
            return new PlayableNode(h, weight);
        }

        private PlayableOutputNode CreateNodeFromPlayableOutput(PlayableOutput h)
        {
            return new PlayableOutputNode(h);
        }
    }
}
