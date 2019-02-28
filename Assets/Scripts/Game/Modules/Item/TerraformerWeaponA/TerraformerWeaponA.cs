using System;
using UnityEngine.Playables;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Experimental.VFX;

[DisallowMultipleComponent]
[ClientOnlyComponent]
public class TerraformerWeaponA : MonoBehaviour
{
    public Transform muzzle;
    
    public PlayableDirector timelineGrenadeRefill;

    public Transform grenadeFuelBase;
    public Transform magazineFuelBase;

    public Transform[] vents;
    public float ventDampSpeed = 5;
    public float ventStartSpeed = 1000;

    public SoundDef primaryFireSound;
    public VisualEffect primaryMuzzleFlash;
    public HitscanEffectTypeDefinition hitscanEffect;
    public SpatialEffectTypeDefinition environmentImpactEffect;
    public SpatialEffectTypeDefinition characterImpactEffect;

    public SoundDef secondaryFireSound;
    public VisualEffect secondaryMuzzleFlash;
    
    public SoundDef meleeImpactSound;
    public VisualEffect meleeImpactEffect;
    
    [NonSerialized] public TickEventHandler primaryFireEvent = new TickEventHandler(0.5f);
    [NonSerialized] public TickEventHandler secondaryFireEvent = new TickEventHandler(0.5f);
    [NonSerialized] public TickEventHandler meleeImpactEvent = new TickEventHandler(0.5f);
    [NonSerialized] public SoundSystem.SoundHandle primaryFireSoundHandle;

    public float[] ventRotationSpeed;
    public int nextVentIndex;

    public Vector3 m_lastGrenadeFuelWorldPos;
    public CharacterPredictedData.Action m_prevAction;

    void Awake()
    {
        if (vents != null)
            ventRotationSpeed = new float[vents.Length];
    }
}

// System
[DisableAutoCreation]
public class UpdateTerraformerWeaponA : BaseComponentSystem<CharacterPresentationSetup,TerraformerWeaponA>
{
    public UpdateTerraformerWeaponA(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new[] {ComponentType.Subtractive<DespawningEntity>(),};
    }
    
    protected override void Update(Entity entity, CharacterPresentationSetup charPresentation, TerraformerWeaponA weapon)
    {
        if (!charPresentation.IsVisible)
            return;

        var charRepAll = EntityManager.GetComponentData<CharacterReplicatedData>(charPresentation.character);
        Update(m_world.worldTime, weapon, ref charRepAll);
    }


