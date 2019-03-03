using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[Serializable]
public struct ActionAnimationDefinition
{
    public CharacterPredictedData.Action action;
    public AnimationClip animation;
    public float restartTimeOffset;
}

public class ActionAnimationHandler
{
    public struct ActionAnimation
    {
        public int port;
        public AnimationClipPlayable animation;
        public float restartTimeOffset;
    }

    public ActionAnimationHandler(AnimationLayerMixerPlayable mixer, ActionAnimationDefinition[] actionAnimationDefs)
    {
        if (actionAnimationDefs == null)
            return;

        m_mixer = mixer;
        foreach (var def in actionAnimationDefs)
        {
            if (def.animation == null)
                continue;

            if (m_actionAnimations.ContainsKey(def.action))
                continue;

            ActionAnimation actionAnim = new ActionAnimation();
            actionAnim.animation = AnimationClipPlayable.Create(mixer.GetGraph(), def.animation);
            actionAnim.animation.SetApplyFootIK(false);
            actionAnim.animation.SetDuration(def.animation.length);
            actionAnim.port = mixer.AddInput(actionAnim.animation, 0);
            actionAnim.restartTimeOffset = def.restartTimeOffset;
            mixer.SetLayerAdditive((uint)actionAnim.port, true);
            m_actionAnimations.Add(def.action, actionAnim);
        }
    }

    public void UpdateAction(CharacterPredictedData.Action newAction, float actionTime)
    {
        // Handle action change. This does not happen when action changes to None
        if (newAction != m_currentAction && newAction != CharacterPredictedData.Action.None && m_actionAnimations.ContainsKey(newAction))
        {
            // Stop current action
            if (m_currentAction != CharacterPredictedData.Action.None)
            {
                m_actionAnimations[m_currentAction].animation.Pause();
                m_mixer.SetInputWeight(m_actionAnimations[m_currentAction].port, 0);
            }

            m_currentAction = newAction;

            // Start new action animation
            m_actionAnimations[m_currentAction].animation.Play();
            m_mixer.SetInputWeight(m_actionAnimations[m_currentAction].port, 1);
        }

        if (m_currentAction == CharacterPredictedData.Action.None)
            return;


        // We syncronize time when presentation state is in same action. If presentation state has None action the animation will continue playing normally 
        if (newAction == m_currentAction)
        {
            if (actionTime < m_lastActionTime)
                m_actionRestarted = true;
            m_lastActionTime = actionTime;

            float time = m_actionRestarted ? actionTime + m_actionAnimations[m_currentAction].restartTimeOffset : actionTime;
            m_actionAnimations[m_currentAction].animation.SetTime(time);
        }
            


        // Stop animation when it is done
        bool animDone = m_actionAnimations[m_currentAction].animation.GetTime() >= m_actionAnimations[m_currentAction].animation.GetDuration();
        if (animDone)
        {
            m_actionAnimations[m_currentAction].animation.Pause();
            m_mixer.SetInputWeight(m_actionAnimations[m_currentAction].port, 0);
            m_currentAction = CharacterPredictedData.Action.None;
            m_actionRestarted = false;
            m_lastActionTime = 0;
        }
    }

    public ActionAnimation GetActionAnimation(CharacterPredictedData.Action action)
    {
        ActionAnimation actionAnimation;
        m_actionAnimations.TryGetValue(action, out actionAnimation);
        return actionAnimation;
    }

    AnimationLayerMixerPlayable m_mixer;
    Dictionary<CharacterPredictedData.Action, ActionAnimation> m_actionAnimations = new Dictionary<CharacterPredictedData.Action, ActionAnimation>();
    CharacterPredictedData.Action m_currentAction;
    float m_lastActionTime;
    bool m_actionRestarted;
}
