using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "Death", menuName = "FPS Sample/Animation/AnimGraph/Death")]
public class AnimGraph_Death : AnimGraphAsset
{
    public AnimationClip[] anims;

    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        return new Instance(graph, this); 
    }
    
    class Instance : IAnimGraphInstance
    {
        public Instance(PlayableGraph graph, AnimGraph_Death settings)
        {
            m_settings = settings;
            m_Mixer = AnimationMixerPlayable.Create(graph);
            m_NumAnims = 0;
        
            foreach (var animClip in m_settings.anims)
            {
                if (animClip != null)
                {
                    var clipPlayable = AnimationClipPlayable.Create(graph, animClip);
                    clipPlayable.SetDuration(animClip.length);
                    clipPlayable.SetApplyFootIK(false);
                    clipPlayable.Pause();
                    var port = m_Mixer.AddInput(clipPlayable, 0);
                    m_Mixer.SetInputWeight(port, 0f);
                    m_NumAnims++;
                }
            }   
        }

        public void Shutdown()
        {
        }

        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
        }

        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_Mixer;
            playablePort = 0;
        }

        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Death.Apply");
            
            if (!m_Started)
            {
                m_Started = true;

                var oldState = Random.state;        
                Random.InitState((int)Time.time);
                var currentAnim = Random.Range(0, m_NumAnims);
                Random.state = oldState;
        
                m_Mixer.SetInputWeight(currentAnim, 1f);
                m_Mixer.GetInput(currentAnim).Play();
            }
            
            Profiler.EndSample();
        }

        AnimationMixerPlayable m_Mixer;
        AnimGraph_Death m_settings;
        int m_NumAnims;
        bool m_Started;
    }
}
