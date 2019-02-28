using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;

[CreateAssetMenu(fileName = "AnimatorController", menuName = "FPS Sample/Animation/AnimGraph/AnimatorController")]
public class AnimGraph_AnimatorController : AnimGraphAsset
{
    public RuntimeAnimatorController animatorController;

    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        var animState = new Instance(entityManager, owner, graph, animStateOwner, this);
        return animState;
    }
    
    class Instance : IAnimGraphInstance
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_AnimatorController settings)
        {
            m_settings = settings;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;

            m_characterAnimatorController = new CharacterAnimatorController(graph, m_settings.animatorController);
            m_characterAnimatorController.Start();
        }

        public void Shutdown()
        {
        }

        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
        }

        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_characterAnimatorController.GetRootPlayable();
            playablePort = 0;
        }

        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            m_characterAnimatorController.Update(ref animState);
        }

        AnimGraph_AnimatorController m_settings;
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;
        CharacterAnimatorController m_characterAnimatorController;
    }
}
