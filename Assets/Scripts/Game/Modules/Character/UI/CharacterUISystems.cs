using Unity.Entities;
using UnityEngine;

[DisableAutoCreation]
public class UpdateCharacterUI : BaseComponentSystem
{
    ComponentGroup Group;   

    public UpdateCharacterUI(GameWorld world) : base(world)
    {
        m_prefab = Resources.Load<IngameHUD>("Prefabs/CharacterHUD");       
    }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(LocalPlayer), typeof(PlayerCameraSettings),
            typeof(LocalPlayerCharacterControl));
    }

    protected override void OnDestroyManager()
    {
        var charControlArray = Group.GetComponentArray<LocalPlayerCharacterControl>();
        for (int i = 0; i < charControlArray.Length; i++)
        {
            if (charControlArray[i].hud == null)
                continue;
            m_world.RequestDespawn(charControlArray[i].hud.gameObject, PostUpdateCommands);
        }
    }

    protected override void OnUpdate()
    {
        var time = m_world.worldTime;

        var localPlayerArray = Group.GetComponentArray<LocalPlayer>();
        var playerCamSettingsArray = Group.GetComponentArray<PlayerCameraSettings>();
        var charControlArray = Group.GetComponentArray<LocalPlayerCharacterControl>();
        
        GameDebug.Assert(localPlayerArray.Length <= 1, "There should never be more than 1 local player!");

        for (var i = 0; i < localPlayerArray.Length; i++)
        {
            
            var player = localPlayerArray[i];
            var characterControl = charControlArray[i];
            var cameraSettings = playerCamSettingsArray[i];

            if (characterControl.hud == null)
                characterControl.hud = m_world.Spawn<IngameHUD>(m_prefab.gameObject);

            // Handle controlled entity change
            if (characterControl.lastRegisteredControlledEntity != player.controlledEntity)
            {
                // Delete all current UI elements
                if(characterControl.healthUI != null)
                    GameObject.Destroy(characterControl.healthUI.gameObject);
                characterControl.healthUI = null;
                
                foreach(var charUI in characterControl.registeredCharUIs)
                    GameObject.Destroy(charUI.gameObject);
                characterControl.registeredCharUIs.Clear();
                    
                // 
                characterControl.lastRegisteredControlledEntity = Entity.Null;


                // Set new controlled entity
                if (EntityManager.HasComponent<Character>(player.controlledEntity))
                {
                    characterControl.lastRegisteredControlledEntity = player.controlledEntity;
                }
                    

                // Build new UI elements
                if (characterControl.lastRegisteredControlledEntity != Entity.Null &&
                    EntityManager.Exists(characterControl.lastRegisteredControlledEntity))
                {
                    var characterEntity = characterControl.lastRegisteredControlledEntity;
                    var character =
                        EntityManager.GetComponentObject<Character>(characterEntity);

                    var charPresentation = character.presentation;
                    
                    if (EntityManager.HasComponent<CharacterUISetup>(charPresentation))
                    {
                        // TODO (mogensh) we should move UI setup out to Hero setup (or something similar clientside)
                        var uiSetup = EntityManager.GetComponentObject<CharacterUISetup>(charPresentation);
                        if (uiSetup.healthUIPrefab != null)
                        {
                            characterControl.healthUI = GameObject.Instantiate(uiSetup.healthUIPrefab);
                            characterControl.healthUI.transform.SetParent(characterControl.hud.transform, false);
                        }
                    }

                    foreach (var cherPresentation in character.presentations)
                    {
                        if (cherPresentation.uiPrefabs == null || cherPresentation.uiPrefabs.Length == 0)
                            continue;

                        foreach (var uiPrefab in cherPresentation.uiPrefabs)
                        {
                            var abilityUI = GameObject.Instantiate(uiPrefab);
                            abilityUI.abilityOwner = characterEntity;
                        
                            abilityUI.transform.SetParent(characterControl.hud.transform, false);
                            characterControl.registeredCharUIs.Add(abilityUI);
                        }
                    }
                }
            }


            // Update current setup
            if (characterControl.lastRegisteredControlledEntity == Entity.Null)
                continue;

            // Check for damage inflicted and recieved
            var damageHistory = EntityManager.GetComponentData<DamageHistoryData>(characterControl.lastRegisteredControlledEntity);
            if (damageHistory.inflictedDamage.tick > characterControl.lastDamageInflictedTick)
            {
                characterControl.lastDamageInflictedTick = damageHistory.inflictedDamage.tick;
                characterControl.hud.ShowHitMarker(damageHistory.inflictedDamage.lethal == 1);
            }

            var charAnimState =
                EntityManager.GetComponentData<CharacterInterpolatedData>(characterControl
                    .lastRegisteredControlledEntity);
            if (charAnimState.damageTick > characterControl.lastDamageReceivedTick)
            {
                characterControl.hud.m_Crosshair.ShowHitDirectionIndicator(charAnimState.damageDirection);
                characterControl.lastDamageReceivedTick = charAnimState.damageTick;
            }

            // Update health
            if (characterControl.healthUI != null)
            {
                var healthState = EntityManager.GetComponentData<HealthStateData>(characterControl.lastRegisteredControlledEntity);
                characterControl.healthUI.UpdateUI(ref healthState);
            }
                

            characterControl.hud.FrameUpdate(player, cameraSettings);

            foreach (var charUI in characterControl.registeredCharUIs)
            {
                charUI.UpdateAbilityUI(EntityManager, ref time);
            }
        }
    }
    
    IngameHUD m_prefab;
}

