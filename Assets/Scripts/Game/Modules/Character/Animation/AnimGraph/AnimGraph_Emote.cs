using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Emote", menuName = "FPS Sample/Animation/AnimGraph/Emote")]
public class AnimGraph_Emote : AnimGraphAsset
{
    [Serializable]
    public struct EmoteData
    {
        public AnimationClip clip;
        public bool keepPlaying;
    }

    public float blendTime = 0.2f;
    public EmoteData[] emoteData;

    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        return new Instance(entityManager, owner, graph, animStateOwner, this);
    }

    class Instance : IAnimGraphInstance, IGraphState
    {
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;
        AnimGraph_Emote m_settings;

        AnimationMixerPlayable m_mixer;
        SimpleTranstion<AnimationMixerPlayable> m_Transition;

        int activePort;
        int lastEmoteCount = -1;

        Entity ability;

        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_Emote settings)
        {
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;
            m_settings = settings;

            m_mixer = AnimationMixerPlayable.Create(graph, settings.emoteData.Length + 1);

            var ports = new int[m_settings.emoteData.Length + 1];
            for (var i = 0; i < ports.Length; i++)
            {
                ports[i] = i;
            }            
            
            m_Transition = new SimpleTranstion<AnimationMixerPlayable>(m_mixer, ports);

            for (int i = 0; i < settings.emoteData.Length; i++)
            {
                var clip = settings.emoteData[i].clip;
                var clipPlayable = AnimationClipPlayable.Create(graph, clip);
                clipPlayable.SetDuration(clip.length);
                clipPlayable.Pause();

                var port = i + 1;
                m_mixer.ConnectInput(port, clipPlayable, 0);
                m_mixer.SetInputWeight(port, 0.0f);
            }
        }

        public void Shutdown() { }

        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
            m_mixer.ConnectInput(0, playable, playablePort);
            m_mixer.SetInputWeight(0, 1.0f);
        }

        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_mixer;
            playablePort = 0;
        }

        public void UpdatePresentationState(bool firstUpdate, GameTime time, float deltaTime) { }

        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Emote.Apply");
            
            // Find ability entity
            if (!m_EntityManager.Exists(ability))
            {
                var charRepAll = m_EntityManager.GetComponentData<CharacterReplicatedData>(m_AnimStateOwner);
                ability = charRepAll.FindAbilityWithComponent(m_EntityManager, typeof(Ability_Emote.SerializerState));
            }

            if (ability == Entity.Null)
            {
                Profiler.EndSample();
                return;
            }

            var abilityState = m_EntityManager.GetComponentData<Ability_Emote.SerializerState>(ability);

            var blendVel = m_settings.blendTime > 0 ? 1.0f / m_settings.blendTime : 1.0f / deltaTime;

            if (abilityState.emote == CharacterEmote.None)
            {
                if (activePort != 0)
                {
                    GameDebug.Log("anim stopping");
                    activePort = 0;
                }

                m_Transition.Update(activePort, blendVel, Time.deltaTime);
                Profiler.EndSample();
                return;
            }

            var requestedPort = (int)abilityState.emote;
            if (requestedPort != activePort || abilityState.emoteCount != lastEmoteCount)
            {
                lastEmoteCount = abilityState.emoteCount;
                StartEmote(requestedPort);
                m_Transition.Update(activePort, blendVel, Time.deltaTime);
                Profiler.EndSample();
                return;
            }

            // Timeout.
            if (activePort > 0)
            {
                if (!m_settings.emoteData[activePort - 1].keepPlaying)
                {
                    var clipPlayable = m_mixer.GetInput(activePort);
                    if (clipPlayable.GetPlayState() == PlayState.Playing && clipPlayable.GetTime() >= clipPlayable.GetDuration() - m_settings.blendTime)
                    {
                        clipPlayable.Pause();
                        activePort = 0;

                        var internalState = m_EntityManager.GetComponentData<Ability_Emote.InternalState>(ability);
                        internalState.animDone = 1;
                        m_EntityManager.SetComponentData(ability, internalState);
                    }
                }
            }

            m_Transition.Update(activePort, blendVel, Time.deltaTime);
            
            Profiler.EndSample();
        }

        void StartEmote(int port)
        {
            activePort = port;

            var clipPlayable = m_mixer.GetInput(port);

            GameDebug.Assert(clipPlayable.IsValid(), "playable in port:{0} is invalid", port);
            
            clipPlayable.SetTime(0);
            clipPlayable.Play();
        }
    }
}
