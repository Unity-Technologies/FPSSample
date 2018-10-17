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
        m_supportedActionTriggers = new int[(int)CharacterPredictedState.StateData.Action.NumActions];
        for (int i = 0; i < m_animatorController.GetParameterCount(); i++)
        {
            AnimatorControllerParameter param = m_animatorController.GetParameter(i);

            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    {
                        CharacterPredictedState.StateData.LocoState animState;
                        if (s_charLocoStateHashes.TryGetValue(param.nameHash, out animState))
                        {
                            var p = new AnimStateParams();
                            p.state = animState;
                            p.hash = param.nameHash;
                            animStateParams.Add(p);
                            continue;
                        }

                        CharacterPredictedState.StateData.Action action;
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
                        CharacterPredictedState.StateData.Action action;
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

    public void Update(ref CharAnimState animState)
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
            if (m_lastActionTriggered != CharacterPredictedState.StateData.Action.None)
            {
                m_animatorController.ResetTrigger(m_supportedActionTriggers[(int)m_lastActionTriggered]);
            }
                
            m_lastActionTriggered = CharacterPredictedState.StateData.Action.None;

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
        public CharacterPredictedState.StateData.LocoState state;
        public int hash;
    }

    struct ActionStateParams
    {
        public CharacterPredictedState.StateData.Action action;
        public int hash;
    }

    readonly static int s_resetHash = Animator.StringToHash("Trigger_Reset");

    readonly static Dictionary<int, CharacterPredictedState.StateData.LocoState> s_charLocoStateHashes = new Dictionary<int, CharacterPredictedState.StateData.LocoState>() {
        { Animator.StringToHash("AnimState_Stand"), CharacterPredictedState.StateData.LocoState.Stand },
        { Animator.StringToHash("AnimState_Run"), CharacterPredictedState.StateData.LocoState.GroundMove },
        { Animator.StringToHash("AnimState_Jump"), CharacterPredictedState.StateData.LocoState.Jump },
        { Animator.StringToHash("AnimState_DoubleJump"), CharacterPredictedState.StateData.LocoState.DoubleJump },
        { Animator.StringToHash("AnimState_InAir"), CharacterPredictedState.StateData.LocoState.InAir },
    };

    readonly static Dictionary<int, CharacterPredictedState.StateData.Action> s_actionStateHashes = new Dictionary<int, CharacterPredictedState.StateData.Action>() {
        { Animator.StringToHash("Action_None"), CharacterPredictedState.StateData.Action.None },
        { Animator.StringToHash("Action_PrimaryFire"), CharacterPredictedState.StateData.Action.PrimaryFire },
        { Animator.StringToHash("Action_SecondaryFire"), CharacterPredictedState.StateData.Action.SecondaryFire },
        { Animator.StringToHash("Action_Reloading"), CharacterPredictedState.StateData.Action.Reloading },
        { Animator.StringToHash("Action_Melee"), CharacterPredictedState.StateData.Action.Melee },
    };

    readonly static Dictionary<int, CharacterPredictedState.StateData.Action> s_actionTriggerHashes = new Dictionary<int, CharacterPredictedState.StateData.Action>() {
        { Animator.StringToHash("TriggerAction_None"), CharacterPredictedState.StateData.Action.None },
        { Animator.StringToHash("TriggerAction_PrimaryFire"), CharacterPredictedState.StateData.Action.PrimaryFire },
        { Animator.StringToHash("TriggerAction_SecondaryFire"), CharacterPredictedState.StateData.Action.SecondaryFire },
        { Animator.StringToHash("TriggerAction_Reloading"), CharacterPredictedState.StateData.Action.Reloading },
        { Animator.StringToHash("TriggerAction_Melee"), CharacterPredictedState.StateData.Action.Melee },
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
    

    CharacterPredictedState.StateData.Action m_lastActionTriggered = CharacterPredictedState.StateData.Action.None;
    int lastActionTick;
}

