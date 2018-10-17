using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;

public class AimVerticalHandler  {

    public AimVerticalHandler(AnimationLayerMixerPlayable mixer, AnimationClip animAimDownToUp)
    {
        // Aim 
        m_animAim = AnimationClipPlayable.Create(mixer.GetGraph(), animAimDownToUp);
        m_animAim.SetApplyFootIK(false);
        m_animAim.Pause();
        m_aimTimeFactor = animAimDownToUp.length / 180.0f;

        m_port = mixer.AddInput(m_animAim, 0);
        mixer.SetLayerAdditive((uint)m_port, true);
        mixer.SetInputWeight(m_port, 1.0f);
    }

    public void SetAngle(float pitchAngle)
    {
        m_animAim.SetTime(pitchAngle * m_aimTimeFactor);
    }

    AnimationClipPlayable m_animAim;
    float m_aimTimeFactor;
    int m_port;
}
