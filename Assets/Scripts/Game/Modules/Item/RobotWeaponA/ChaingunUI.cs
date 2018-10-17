using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class ChaingunUI : AbilityUI
{
    public override void UpdateAbilityUI(EntityManager entityManager, ref GameTime time)
    {
        var state = entityManager.GetComponentData<Ability_Chaingun.PredictedState>(ability);
        var settings = entityManager.GetComponentData<Ability_Chaingun.Settings>(ability);

        if (m_AmmoInClip != state.ammoInClip)
        {
            m_AmmoInClip = state.ammoInClip;
            m_AmmoInClipText.text = m_AmmoInClip.ToString();
        }

        if (m_ClipSize != settings.clipSize)
        {
            m_ClipSize = settings.clipSize;
            m_ClipSizeText.text = "/" + m_ClipSize.ToString();
        }
    }

    [SerializeField] Text m_AmmoInClipText;
    [SerializeField] Text m_ClipSizeText;

    int m_AmmoInClip = -1;
    int m_ClipSize = -1;
}