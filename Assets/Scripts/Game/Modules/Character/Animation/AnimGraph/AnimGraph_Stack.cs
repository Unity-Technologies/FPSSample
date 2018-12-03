using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Boo.Lang.Environments;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "Stack", menuName = "FPS Sample/Animation/AnimGraph/Stack")]
public class AnimGraph_Stack : AnimGraphAsset 
{
    public List<AnimGraphAsset> rootNodes = new List<AnimGraphAsset>();

    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        var instance = new GraphInstance(entityManager, owner, graph, animStateOwner, this);
        return instance;
    }

    class GraphInstance : IAnimGraphInstance, IGraphLogic
    {
        public GraphInstance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner,
            AnimGraph_Stack graphAsset)
        {
            for (var i = 0; i < graphAsset.rootNodes.Count; i++)
            {
                var subGraph = graphAsset.rootNodes[i].Instatiate(entityManager, owner, graph, animStateOwner);
                subGraph.SetPlayableInput(0, m_RootPlayable, 0);

                var outputPort = 0;
                subGraph.GetPlayableOutput(0, ref m_RootPlayable, ref outputPort);
             

                var animStackEntry = new AnimStackEntry()
                {
                    subGraph = subGraph,
                    graphLogic = subGraph as IGraphLogic
                };
                m_subGraphs.Add(animStackEntry);
            }
        }

        public void Shutdown()
        {
            for (var i = 0; i < m_subGraphs.Count; i++)
            {
                m_subGraphs[i].subGraph.Shutdown();
            }
        }

        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
        }

        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_RootPlayable;
            playablePort = 0;
        }

        public void UpdateGraphLogic(GameTime time, float deltaTime)
        {
            for (var i = 0; i < m_subGraphs.Count; i++)
            {
                if (m_subGraphs[i].graphLogic == null)
                    continue;
                m_subGraphs[i].graphLogic.UpdateGraphLogic(time, deltaTime);
            }
        }

        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            for (var i = 0; i < m_subGraphs.Count; i++)
            {
                m_subGraphs[i].subGraph.ApplyPresentationState(time, deltaTime);
            }
        }

        struct AnimStackEntry
        {
            public IAnimGraphInstance subGraph;
            public IGraphLogic graphLogic;
        }

        Playable m_RootPlayable;
        List<AnimStackEntry> m_subGraphs = new List<AnimStackEntry>(); 
    }
}
