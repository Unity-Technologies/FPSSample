using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class AutomaticRifleUI : AbilityUI
{
    public override void UpdateAbilityUI(EntityManager entityManager, ref GameTime time)
    {
        var character = entityManager.GetComponentObject<Character>(abilityOwner);
        var ability = character.FindAbilityWithComponent(entityManager,typeof(Ability_AutoRifle.PredictedState));
        GameDebug.Assert(ability != Entity.Null,"AbilityController does not own a Ability_AutoRifle ability");
        
        var state = entityManager.GetComponentData<Ability_AutoRifle.PredictedState>(ability);
        var settings = entityManager.GetComponentData<Ability_AutoRifle.Settings>(ability);
        
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
