using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "AdditiveAnimation", menuName = "FPS Sample/Animation/AnimGraph/AdditiveAnimation")]
public class AnimGraph_AdditiveAnimation : AnimGraphAsset
{
    public AnimationClip clip;
    [Range(0f, 1f)]
    public float weight;

	public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
	    Entity animStateOwner)
	{
		return new Instance(entityManager, owner, graph, this);
	}
	
    class Instance : IAnimGraphInstance
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, AnimGraph_AdditiveAnimation settings)
        {
            m_Settings = settings;
            m_graph = graph;
            m_Mixer = AnimationLayerMixerPlayable.Create(m_graph, 2);
            m_Mixer.SetInputWeight(0, 1f);
            m_Mixer.SetInputWeight(1, 1f);
            m_Mixer.SetLayerAdditive(1, true);
            m_AdditiveClip = AnimationClipPlayable.Create(m_graph, m_Settings.clip);
            m_AdditiveClip.SetApplyFootIK(false);
            m_graph.Connect(m_AdditiveClip, 0, m_Mixer, 1);
        }
    
        public void Shutdown()
        {
        }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
            m_graph.Connect(playable, playablePort, m_Mixer, 0);
        }
        
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_Mixer;
            playablePort = 0;
        }
   
        public void ResetNetwork( float timeOffset)
        {
        }
    
        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
        }
    
        AnimationLayerMixerPlayable m_Mixer;
        AnimGraph_AdditiveAnimation m_Settings;
        AnimationClipPlayable m_AdditiveClip;
        
        PlayableGraph m_graph;
    }
}
