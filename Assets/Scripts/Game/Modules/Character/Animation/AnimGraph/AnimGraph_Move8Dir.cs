using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "Move8Dir", menuName = "FPS Sample/Animation/AnimGraph/Move8Dir")]
public class AnimGraph_Move8Dir : AnimGraphAsset
{
    public float animMovePlaySpeed = 1.0f;
    public float damping = 0.1f;
    public float maxStep = 15f;
   
    [Range(0f, 1f)]
    [Tooltip("The max. time between exiting ground move and re-entering, before a state reset is triggered")]
    public float stateResetWindow;    
    
    public AnimationClip animAim;
    public List<BlendSpaceNode> blendSpaceNodes;

    public bool enableIK;
    public bool useVariableMoveSpeed; // Experimentatal
    
    public ActionAnimationDefinition[] actionAnimations;

    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        var animState = new Instance(entityManager, owner, graph, animStateOwner, this);
        return animState;
    }
        
    class Instance : IAnimGraphInstance, IGraphState
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_Move8Dir settings)
        {
            m_settings = settings;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;
            m_locomotionMixer = AnimationLayerMixerPlayable.Create(graph, 3);
    
            // Movement
            m_BlendTree = new BlendTree2dSimpleDirectional(graph, settings.blendSpaceNodes);
            graph.Connect(m_BlendTree.GetRootPlayable(), 0, m_locomotionMixer, 0);
            m_locomotionMixer.SetInputWeight(0, 1.0f);
            m_BlendTree.masterSpeed = m_settings.animMovePlaySpeed;
    
            // Aim
            if (settings.animAim != null)
            {
                m_clipAim = AnimationClipPlayable.Create(graph, settings.animAim);
                m_clipAim.SetApplyFootIK(false);
                m_clipAim.Pause();
                m_aimTimeFactor = m_clipAim.GetAnimationClip().length / 180.0f;
    
                m_locomotionMixer.SetLayerAdditive(1, true);
                graph.Connect(m_clipAim, 0, m_locomotionMixer, 1);
                m_locomotionMixer.SetInputWeight(1, 1.0f);
            }
    
            // Actions
            m_actionMixer = AnimationLayerMixerPlayable.Create(graph);
            var port = m_actionMixer.AddInput(m_locomotionMixer, 0);
            m_actionMixer.SetInputWeight(port, 1);
    
            m_actionAnimationHandler = new ActionAnimationHandler(m_actionMixer, settings.actionAnimations);
        }
    
        public void Shutdown()
        {
        }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
        }
    
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = m_actionMixer;
            playablePort = 0;
        }
    
        public void UpdatePresentationState(bool firstUpdate, GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Move8Dir.Update");
            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            var charState = m_EntityManager.GetComponentData<CharacterPredictedData>(m_AnimStateOwner);

            
            
            if (firstUpdate)
            {
                // Do phase projection for time not spent in state
                var ticksSincePreviousGroundMove = time.tick - animState.lastGroundMoveTick;                
                if (ticksSincePreviousGroundMove > 1)
                {
                    animState.locomotionPhase += m_playSpeed * (ticksSincePreviousGroundMove - 1f);
                } 
                
                // Reset the phase and position in blend space if appropriate
                var timeSincePreviousGroundMove = ticksSincePreviousGroundMove / (float)time.tickRate;                
                if (animState.previousCharLocoState != CharacterPredictedData.LocoState.GroundMove && timeSincePreviousGroundMove >  m_settings.stateResetWindow)
                {
//                    Debug.Log("Reset movement run! (Ticks since: " + ticksSincePreviousGroundMove + " Time since: " + timeSincePreviousGroundMove + ")");
                    animState.locomotionPhase = 0f;
                    animState.moveAngleLocal = CalculateMoveAngleLocal(animState.rotation, animState.moveYaw);

                    if (m_settings.useVariableMoveSpeed)
                    {
                        animState.locomotionVector = AngleToPosition(animState.moveAngleLocal) * charState.velocity.magnitude;                        
                    }
                    else
                    {
                        animState.locomotionVector = AngleToPosition(animState.moveAngleLocal);
                    }
                    m_CurrentVelocity = Vector2.zero;
                }
            }
            
            // Get new local move angle
            animState.rotation = animState.aimYaw;
            animState.moveAngleLocal = CalculateMoveAngleLocal(animState.rotation, animState.moveYaw);
    
            #if UNITY_EDITOR
                m_BlendTree.masterSpeed =  m_settings.animMovePlaySpeed;
                m_BlendTree.footIk = m_settings.enableIK;
            #endif
            
            // Smooth through blend tree
            var targetBlend = AngleToPosition(animState.moveAngleLocal);
            if (m_settings.useVariableMoveSpeed) // Experimental
            {
                targetBlend = AngleToPosition(animState.moveAngleLocal) * charState.velocity.magnitude;
            }

            animState.locomotionVector = Vector2.SmoothDamp(animState.locomotionVector, targetBlend, ref m_CurrentVelocity, m_settings.damping, m_settings.maxStep, deltaTime);
            
            // Update position and increment phase
            m_playSpeed = 1f / m_BlendTree.SetBlendPosition(animState.locomotionVector, false) * deltaTime;
            m_DoUpdateBlendPositions = false;
            animState.locomotionPhase += m_playSpeed;
            
            m_EntityManager.SetComponentData(m_AnimStateOwner,animState);
            
            Profiler.EndSample();
        }

        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Move8Dir.Apply");
            
            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);

            if (m_DoUpdateBlendPositions)
            {
                m_BlendTree.SetBlendPosition(animState.locomotionVector, false);
            }
            
            m_BlendTree.UpdateGraph();
            m_BlendTree.SetPhase(animState.locomotionPhase);
            
            m_clipAim.SetTime(animState.aimPitch * m_aimTimeFactor);
    
            var characterActionDuration = time.DurationSinceTick(animState.charActionTick);
            m_actionAnimationHandler.UpdateAction(animState.charAction, characterActionDuration);
                        
            Profiler.EndSample();
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float CalculateMoveAngleLocal(float rotation, float moveYaw)
        {
            // Get new local move angle
            var moveAngleLocal = Mathf.DeltaAngle(rotation, moveYaw);
    
            // We cant blend running sideways and running backwards so in range 90->135 we snap to either sideways or backwards
            var absMoveAngle = Mathf.Abs(moveAngleLocal);
            if (absMoveAngle > 90 && absMoveAngle < 135)
            {
                var sign = Mathf.Sign(moveAngleLocal);
                moveAngleLocal = absMoveAngle > 112.5f ? sign * 135.0f : sign * 90.0f;
            }
            return moveAngleLocal;
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector2 AngleToPosition(float _angle)
        {
            var dir3D = Quaternion.AngleAxis(_angle, Vector3.up) * Vector3.forward;
            return new Vector2(dir3D.x, dir3D.z);
        }
    
        
        AnimGraph_Move8Dir m_settings;
        EntityManager m_EntityManager;
        Entity m_Owner;        
        Entity m_AnimStateOwner;
        AnimationLayerMixerPlayable m_locomotionMixer;
    
        AnimationClipPlayable m_clipAim;
        float m_aimTimeFactor;
    
        AnimationLayerMixerPlayable m_actionMixer;
        ActionAnimationHandler m_actionAnimationHandler;
        
        BlendTree2dSimpleDirectional m_BlendTree;

        float m_playSpeed;
        
        Vector2 m_CurrentVelocity;
        bool m_DoUpdateBlendPositions = true;
    }

}
