using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "Sprint", menuName = "FPS Sample/Animation/AnimGraph/Sprint")]
public class AnimGraph_Sprint : AnimGraphAsset
{
    public AnimationClip animMoveNW;
    public AnimationClip animMoveN;
    public AnimationClip animMoveNE;
    public float animMovePlaySpeed = 1.0f;
    public AnimationClip animAimDownToUp;
    
    [Range(0f, 1f)]
    [Tooltip("The max. time between exiting ground move and re-entering before a state reset is triggered")]
    public float stateResetWindow;  
    
    
    public override IAnimGraphInstance Instatiate(EntityManager entityManager, Entity owner, PlayableGraph graph,
        Entity animStateOwner)
    {
        var animState = new Instance(entityManager, owner, graph, animStateOwner, this);
        return animState;
    }
        
    class Instance : IAnimGraphInstance, IGraphState
    {
        public Instance(EntityManager entityManager, Entity owner, PlayableGraph graph, Entity animStateOwner, AnimGraph_Sprint settings)
        {
            m_settings = settings;
            m_EntityManager = entityManager;
            m_Owner = owner;
            m_AnimStateOwner = animStateOwner;
            
            // Movement
            m_movementMixer = AnimationMixerPlayable.Create(graph, 3);
    
            m_movementClips = new AnimationClipPlayable[3];
            m_movementClips[0] = AnimationClipPlayable.Create(graph, settings.animMoveN);
            m_movementClips[1] = AnimationClipPlayable.Create(graph, settings.animMoveNW);
            m_movementClips[2] = AnimationClipPlayable.Create(graph, settings.animMoveNE);
    
            foreach (var moveClip in m_movementClips)
            {
                moveClip.SetApplyFootIK(true);
                moveClip.SetSpeed(settings.animMovePlaySpeed);
            }
    
            graph.Connect(m_movementClips[0], 0, m_movementMixer, (int)Direction.Forward);
            graph.Connect(m_movementClips[1], 0, m_movementMixer, (int)Direction.Left);
            graph.Connect(m_movementClips[2], 0, m_movementMixer, (int)Direction.Right);
            
            outputPlayable = m_movementMixer;
            
            // Aim
            if (settings.animAimDownToUp != null)
            {
                m_aimMixer = AnimationLayerMixerPlayable.Create(graph, 1);
                graph.Connect(m_movementMixer, 0, m_aimMixer, 0);
                m_aimMixer.SetInputWeight(0, 1f);
                m_aimMixer.SetLayerAdditive(0, false);
                m_aimHandler = new AimVerticalHandler(m_aimMixer, settings.animAimDownToUp);      
                
                outputPlayable = m_aimMixer;
            }
        }
    
        public void Shutdown() { }
    
        public void SetPlayableInput(int index, Playable playable, int playablePort)
        {
        }
    
        public void GetPlayableOutput(int index, ref Playable playable, ref int playablePort)
        {
            playable = outputPlayable;
            playablePort = 0;
        }
    
        public void UpdatePresentationState(bool firstUpdate, GameTime time, float deltaTime)
        {            
            Profiler.BeginSample("Sprint.Update");
            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            if (firstUpdate)
            {
                // Do phase projection for time not spent in state
                var ticksSincePreviousGroundMove = time.tick - animState.lastGroundMoveTick;                
                if (ticksSincePreviousGroundMove > 1)
                {
                    animState.locomotionPhase += m_playSpeed * (ticksSincePreviousGroundMove - 1f);
                } 
                
                // Reset the phase if appropriate
                var timeSincePreviousGroundMove = ticksSincePreviousGroundMove / (float)time.tickRate;                
                if (animState.previousCharLocoState != CharacterPredictedData.LocoState.GroundMove && 
                    timeSincePreviousGroundMove >  m_settings.stateResetWindow)
                {
//                    Debug.Log("Reset movement sprint! (Ticks since: " + ticksSincePreviousGroundMove + " Time since: " + timeSincePreviousGroundMove + ")");
                    animState.locomotionPhase = 0f;
                }
            }
            
            animState.rotation = animState.aimYaw;
            var moveAngleLocal = Mathf.DeltaAngle(animState.rotation, animState.moveYaw);
    
            // Damp local move angle 
            var speed = 800.0f;             // TODO this magic number should be tweakable
            var deltaAngle = deltaTime * speed;
            var diff = Mathf.Abs(Mathf.DeltaAngle(animState.moveAngleLocal, moveAngleLocal));
            var t = deltaAngle >= diff ? 1.0f : deltaAngle / diff;
            var dampedAngle = Mathf.LerpAngle(animState.moveAngleLocal + 180, moveAngleLocal + 180, t);
            while (dampedAngle > 360) dampedAngle -= 360;
            while (dampedAngle < 0) dampedAngle += 360;
            animState.moveAngleLocal = dampedAngle - 180;

            m_playSpeed = m_settings.animMovePlaySpeed / m_movementClips[0].GetAnimationClip().length * deltaTime;
            animState.locomotionPhase += m_playSpeed;
            
            m_EntityManager.SetComponentData(m_AnimStateOwner, animState);
            Profiler.EndSample();
        }
    
        public void ApplyPresentationState(GameTime time, float deltaTime)
        {
            Profiler.BeginSample("Sprint.Apply");
            
            var animState = m_EntityManager.GetComponentData<CharacterInterpolatedData>(m_AnimStateOwner);
            // Set the phase of the animation
            var clipLength = m_movementClips[0].GetAnimationClip().length;
            for (int i = 0; i < m_movementClips.Length; i++)
            {
                m_movementClips[i].SetTime(animState.locomotionPhase * clipLength);
            }
    
            var turnWeight = Mathf.Clamp(Mathf.Abs(animState.moveAngleLocal) / 35.0f, 0, 1.0f);
            var turningRight = animState.moveAngleLocal > 0;
    
            m_movementMixer.SetInputWeight((int)Direction.Forward, 1.0f - turnWeight);
            m_movementMixer.SetInputWeight((int)Direction.Left, turningRight ? 0.0f : turnWeight);
            m_movementMixer.SetInputWeight((int)Direction.Right, turningRight ? turnWeight : 0.0f);
            
            if(m_aimHandler != null)
                m_aimHandler.SetAngle(animState.aimPitch);
            
            Profiler.EndSample();
        }
    
        AnimGraph_Sprint m_settings;
        EntityManager m_EntityManager;
        Entity m_Owner;
        Entity m_AnimStateOwner;

        enum Direction
        {
            Forward,
            Left,
            Right,
        }
    
        AnimationClipPlayable[] m_movementClips;
        AnimationMixerPlayable m_movementMixer;
        AnimationLayerMixerPlayable m_aimMixer;
        AimVerticalHandler m_aimHandler;
        Playable outputPlayable;
        float m_playSpeed;
    }

}
