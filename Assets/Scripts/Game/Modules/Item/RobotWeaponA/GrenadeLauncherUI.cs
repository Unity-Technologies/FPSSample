using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class GrenadeLauncherUI : AbilityUI 
{
    public RawImage activeIcon;
    public TMPro.TextMeshProUGUI cooldownText;
    public GameObject disabledOverlay;

    public override void UpdateAbilityUI(EntityManager entityManager, ref GameTime time)
    {
        var charRepAll = entityManager.GetComponentData<CharacterReplicatedData>(abilityOwner);
        var ability = charRepAll.FindAbilityWithComponent(entityManager,typeof(Ability_GrenadeLauncher.PredictedState));
        GameDebug.Assert(ability != Entity.Null,"AbilityController does not own a Ability_GrenadeLauncher ability");

        var state = entityManager.GetComponentData<Ability_GrenadeLauncher.PredictedState>(ability);
        var settings = entityManager.GetComponentData<Ability_GrenadeLauncher.Settings>(ability);

        activeIcon.enabled = state.phase == Ability_GrenadeLauncher.Phase.Active;

        bool showCooldown = state.phase == Ability_GrenadeLauncher.Phase.Cooldown;
        cooldownText.gameObject.SetActive(showCooldown);
        disabledOverlay.SetActive(showCooldown);
        if (showCooldown)
        {
            float cooldownLeft = settings.cooldownDuration - time.DurationSinceTick(state.phaseStartTick);
            cooldownText.text = string.Format("{0:F1}", cooldownLeft);
        }
    }
}
