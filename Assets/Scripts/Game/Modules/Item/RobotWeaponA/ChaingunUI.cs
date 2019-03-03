using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class ChaingunUI : AbilityUI
{
    public override void UpdateAbilityUI(EntityManager entityManager, ref GameTime time)
    {
        var charRepAll = entityManager.GetComponentData<CharacterReplicatedData>(abilityOwner);
        var ability = charRepAll.FindAbilityWithComponent(entityManager,typeof(Ability_Chaingun.PredictedState));
        GameDebug.Assert(ability != Entity.Null,"AbilityController does not own a Ability_Chaingun ability");

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
            m_ClipSizeText.text = "/ " + m_ClipSize.ToString();
        }
    }

    [SerializeField] TMPro.TextMeshProUGUI m_AmmoInClipText;
    [SerializeField] TMPro.TextMeshProUGUI m_ClipSizeText;

    int m_AmmoInClip = -1;
    int m_ClipSize = -1;
}