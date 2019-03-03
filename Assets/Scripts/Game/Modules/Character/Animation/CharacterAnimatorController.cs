using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class CharacterAnimatorController {

    public CharacterAnimatorController(PlayableGraph graph, RuntimeAnimatorController animatorController)
    {
        m_animatorController = AnimatorControllerPlayable.Create(graph, animatorController);

        // Find valid parameters in animatorcontroller
        var animStateParams = new List<AnimStateParams>();
        var actionStateParams = new List<ActionStateParams>();
        m_supportedActionTriggers = new int[(int)CharacterPredictedData.Action.NumActions];
        for (int i = 0; i < m_animatorController.GetParameterCount(); i++)
        {
            AnimatorControllerParameter param = m_animatorController.GetParameter(i);

            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    {
                        CharacterPredictedData.LocoState animState;
                        if (s_charLocoStateHashes.TryGetValue(param.nameHash, out animState))
                        {
                            var p = new AnimStateParams();
                            p.state = animState;
                            p.hash = param.nameHash;
                            animStateParams.Add(p);
                            continue;
                        }

                        CharacterPredictedData.Action action;
                        if (s_actionStateHashes.TryGetValue(param.nameHash, out action))
                        {
                            var p = new ActionStateParams();
                            p.action = action;
                            p.hash = param.nameHash;
                            actionStateParams.Add(p);
                            continue;
                        }
                        break;
                    }
                case AnimatorControllerParameterType.Trigger:
                    {
                        CharacterPredictedData.Action action;
                        if (s_actionTriggerHashes.TryGetValue(param.nameHash, out action))
                        {
                            m_supportedActionTriggers[(int)action] = param.nameHash;
                            continue;
                        }

                        if (param.nameHash == s_resetHash)
                        {
                            m_resetSupported = true;
                            continue;
                        }
                    }
                    break;
            }

        }
        m_supportedAnimStates = animStateParams.ToArray();
        m_supportedActionStates = actionStateParams.ToArray();
    }

    public Playable GetRootPlayable()
    {
        return m_animatorController;
    }

    public void Update(ref CharacterInterpolatedData animState)
    {
        // Set supported animation state bools
        for (var i = 0; i < m_supportedAnimStates.Length; i++)
            m_animatorController.SetBool(m_supportedAnimStates[i].hash, animState.charLocoState == m_supportedAnimStates[i].state);

        // Set supported action state bools
        for (var i = 0; i < m_supportedActionStates.Length; i++)
            m_animatorController.SetBool(m_supportedActionStates[i].hash, animState.charAction == m_supportedActionStates[i].action);

        // Set supported triggers
        if (animState.charAction != m_lastActionTriggered || animState.charActionTick != lastActionTick)
        {
            // Clear last trigger
            if (m_lastActionTriggered != CharacterPredictedData.Action.None)
            {
                m_animatorController.ResetTrigger(m_supportedActionTriggers[(int)m_lastActionTriggered]);
            }
                
            m_lastActionTriggered = CharacterPredictedData.Action.None;

            // Trigger new action trigger if it is supported
            if (m_supportedActionTriggers[(int)animState.charAction] != 0)
            {
                m_lastActionTriggered = animState.charAction;
                m_animatorController.SetTrigger(m_supportedActionTriggers[(int)m_lastActionTriggered]);
            }
        }
        lastActionTick = animState.charActionTick;

        m_animatorController.SetBool(m_sprintState, animState.sprinting == 1);
        
        if (animState.damageTick > m_lastReactionTick  + m_waitFor)
        {
            m_animatorController.SetTrigger(m_hitReaction);
            m_lastReactionTick = animState.damageTick;
            
            Random.InitState(animState.damageTick);
            m_waitFor = Random.Range(4, 6);
        }
    }


    public void Start()
    {
        if (m_resetSupported)
            m_animatorController.SetTrigger(s_resetHash);
        m_animatorController.Play();
    }

    public void Stop()
    {
        m_animatorController.Pause();
    }


    struct AnimStateParams
    {
        public CharacterPredictedData.LocoState state;
        public int hash;
    }

    struct ActionStateParams
    {
        public CharacterPredictedData.Action action;
        public int hash;
    }

    readonly static int s_resetHash = Animator.StringToHash("Trigger_Reset");

    readonly static Dictionary<int, CharacterPredictedData.LocoState> s_charLocoStateHashes = new Dictionary<int, CharacterPredictedData.LocoState>() {
        { Animator.StringToHash("AnimState_Stand"), CharacterPredictedData.LocoState.Stand },
        { Animator.StringToHash("AnimState_Run"), CharacterPredictedData.LocoState.GroundMove },
        { Animator.StringToHash("AnimState_Jump"), CharacterPredictedData.LocoState.Jump },
        { Animator.StringToHash("AnimState_DoubleJump"), CharacterPredictedData.LocoState.DoubleJump },
        { Animator.StringToHash("AnimState_InAir"), CharacterPredictedData.LocoState.InAir },
    };

    readonly static Dictionary<int, CharacterPredictedData.Action> s_actionStateHashes = new Dictionary<int, CharacterPredictedData.Action>() {
        { Animator.StringToHash("Action_None"), CharacterPredictedData.Action.None },
        { Animator.StringToHash("Action_PrimaryFire"), CharacterPredictedData.Action.PrimaryFire },
        { Animator.StringToHash("Action_SecondaryFire"), CharacterPredictedData.Action.SecondaryFire },
        { Animator.StringToHash("Action_Reloading"), CharacterPredictedData.Action.Reloading },
        { Animator.StringToHash("Action_Melee"), CharacterPredictedData.Action.Melee },
    };

    readonly static Dictionary<int, CharacterPredictedData.Action> s_actionTriggerHashes = new Dictionary<int, CharacterPredictedData.Action>() {
        { Animator.StringToHash("TriggerAction_None"), CharacterPredictedData.Action.None },
        { Animator.StringToHash("TriggerAction_PrimaryFire"), CharacterPredictedData.Action.PrimaryFire },
        { Animator.StringToHash("TriggerAction_SecondaryFire"), CharacterPredictedData.Action.SecondaryFire },
        { Animator.StringToHash("TriggerAction_Reloading"), CharacterPredictedData.Action.Reloading },
        { Animator.StringToHash("TriggerAction_Melee"), CharacterPredictedData.Action.Melee },
    };

    readonly int m_sprintState = Animator.StringToHash("State_Sprint");
    readonly int m_hitReaction = Animator.StringToHash("Hit_Reaction");
    
    int m_lastReactionTick;
    int m_waitFor;
    
    
    AnimatorControllerPlayable m_animatorController;
    AnimStateParams[] m_supportedAnimStates;
    ActionStateParams[] m_supportedActionStates;
    int[] m_supportedActionTriggers;
    bool m_resetSupported;
    

    CharacterPredictedData.Action m_lastActionTriggered = CharacterPredictedData.Action.None;
    int lastActionTick;
}

