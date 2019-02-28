using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "AbilityCollection", menuName = "FPS Sample/CharBehaviour/AbilityCollection")]
public class AbilityCollection : ReplicatedEntityFactory
{
    [ConfigVar(Name = "charbehaviour.showstate", DefaultValue = "0", Description = "show state")]
    public static ConfigVar ShowInfo;

    public struct InternalState : IComponentData
    {
        public int foo;
    }

    [InternalBufferCapacity(8)]
    public struct AbilityEntry : IBufferElementData
    {
        public Entity ability;
        public uint canRunWith;
        public uint canInterrupt;
    }

    [Serializable]
    public class AbilitySetup
    {
        [Tooltip("Factory that creates ability")]
        public CharBehaviorFactory factory;
        
        public CharBehaviorFactory[] canRunWith;
        
        public bool canInterruptAll;
        public CharBehaviorFactory[] canInterrupt;
    }

    public AbilitySetup[] abilities = new AbilitySetup[0];
    
    static List<Entity> entities = new List<Entity>(16);
    public override Entity Create(EntityManager entityManager, BundledResourceManager resourceManager, 
        GameWorld world)
    {
        entities.Clear();
        
        var entity = entityManager.CreateEntity();

        var repData = new ReplicatedEntityData(guid);

        var internalState = new InternalState
        {};

        entityManager.AddComponentData(entity, repData);
        entityManager.AddComponentData(entity, internalState);

        
        // Create ability entities
        var abilityEntities = new List<Entity>(abilities.Length);
        for (int i = 0; i < abilities.Length; i++)
        {
            abilityEntities.Add(abilities[i].factory.Create(entityManager, entities));
        }

        // Add abilities to ability buffer
        entityManager.AddBuffer<AbilityEntry>(entity);
        var abilityBuffer = entityManager.GetBuffer<AbilityEntry>(entity);
        
        for (int i = 0; i < abilities.Length; i++)
        {
            uint canRunWith = 0;
            foreach (var ability in abilities[i].canRunWith)
            {
                var abilityIndex = GetAbilityIndex(ability);
                canRunWith |= 1U << abilityIndex;
            }

            uint canInterrupt = 0;
            if (abilities[i].canInterruptAll)
            {
                canInterrupt = ~0U;
            }
            else
            {
                foreach (var ability in abilities[i].canInterrupt)
                {
                    var abilityIndex = GetAbilityIndex(ability);
                    canInterrupt |= 1U << abilityIndex;
                }
            }
            
            abilityBuffer.Add(new AbilityEntry
            {
                ability = abilityEntities[i],
                canRunWith = canRunWith,
                canInterrupt = canInterrupt,
            });
        }

        
        
        // Register replicated entity children so they all get serialized as data on this entity
        entityManager.AddBuffer<EntityGroupChildren>(entity);
        var buffer = entityManager.GetBuffer<EntityGroupChildren>(entity);
        GameDebug.Assert(entities.Count <= buffer.Capacity,"Buffer capacity is to small to fit all behaviors");
        for (int i = 0; i < entities.Count; i++)
        {
            var elm = new EntityGroupChildren
            {
                entity = entities[i]
            };
            buffer.Add(elm);
        }

        return entity;
    }

    int GetAbilityIndex(CharBehaviorFactory factory)
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            if (abilities[i].factory == factory)
                return i;
        }

        return -1;
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(AbilityCollection))]
public class DefaultCharBehaviourControllerEditor : ReplicatedEntityFactoryEditor<AbilityCollection>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawDefaultInspector();
    }
}
#endif

