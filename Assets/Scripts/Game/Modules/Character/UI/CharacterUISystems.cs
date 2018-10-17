using Unity.Entities;
using UnityEngine;

[DisableAutoCreation]
public class UpdateCharacterUI : BaseComponentSystem
{
    public struct Players
    {
        public ComponentArray<LocalPlayer> players;
        public ComponentArray<PlayerCameraSettings> cameraSettings;
        public ComponentArray<LocalPlayerCharacterControl> characterControl;
    }

    [Inject] 
    public Players PlayerGroup;   

    public UpdateCharacterUI(GameWorld world) : base(world)
    {
        m_prefab = Resources.Load<IngameHUD>("Prefabs/CharacterHUD");       
    }
    
    protected override void OnDestroyManager()
    {
        for (int i = 0; i < PlayerGroup.characterControl.Length; i++)
        {
            if (PlayerGroup.characterControl[i].hud == null)
                continue;
            m_world.RequestDespawn(PlayerGroup.characterControl[i].hud.gameObject, PostUpdateCommands);
        }
    }

    protected override void OnUpdate()
    {
        var time = m_world.worldTime;

        GameDebug.Assert(PlayerGroup.players.Length <= 1, "There should never be more than 1 local player!");

        for (var i = 0; i < PlayerGroup.players.Length; i++)
        {
            
            var player = PlayerGroup.players[i];
            var characterControl = PlayerGroup.characterControl[i];
            var cameraSettings = PlayerGroup.cameraSettings[i];

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
                if (EntityManager.HasComponent<CharacterPredictedState>(player.controlledEntity))
                {
                    characterControl.lastRegisteredControlledEntity = player.controlledEntity;
                }
                    

                // Build new UI elements
                if (characterControl.lastRegisteredControlledEntity != Entity.Null &&
                    EntityManager.Exists(characterControl.lastRegisteredControlledEntity)) 
                {
                    if (EntityManager.HasComponent<CharacterUISetup>(characterControl.lastRegisteredControlledEntity))
                    {
                        var uiSetup = EntityManager.GetComponentObject<CharacterUISetup>(characterControl.lastRegisteredControlledEntity);
                        if (uiSetup.healthUIPrefab != null)
                        {
                            characterControl.healthUI = GameObject.Instantiate(uiSetup.healthUIPrefab);
                            characterControl.healthUI.transform.SetParent(characterControl.hud.transform, false);
                            characterControl.healthUI.health = uiSetup.gameObject.GetComponent<HealthState>();
                        }
                    }
                    
                    var abilityCtrl =
                        EntityManager.GetComponentObject<AbilityController>(characterControl.lastRegisteredControlledEntity);
                    for (var j = 0; j < abilityCtrl.abilityEntities.Length; j++)
                    {
                        var abilityEntity = abilityCtrl.abilityEntities[j];
                        if (abilityEntity == Entity.Null)
                            continue;
                        
                        var replicatedAbility = EntityManager.GetComponentObject<ReplicatedAbility>(abilityEntity);
                        if (replicatedAbility.uiPrefab == null)
                            continue;
                        
                        var abilityUI = GameObject.Instantiate(replicatedAbility.uiPrefab);
                        abilityUI.ability = abilityEntity;
                        
                        abilityUI.transform.SetParent(characterControl.hud.transform, false);
                        characterControl.registeredCharUIs.Add(abilityUI);
                    }
                }
            }


            // Update current setup
            if (characterControl.lastRegisteredControlledEntity == Entity.Null)
                continue;

            // Check for damage inflicted and recieved
            var damageHistory = EntityManager.GetComponentObject<DamageHistory>(characterControl.lastRegisteredControlledEntity);
            if (damageHistory.inflictedDamage.tick > characterControl.lastDamageInflictedTick)
            {
                characterControl.lastDamageInflictedTick = damageHistory.inflictedDamage.tick;
                characterControl.hud.ShowHitMarker(damageHistory.inflictedDamage.lethal);
            }

            var charAnimState =
                EntityManager.GetComponentData<CharAnimState>(characterControl
                    .lastRegisteredControlledEntity);
            if (charAnimState.damageTick > characterControl.lastDamageReceivedTick)
            {
                characterControl.hud.m_Crosshair.ShowHitDirectionIndicator(charAnimState.damageDirection);
                characterControl.lastDamageReceivedTick = charAnimState.damageTick;
            }

            // Update health
            if(characterControl.healthUI != null)
                characterControl.healthUI.UpdateUI();

            characterControl.hud.FrameUpdate(player, cameraSettings);

            foreach (var charUI in characterControl.registeredCharUIs)
            {
                charUI.UpdateAbilityUI(EntityManager, ref time);
            }
        }
    }
    
    IngameHUD m_prefab;
}