    void Update(GameTime time, TerraformerWeaponA weapon, ref CharacterReplicatedData charRepAll)
    {
        // Update using AutoRifle ability state
        var autoRifleAbility = charRepAll.FindAbilityWithComponent(EntityManager,typeof(Ability_AutoRifle.InterpolatedState));
        GameDebug.Assert(autoRifleAbility != Entity.Null,"AbilityController does not own a Ability_AutoRifle ability");
        var autoRifleInterpolatedState = EntityManager.GetComponentData<Ability_AutoRifle.InterpolatedState>(autoRifleAbility);
        if (weapon.primaryFireEvent.Update(time, autoRifleInterpolatedState.fireTick))
        {
            if(weapon.primaryFireSound != null)
            {
                if (weapon.primaryFireSoundHandle.IsValid() && weapon.primaryFireSoundHandle.emitter.playing)
                    Game.SoundSystem.Stop(weapon.primaryFireSoundHandle, 0.05f);
                weapon.primaryFireSoundHandle = Game.SoundSystem.Play(weapon.primaryFireSound, weapon.muzzle);
            }
                
            if(weapon.primaryMuzzleFlash != null)
                weapon.primaryMuzzleFlash.Play();

            if (weapon.hitscanEffect != null)
            {
                World.GetExistingManager<HandleHitscanEffectRequests>().Request(weapon.hitscanEffect, 
                    weapon.muzzle.position, autoRifleInterpolatedState.fireEndPos);
            }

            if (autoRifleInterpolatedState.impactType != Ability_AutoRifle.ImpactType.None)
            {
                var rotation = Quaternion.LookRotation(autoRifleInterpolatedState.impactNormal);
                if (autoRifleInterpolatedState.impactType == Ability_AutoRifle.ImpactType.Character)
                {
                    World.GetExistingManager<HandleSpatialEffectRequests>().Request(weapon.characterImpactEffect, 
                        autoRifleInterpolatedState.fireEndPos, rotation);
                }
                else
                {
                    World.GetExistingManager<HandleSpatialEffectRequests>().Request(weapon.environmentImpactEffect, 
                        autoRifleInterpolatedState.fireEndPos, rotation);
                }
            }
        }
   
        // Update using ProjectileLauncher ability state
        var rocketAbility = charRepAll.FindAbilityWithComponent(EntityManager,typeof(Ability_ProjectileLauncher.InterpolatedState));
        GameDebug.Assert(rocketAbility != Entity.Null,"AbilityController does not own a Ability_ProjectileLauncher ability");
        var rocketLaunchInterpolatedState = EntityManager.GetComponentData<Ability_ProjectileLauncher.InterpolatedState>(rocketAbility);
        if (weapon.secondaryFireEvent.Update(time, rocketLaunchInterpolatedState.fireTick))
        {
            if(weapon.secondaryFireSound != null)
                Game.SoundSystem.Play(weapon.secondaryFireSound, weapon.muzzle);
                
            if(weapon.secondaryMuzzleFlash != null)
                weapon.secondaryMuzzleFlash.Play();
        }

        // Update using Melee ability ability state
        var meleeAbility = charRepAll.FindAbilityWithComponent(EntityManager,typeof(Ability_Melee.InterpolatedState));
        GameDebug.Assert(meleeAbility != Entity.Null,"AbilityController does not own a Ability_Melee ability");
        var meleeInterpolatedState = EntityManager.GetComponentData<Ability_Melee.InterpolatedState>(meleeAbility);
        if (weapon.meleeImpactEvent.Update(time, meleeInterpolatedState.impactTick))
        {
            if(weapon.meleeImpactSound != null)
                Game.SoundSystem.Play(weapon.meleeImpactSound, weapon.transform.position);
                
            if(weapon.meleeImpactEffect != null)
                weapon.meleeImpactEffect.Play();
        }
        
        

        // Vents disabled vents until we find out what to do with them
        // Update vents
//            if (entity.vents != null)
//            {
//                for (int ventIndex = 0; ventIndex < entity.vents.Length; ventIndex++)
//                {
//                    if (entity.ventRotationSpeed[ventIndex] == 0)
//                        continue;
//
//                    // Rotate
//                    float deltaRot = entity.ventRotationSpeed[ventIndex] * m_world.frameDuration;
//                    Vector3 eulerRot = entity.vents[ventIndex].rotation.eulerAngles;
//                    eulerRot.z += deltaRot;
//                    entity.vents[ventIndex].rotation = Quaternion.Euler(eulerRot);
//
//                    // Damp speed
//                    float deltaSpeed = entity.ventDampSpeed * m_world.frameDuration;
//                    float absSpeed = Mathf.Abs(entity.ventRotationSpeed[ventIndex]);
//                    if (deltaSpeed >= absSpeed)
//                        entity.ventRotationSpeed[ventIndex] = 0;
//                    else
//                        entity.ventRotationSpeed[ventIndex] -= Mathf.Sign(entity.ventRotationSpeed[ventIndex]) * deltaSpeed;
//                }
//
//                Character.State.Action newAction = weapon.action;
//                if (newAction != entity.m_prevAction)
//                {
//                    if (newAction == Character.State.Action.PrimaryFire)
//                    {
//                        entity.ventRotationSpeed[entity.nextVentIndex] = entity.ventStartSpeed;
//                        entity.ventRotationSpeed[entity.nextVentIndex + 1] = -entity.ventStartSpeed;
//                        entity.nextVentIndex = (entity.nextVentIndex + 2) % entity.vents.Length;
//                    }
//                    if (newAction == Character.State.Action.SecondaryFire)
//                    {
//                        entity.timelineGrenadeRefill.time = 0;
//                        entity.timelineGrenadeRefill.Play();
//                    }
//
//                    entity.m_prevAction = newAction;
//                }
//            }


        // Velocity based ammo fuel angle 
        if(weapon.grenadeFuelBase != null)
        {
            Vector3 basePos = weapon.grenadeFuelBase.position;
            Vector3 moveVec = basePos - weapon.m_lastGrenadeFuelWorldPos;
            weapon.m_lastGrenadeFuelWorldPos = basePos;

            Vector3 rotateAxis = Vector3.Cross(moveVec, Vector3.up).normalized;
            float moveVel = moveVec.magnitude / m_world.frameDuration;
            float angle = moveVel * 2;
            Quaternion targetRot = Quaternion.AngleAxis(-angle, rotateAxis);

            weapon.grenadeFuelBase.rotation = Quaternion.Lerp(weapon.grenadeFuelBase.rotation, targetRot, 3 * m_world.frameDuration);

        }
    }
}


[DisableAutoCreation]
public class TerraformerWeaponClientProjectileSpawnHandler : InitializeComponentGroupSystem<ClientProjectile, TerraformerWeaponClientProjectileSpawnHandler.Initialized>
{
    public struct Initialized : IComponentData {}
    
    private ComponentGroup WeaponGroup;
    
    public TerraformerWeaponClientProjectileSpawnHandler(GameWorld world) : base(world)
    {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        WeaponGroup = GetComponentGroup(typeof(TerraformerWeaponA), typeof(CharacterPresentationSetup));
    }

    protected override void Initialize(ref ComponentGroup group)
    {
        var clientProjectileArray = group.GetComponentArray<ClientProjectile>();
        var weaponArray = WeaponGroup.GetComponentArray<TerraformerWeaponA>();
        var charPresentationArray =  WeaponGroup.GetComponentArray<CharacterPresentationSetup>();
        
        for (var i = 0; i < clientProjectileArray.Length; i++)
        {
         var clientProjectile = clientProjectileArray[i];
        
         var projectileData = EntityManager.GetComponentData<ProjectileData>(clientProjectile.projectile);
         var projectileOwner = projectileData.projectileOwner;
        
         for (var j = 0; j < charPresentationArray.Length; j++)
         {
             var charPresentation = charPresentationArray[j];
        
             if (!charPresentation.IsVisible)
                 continue;
             
             if (charPresentation.character == projectileOwner)
             {
                 var weapon = weaponArray[j];
                 var pos =  weapon.muzzle.position;
                 //GameDebug.Log("proj count;" + clientProjectileArray.Length + "muzzlepos:" + pos);
                 clientProjectile.SetMuzzlePosition(EntityManager, pos);
                 break;
             }
         }
        }
    }
}
