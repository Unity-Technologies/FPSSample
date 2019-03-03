using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;

[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class AnimStateController : MonoBehaviour 
{
    public bool fireAnimationEvents;

    public AnimGraphAsset animStateDefinition;

    void OnDisable()
    {
        Deinitialize();
    }

    public void Initialize(EntityManager entityManager, Entity owner, Entity character)
    {
        m_Animator = entityManager.GetComponentObject<Animator>(owner);

        m_Animator.fireEvents = fireAnimationEvents;
        
        GameDebug.Assert(animStateDefinition != null,"No animStateDefinition defined for AnimStateController:" + this.name);

        Profiler.BeginSample("Create graph");
        m_PlayableGraph = PlayableGraph.Create(name);
        Profiler.EndSample();
    
#if UNITY_EDITOR        
        GraphVisualizerClient.Show(m_PlayableGraph);
#endif
        
        Profiler.BeginSample("Instantiate playables");
        m_animGraph = animStateDefinition.Instatiate(entityManager, owner, m_PlayableGraph, character);
        Profiler.EndSample();
        
        m_animGraphLogic = m_animGraph as IGraphLogic;
        
        m_PlayableGraph.Play();

        var outputPlayable = Playable.Null;
        var outputPort = 0;
        m_animGraph.GetPlayableOutput(0, ref outputPlayable, ref outputPort);

        // Set graph output
        var animationOutput = AnimationPlayableOutput.Create(m_PlayableGraph, "Animator", m_Animator);
        animationOutput.SetSourcePlayable(outputPlayable);
        animationOutput.SetSourceOutputPort (outputPort);
    }

    public void Deinitialize()
    {
        if (m_PlayableGraph.IsValid())
        {
            m_animGraph.Shutdown();
            m_PlayableGraph.Destroy();
        }
    }

    public void UpdatePresentationState(GameTime time, float deltaTime)
    {
        Profiler.BeginSample("AnimStateController.Update");
        
        if (m_animGraphLogic == null)
            return;

        m_animGraphLogic.UpdateGraphLogic(time, deltaTime);
        
        Profiler.EndSample();
    }
    
    public void ApplyPresentationState(GameTime time, float deltaTime)   //
    {
        Profiler.BeginSample("AnimStateController.Apply");

        if (m_animGraph == null)
            return;
        
        m_animGraph.ApplyPresentationState(time, deltaTime);
        
        Profiler.EndSample();
    } 

    IAnimGraphInstance m_animGraph;
    IGraphLogic m_animGraphLogic;
    Animator m_Animator;
    PlayableGraph m_PlayableGraph;
}

[DisableAutoCreation]
public class HandleAnimStateCtrlSpawn : InitializeComponentSystem<AnimStateController>
{
    public HandleAnimStateCtrlSpawn(GameWorld world)
        : base(world) { }
    
    protected override void Initialize(Entity entity, AnimStateController component)
    {

        var charPresentation = EntityManager.GetComponentObject<CharacterPresentationSetup>(entity);
        
        component.Initialize(EntityManager, entity, charPresentation.character);
    }
}

[DisableAutoCreation]
public class HandleAnimStateCtrlDespawn : DeinitializeComponentSystem<AnimStateController>
{
    public HandleAnimStateCtrlDespawn(GameWorld world)
        : base(world) { }
    
    protected override void Deinitialize(Entity entity, AnimStateController component)
    {
        component.Deinitialize();
    }
}