[DisableAutoCreation]
class DefaultBehaviourController_Update : BaseComponentDataSystem<AbilityCollection.InternalState>
{
    public DefaultBehaviourController_Update(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
	
    protected override void Update(Entity entity, AbilityCollection.InternalState internalState)
    {

        var abilityEntries = EntityManager.GetBuffer<AbilityCollection.AbilityEntry>(entity);
        
        // Check for abilities done
        for (int i = 0; i < abilityEntries.Length; i++)
        {
            var ability = abilityEntries[i].ability;
            var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(ability);
            if (abilityCtrl.active == 1 && abilityCtrl.behaviorState != AbilityControl.State.Active)
            {
//                GameDebug.Log("Behavior done:" + ability);
                Deactivate(ability);
            }
        }

        // Get active abilties
        uint activeAbilityFlags = 0;
        for (int i = 0; i < abilityEntries.Length; i++)
        {
            var ability = abilityEntries[i].ability;
            var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(ability);

            if (abilityCtrl.active == 1)
                activeAbilityFlags |= 1U << i;
        }
        
        // Check for ability activate
        for (int i = 0; i < abilityEntries.Length; i++)
        {
            var abilityEntry = abilityEntries[i];
            
            var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(abilityEntry.ability);
            if (abilityCtrl.active == 1)
                continue;

            if (abilityCtrl.behaviorState != AbilityControl.State.RequestActive)
                continue;
            
//            if(DefaultCharBehaviourController.ShowInfo.IntValue > 0)
//                GameDebug.Log("AttempActivate:" + ability);


            var canNotRunWith = activeAbilityFlags & ~abilityEntries[i].canRunWith;
            if (activeAbilityFlags == 0 || canNotRunWith == 0)
            {
                Activate(abilityEntry.ability);
                continue;
            }

            var canActivate = true;
            for (int j = 0; j < abilityEntries.Length; j++)
            {
                var flag = 1U << j;
                var blocking = (canNotRunWith & flag) > 0;
                if (blocking)
                {
                    var canInterrupt = (abilityEntry.canInterrupt & flag) > 0;
                    if (canInterrupt)
                    {
                        Deactivate(abilityEntries[j].ability);
                    }
                    else
                    {
                        RequestDeactivate(abilityEntries[j].ability);
                        canActivate = false;
                    }
                }
            }

            if (canActivate)
            {
                Activate(abilityEntry.ability);
            }
        }
        
        

        if(AbilityCollection.ShowInfo.IntValue > 0)
        {
            int x = 1;
            int y = 2;
            DebugOverlay.Write(x, y++, "Ability controller");
            
            for (int i = 0; i < abilityEntries.Length; i++)
            {
                var ability = abilityEntries[i].ability;
                var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(ability);

                DebugOverlay.Write(x, y++, " " + ability);
                DebugOverlay.Write(x, y++, "   active:" + abilityCtrl.active);
                DebugOverlay.Write(x, y++, "   request deactivate:" + abilityCtrl.requestDeactivate);
                DebugOverlay.Write(x, y++, "   state:" + abilityCtrl.behaviorState);
            }
            
            
//            DebugOverlay.Write(x, y++, "  Active ability:" + predictedState.activeAbility);
//            DebugOverlay.Write(x, y++, "  dead:" + predictedState.dead);
        }
    }



    void Activate(Entity behaviour)
    {
        if(AbilityCollection.ShowInfo.IntValue > 0)
            GameDebug.Log("Activate:" + behaviour);
        var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(behaviour);
        abilityCtrl.active = 1;
        abilityCtrl.requestDeactivate = 0;
        EntityManager.SetComponentData(behaviour, abilityCtrl);
    }
    
    void Deactivate(Entity ability)
    {
        if(AbilityCollection.ShowInfo.IntValue > 0)
            GameDebug.Log("Deactivate:" + ability);
        var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(ability);
        abilityCtrl.active = 0;
        EntityManager.SetComponentData(ability, abilityCtrl);
    }
    
    void RequestDeactivate(Entity ability)
    {
        if(AbilityCollection.ShowInfo.IntValue > 0)
           GameDebug.Log("RequestDeactivate:" + ability);
        var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(ability);
        abilityCtrl.requestDeactivate = 1;
        EntityManager.SetComponentData(ability, abilityCtrl);
    }
}
