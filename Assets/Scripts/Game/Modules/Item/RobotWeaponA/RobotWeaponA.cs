using System;
using System.Net;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Experimental.VFX;

public struct GunBarrelState
{
    public Vector3 startPosition;
    public float animTime;
}

[Serializable]
public class GunBarrelSetup
{
    public Transform[] barrels;
    public VisualEffect[] muzzleFlash;
    public SoundDef fireSound;
    public AnimationCurve fireAnimCurve;

    [NonSerialized] public SoundSystem.SoundHandle fireSoundHandle;
    [NonSerialized] public GunBarrelState[] states;
    [NonSerialized] public int index;
}


[ClientOnlyComponent]
public class RobotWeaponA : MonoBehaviour    
{
    [NonSerialized] public TickEventHandler primFireEvent = new TickEventHandler(0.5f);
    [NonSerialized] public TickEventHandler secFireEvent = new TickEventHandler(0.5f);
    [NonSerialized] public TickEventHandler meleeImpactEvent = new TickEventHandler(0.5f);
    
    public GunBarrelSetup barrelSetup;
    
    public SoundDef secondaryFireSound;
    public VisualEffect secondaryMuzzleFlash;
    
    public SoundDef meleeImpactSound;
    public VisualEffect meleeImpactEffect;
}


// System
[DisableAutoCreation]
public class System_RobotWeaponA : BaseComponentSystem<RobotWeaponA,CharacterPresentationSetup>
{
    public System_RobotWeaponA(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new[] {ComponentType.Subtractive<DespawningEntity>()};
    }
    
    protected override void Update(Entity entity, RobotWeaponA weapon, CharacterPresentationSetup charPresentation)
    {
        if (!charPresentation.IsVisible)
            return;

        var charRepAll = EntityManager.GetComponentData<CharacterReplicatedData>(charPresentation.character);
        Update(m_world.worldTime, weapon, ref charRepAll, m_world.frameDuration);
    }


    public void Update(GameTime time, RobotWeaponA weapon,ref CharacterReplicatedData charRepAll, float deltaTime)
    {
        GameDebug.Assert(weapon.barrelSetup != null && weapon.barrelSetup.barrels.Length > 0, "Robotweapon has no barrels defined");

        // Initialize barrels
        if (weapon.barrelSetup.states == null)
        {
            weapon.barrelSetup.states = new GunBarrelState[weapon.barrelSetup.barrels.Length];
            for (var i = 0; i < weapon.barrelSetup.states.Length; i++)
            {
                weapon.barrelSetup.states[i].startPosition = weapon.barrelSetup.barrels[i].transform.localPosition;
                weapon.barrelSetup.states[i].animTime = 1;
            }
        }

        // Update using chaingun ability state
        var chaingunAbility = charRepAll.FindAbilityWithComponent(EntityManager,typeof(Ability_Chaingun.InterpolatedState));
        GameDebug.Assert(chaingunAbility != Entity.Null,"AbilityController does not own a Ability_Chaingun ability");
        var chaingunInterpolatedState = EntityManager.GetComponentData<Ability_Chaingun.InterpolatedState>(chaingunAbility);
        
        if (weapon.primFireEvent.Update(time, chaingunInterpolatedState.fireTick))
        {
            weapon.barrelSetup.index = (weapon.barrelSetup.index + 1) % weapon.barrelSetup.barrels.Length;

            var index = weapon.barrelSetup.index;
            weapon.barrelSetup.states[index].animTime = 0;
            weapon.barrelSetup.muzzleFlash[index].Play();
            if (weapon.barrelSetup.fireSoundHandle.IsValid() && weapon.barrelSetup.fireSoundHandle.emitter.playing)
                Game.SoundSystem.Stop(weapon.barrelSetup.fireSoundHandle, 0.1f);
            weapon.barrelSetup.fireSoundHandle = Game.SoundSystem.Play(weapon.barrelSetup.fireSound, 
                weapon.barrelSetup.muzzleFlash[index].transform.position);
        }
        
        // Update using grenade ability state
        var grenadeAbility = charRepAll.FindAbilityWithComponent(EntityManager,typeof(Ability_GrenadeLauncher.InterpolatedState));
        GameDebug.Assert(grenadeAbility != Entity.Null,"AbilityController does not own a Ability_GrenadeLauncher ability");
        var grenadeInterpolatedState = EntityManager.GetComponentData<Ability_GrenadeLauncher.InterpolatedState>(grenadeAbility);
        if (weapon.secFireEvent.Update(time, grenadeInterpolatedState.fireTick))
        {
            if (weapon.secondaryFireSound != null)
                Game.SoundSystem.Play(weapon.secondaryFireSound, weapon.transform.position);

            if (weapon.secondaryMuzzleFlash != null)
                weapon.secondaryMuzzleFlash.Play();   
        }


        // Update using Melee ability ability state
        var meleeAbility = charRepAll.FindAbilityWithComponent(EntityManager,typeof(Ability_Melee.InterpolatedState));
        GameDebug.Assert(meleeAbility != Entity.Null,"AbilityController does not own a Ability_Melee ability");
        var meleeInterpolatedState = EntityManager.GetComponentData<Ability_Melee.InterpolatedState>(meleeAbility);
        if (weapon.meleeImpactEvent.Update(time, meleeInterpolatedState.impactTick))
        {
            if(weapon.meleeImpactSound != null)
                Game.SoundSystem.Play(weapon.meleeImpactSound, weapon.transform.position);// TODO this should be hand position
                
            if(weapon.meleeImpactEffect != null)
                weapon.meleeImpactEffect.Play();
        }
        
        // Update barrel state
        for (var i = 0; i < weapon.barrelSetup.states.Length; i++)
        {
            weapon.barrelSetup.states[i].animTime += deltaTime;
        }

        // Apply barrel state
        for (var i = 0; i < weapon.barrelSetup.states.Length; i++)
        {
            var localPos = weapon.barrelSetup.states[i].startPosition;

            var animDuration = weapon.barrelSetup.fireAnimCurve[weapon.barrelSetup.fireAnimCurve.length - 1].time;
            if (weapon.barrelSetup.states[i].animTime < animDuration)
            {
                var offset = weapon.barrelSetup.fireAnimCurve.Evaluate(weapon.barrelSetup.states[i].animTime);
                localPos.z += offset;
            }

            weapon.barrelSetup.barrels[i].transform.localPosition = localPos;
        }
    }
}


[DisableAutoCreation]
public class RobotWeaponClientProjectileSpawnHandler : InitializeComponentGroupSystem<ClientProjectile,RobotWeaponClientProjectileSpawnHandler.Initialzied>
{
    public struct Initialzied : IComponentData {}

    private ComponentGroup WeaponGroup;
    
    public RobotWeaponClientProjectileSpawnHandler(GameWorld world) : base(world)
    {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        WeaponGroup = GetComponentGroup(typeof(RobotWeaponA), typeof(CharacterPresentationSetup));
    }

    protected override void Initialize(ref ComponentGroup group)
    {
        var clientProjectileArray = group.GetComponentArray<ClientProjectile>();
        var weaponArray = WeaponGroup.GetComponentArray<RobotWeaponA>();
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
                    var index = weapon.barrelSetup.index;
                    var pos = weapon.barrelSetup.barrels[index].position;
//                    GameDebug.Log("MATCH weapon:" + weapon.name + "proj count;" + clientProjectileArray.Length + " Index:" + index + "muzzlepos:" + pos + " tick:" + m_world.worldTime.tick);
                    clientProjectile.SetMuzzlePosition(EntityManager, pos);
                    break;
                }
            }
        }
    }
}
