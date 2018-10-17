using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Xml.Schema;
using Unity.Entities;
using Unity.Properties;
using UnityEngine;

public struct CharacterAbility : IComponentData
{
    public Entity character;
}

public struct AbilityControl : IPredictedData<AbilityControl>, IComponentData
{
    public int activeAllowed;
    public int requestsActive;
	
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteBoolean("activeAllowed", activeAllowed == 1);
        writer.WriteBoolean("requestsActive", requestsActive == 1);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        activeAllowed = reader.ReadBoolean() ? 1 : 0;
        requestsActive = reader.ReadBoolean() ? 1 : 0;
    }

#if UNITY_EDITOR
    public bool VerifyPrediction(ref AbilityControl state)
    {
        return true;
    }
    
    public override string ToString()
    {
        return "AbilityControl activeAllowed:" + activeAllowed + " requestActive:" + requestsActive;
    }
#endif    
}


public class AbilityController : MonoBehaviour, INetworkSerializable
{
    [NonSerialized] public Entity[] abilityEntities = new Entity[8];
    
    [NonSerialized] public int activeAbility = -1;
    [NonSerialized] public int lastServerActiveAbility = -1;

    public Entity GetAbilityEntity(EntityManager entityManager, Type abilityType)
    {
        for (var i = 0; i < abilityEntities.Length; i++)
        {
            var entity = abilityEntities[i];
            if (entity == Entity.Null)
                continue;

            if (entityManager.HasComponent(entity, abilityType))
                return entity;
        }
        
        return Entity.Null;
    }
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyPredicting);

        writer.WriteInt16("activeAbility",(short)activeAbility);
       
        writer.ClearFieldSection();

        for (var i = 0; i < abilityEntities.Length; i++)
        {
            refSerializer.SerializeReference(ref writer, "ability", abilityEntities[i]);
        }
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        lastServerActiveAbility = reader.ReadInt16();
        activeAbility = lastServerActiveAbility;

        for (var i = 0; i < abilityEntities.Length; i++)
        {
            var entity = Entity.Null;
            refSerializer.DeserializeReference(ref reader, ref entity);
            abilityEntities[i] = entity;
        }
    }
    
    public void Rollback()
    {
        activeAbility = lastServerActiveAbility;
    }
}


[DisableAutoCreation]
public class AbilityCtrl_UpdateServerEntityComponent : BaseComponentSystem    
{
    public struct GroupType
    {
        public EntityArray entities;
        public ComponentArray<AbilityController> abilityControllers;
    }

    [Inject] 
    public GroupType Group;  
    
    public AbilityCtrl_UpdateServerEntityComponent(GameWorld world) : base(world) {}

    protected override void OnUpdate()
    {
        for (var i = 0; i < Group.abilityControllers.Length; i++)
        {
            var abilityCtrl = Group.abilityControllers[i];
            if (abilityCtrl.abilityEntities == null)
                continue;

            var abilityCtrlEntity = Group.entities[i];

            var predicted = EntityManager.HasComponent<ServerEntity>(abilityCtrlEntity);
            for (var j = 0; j < abilityCtrl.abilityEntities.Length; j++)
            {
                var ability = abilityCtrl.abilityEntities[j];
                if (ability == Entity.Null)
                    continue;
                
                if (EntityManager.HasComponent<ServerEntity>(ability) != predicted)
                {
                    if(predicted)
                        PostUpdateCommands.AddComponent(ability, new ServerEntity());
                    else
                        PostUpdateCommands.RemoveComponent<ServerEntity>(ability);
                }
            }
        }
    }
}

[DisableAutoCreation]
[AlwaysUpdateSystem]
public class AbilityCtrl_HandleSpawn : InitializeComponentSystem<AbilityController>
{
    public AbilityCtrl_HandleSpawn(GameWorld world) : base(world) {}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void Initialize(Entity entity, AbilityController component)
    {
        // Set character as owner of abilities
        for (var i = 0; i < component.abilityEntities.Length; i++)
        {
            var abilityEntity = component.abilityEntities[i];
            if (abilityEntity == Entity.Null)
                continue;

            var gameObjectEntity = component.GetComponent<GameObjectEntity>();
                
            var charAbility = EntityManager.GetComponentData<CharacterAbility>(abilityEntity);
            charAbility.character = gameObjectEntity.Entity;
            EntityManager.SetComponentData(abilityEntity,charAbility);
        }
    }
}



[DisableAutoCreation]
public class AbilityCtrl_Update : BaseComponentSystem
{
    ComponentGroup Group;   
    
    public AbilityCtrl_Update(GameWorld world) : base(world) {}

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        Group = GetComponentGroup(typeof(ServerEntity), typeof(AbilityController));
    }

    protected override void OnUpdate()
    {
        var entityArray = Group.GetEntityArray();
        var abilityControllerArray = Group.GetComponentArray<AbilityController>();
        for (var i = 0; i < entityArray.Length; i++)
        {
            var abilityController = abilityControllerArray[i];

            if (abilityController.abilityEntities == null)
                continue;
            
            var abilityControllerEntity = entityArray[i];
            
            // Handle command
            var requestedAbility = -1;
            for (var j = 0; j < abilityController.abilityEntities.Length; j++)
            {
                var abilityEntity = abilityController.abilityEntities[j];
                if (abilityEntity == Entity.Null)
                    continue;
                
                var isActive = j == abilityController.activeAbility;
                var ability = EntityManager.GetComponentData<AbilityControl>(abilityEntity);
                var requestActive = ability.requestsActive == 1;
                
                if(requestActive)
                {
                    // TODO we simply select last that is requested. We need to setup conditions
                    requestedAbility = j;
                    if(isActive)
                        break;
                } 
            }

            if (requestedAbility != abilityController.activeAbility)
            {
                if (abilityController.activeAbility != -1)
                {
                    var abilityEntity = abilityController.abilityEntities[abilityController.activeAbility];
                    var ability = EntityManager.GetComponentData<AbilityControl>(abilityEntity);
                    ability.activeAllowed = 0;
                    EntityManager.SetComponentData(abilityEntity,ability);
                }
                    
                abilityController.activeAbility = requestedAbility;

                if (abilityController.activeAbility != -1)
                {
                    var abilityEntity = abilityController.abilityEntities[abilityController.activeAbility];
                    var ability = EntityManager.GetComponentData<AbilityControl>(abilityEntity);
                    ability.activeAllowed = 1;
                    EntityManager.SetComponentData(abilityEntity,ability);
                }
                
                var character = EntityManager.GetComponentObject<CharacterPredictedState>(abilityControllerEntity);
                character.State.abilityActive = abilityController.activeAbility != -1;
            }
        }
    }
}

[DisableAutoCreation]
public class AbilityCtrl_Rollback : ComponentSystem 
{
    public struct GroupType
    {
        public ComponentDataArray<ServerEntity> serverBehavior;
        public ComponentArray<AbilityController> abilityController;
    }

    [Inject] 
    public GroupType Group;   
    
    public AbilityCtrl_Rollback(GameWorld gameWorld)
    {
    }

    protected override void OnUpdate()
    {
        for (var i = 0; i < Group.abilityController.Length; i++)
        {
            Group.abilityController[i].Rollback();
        }
    }
}
