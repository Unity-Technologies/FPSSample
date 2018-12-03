using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "CharBehaviourController",menuName = "FPS Sample/CharBehaviour/DefaultController")]
public class DefaultCharBehaviourController : ReplicatedEntityFactory
{
    [ConfigVar(Name = "charbehaviour.showstate", DefaultValue = "0", Description = "show state")]
    public static ConfigVar ShowInfo;
                                                                                         
    public enum Ability
    {
        None,
        PrimFire,
        SecFire,
        Sprint,
        Melee,
        Emote,
    }
    
    
    public struct InternalState : IComponentData
    {
        public Entity abilityMovement;
        public Entity abilityPrimFire;
        public Entity abilitySecFire;
        public Entity abilitySprint;
        public Entity abilityMelee;
        public Entity abilityEmote;
        public Entity abilityDead;
        
        public Entity GetAbilityBehavior(Ability ability)
        {
            switch (ability)
            {
                case Ability.PrimFire:
                    return abilityPrimFire;
                case Ability.SecFire:
                    return abilitySecFire;
                case Ability.Sprint:
                    return abilitySprint;
                case Ability.Melee:
                    return abilityMelee;
                case Ability.Emote:
                    return abilityEmote;
            }
            return Entity.Null;
        }

        
        public int initialized;
    }
    
    public struct PredictedState : INetPredicted<PredictedState>, IComponentData
    {
        public Ability activeAbility;
        public int dead;

        public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
        {
            writer.WriteByte("activeability", (byte)activeAbility);
            writer.WriteBoolean("dead", dead == 1);
        }

        public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
        {
            activeAbility = (Ability)reader.ReadByte();
            dead = reader.ReadBoolean() ? 1 : 0;
        }

#if UNITY_EDITOR
        public bool VerifyPrediction(ref PredictedState state)
        {
            return true;
        }
    
        public override string ToString()
        {
            var strBuilder = new System.Text.StringBuilder();
            strBuilder.AppendLine("activeAbility:" + activeAbility);
            strBuilder.AppendLine("dead:" + dead);
            return strBuilder.ToString();
        }
#endif    
    }

    public CharBehaviorFactory abilityMovement;
//    public CharBehaviorFactory abilityPrimFire;
//    public CharBehaviorFactory abilitySecFire;
    public CharBehaviorFactory abilitySprint;
    public CharBehaviorFactory abilityMelee;
    public CharBehaviorFactory abilityEmote;
    public CharBehaviorFactory abilityDead;
    
    
    static List<Entity> entities = new List<Entity>(16);
    public override Entity Create(EntityManager entityManager, int predictingPlayerId)
    {
        entities.Clear();
        
        var entity = entityManager.CreateEntity();

        // We add CharBehaviour to BehaviourController so we have a common way to define character          
        entityManager.AddComponentData(entity, new CharBehaviour());
        
        // Add uninitialized replicated entity
        var repData = new ReplicatedDataEntity
        {
            id = -1,
            typeId = registryId,
            predictingPlayerId = predictingPlayerId,
        };
        entityManager.AddComponentData(entity, repData);
            
        var internalState = new InternalState
        {
            abilityMovement = abilityMovement.Create(entityManager, entities),
//            abilityPrimFire = abilityPrimFire.Create(entityManager, entities),
//            abilitySecFire = abilitySecFire.Create(entityManager, entities),
            abilitySprint = abilitySprint.Create(entityManager, entities),
            abilityMelee = abilityMelee.Create(entityManager, entities),
            abilityEmote = abilityEmote.Create(entityManager, entities),
            abilityDead = abilityDead.Create(entityManager, entities),
        };
        entityManager.AddComponentData(entity, internalState);
        entityManager.AddComponentData(entity, new PredictedState());

        
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
}


#if UNITY_EDITOR
[CustomEditor(typeof(DefaultCharBehaviourController))]
public class DefaultCharBehaviourControllerEditor : ReplicatedFactoryEntryEditor<DefaultCharBehaviourController>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawDefaultInspector();
    }
}
#endif

[DisableAutoCreation]
class DefaultBehaviourController_Update : BaseComponentDataSystem<CharBehaviour, DefaultCharBehaviourController.InternalState, DefaultCharBehaviourController.PredictedState>
{
    public DefaultBehaviourController_Update(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
	
    protected override void Update(Entity entity, CharBehaviour charBehavior, DefaultCharBehaviourController.InternalState internalState, DefaultCharBehaviourController.PredictedState predictedState)
    {
        if (internalState.initialized == 0)
        {
            Activate(internalState.abilityMovement);
            internalState.initialized = 1;
            EntityManager.SetComponentData(entity, internalState);
        }
        
        var command = EntityManager.GetComponentObject<UserCommandComponent>(charBehavior.character).command;

        var healthState = EntityManager.GetComponentObject<HealthState>(charBehavior.character);
        if (healthState.health <= 0 && predictedState.dead == 0)
        {
            if(predictedState.activeAbility != DefaultCharBehaviourController.Ability.None)
            {
                var behavior = internalState.GetAbilityBehavior(predictedState.activeAbility);
                Deactivate(behavior);
            }
            Deactivate(internalState.abilityMovement);
            Activate(internalState.abilityDead);

            predictedState.dead = 1;
            EntityManager.SetComponentData(entity, predictedState);
        }

        if (predictedState.dead == 1)
            return;
        
        // Check for abilities done
        if (predictedState.activeAbility != DefaultCharBehaviourController.Ability.None)
        {
            var activeBehavior = internalState.GetAbilityBehavior(predictedState.activeAbility);
            var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(activeBehavior);
            if (abilityCtrl.behaviorState != AbilityControl.State.Active)
            {
//                GameDebug.Log("Behavior done:" + activeBehavior);
                Deactivate(activeBehavior);
                predictedState.activeAbility = DefaultCharBehaviourController.Ability.None;
            }
        }
        
        if (IsRequestingActive(ref internalState, DefaultCharBehaviourController.Ability.PrimFire))
        {
            AttempActivate(ref internalState, ref predictedState, DefaultCharBehaviourController.Ability.PrimFire, false);
        }
        else if (command.secondaryFire)
        {
            AttempActivate(ref internalState, ref predictedState, DefaultCharBehaviourController.Ability.SecFire, false);
        }
        else if (command.melee)
        {
            var force = predictedState.activeAbility == DefaultCharBehaviourController.Ability.Sprint;
            AttempActivate(ref internalState, ref predictedState, DefaultCharBehaviourController.Ability.Melee, force);
        }
        else if (command.sprint)
        {
            AttempActivate(ref internalState, ref predictedState, DefaultCharBehaviourController.Ability.Sprint, false);
        }
        else if (command.emote != CharacterEmote.None)
        {
            if(command.moveMagnitude == 0)
                AttempActivate(ref internalState, ref predictedState, DefaultCharBehaviourController.Ability.Emote, false);
        }
        
        EntityManager.SetComponentData(entity, predictedState);


        if(DefaultCharBehaviourController.ShowInfo.IntValue > 0)
        {
            int x = 1;
            int y = 2;
            DebugOverlay.Write(x, y++, "Ability controller");
            DebugOverlay.Write(x, y++, "  Active ability:" + predictedState.activeAbility);
            DebugOverlay.Write(x, y++, "  dead:" + predictedState.dead);
        }
    }

    bool IsRequestingActive(ref DefaultCharBehaviourController.InternalState internalState, DefaultCharBehaviourController.Ability ability)
    {
        var behavior = internalState.GetAbilityBehavior(ability);
        var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(behavior);
        return abilityCtrl.behaviorState == AbilityControl.State.RequestActive;
    }
    
    void AttempActivate(ref DefaultCharBehaviourController.InternalState internalState, 
        ref DefaultCharBehaviourController.PredictedState predictedState, DefaultCharBehaviourController.Ability ability, bool force)
    {
        if (predictedState.activeAbility == ability)
            return;

        if(DefaultCharBehaviourController.ShowInfo.IntValue > 0)
            GameDebug.Log("AttempActivate:" + ability);

        if (predictedState.activeAbility != DefaultCharBehaviourController.Ability.None)
        {
            var behavior = internalState.GetAbilityBehavior(predictedState.activeAbility);
            if (force)
            {
                Deactivate(behavior);
            }
            else
            {
                RequestDeactivate(behavior);
                return;
            }
        }

        {
            predictedState.activeAbility = ability;
            var behavior = internalState.GetAbilityBehavior(predictedState.activeAbility);
            Activate(behavior);
        }
    }

    void Activate(Entity behaviour)
    {
        if(DefaultCharBehaviourController.ShowInfo.IntValue > 0)
            GameDebug.Log("Activate:" + behaviour);
        var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(behaviour);
        abilityCtrl.active = 1;
        abilityCtrl.requestDeactivate = 0;
        EntityManager.SetComponentData(behaviour, abilityCtrl);
    }
    
    void Deactivate(Entity ability)
    {
        if(DefaultCharBehaviourController.ShowInfo.IntValue > 0)
            GameDebug.Log("Deactivate:" + ability);
        var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(ability);
        abilityCtrl.active = 0;
        EntityManager.SetComponentData(ability, abilityCtrl);
    }
    
    void RequestDeactivate(Entity ability)
    {
        if(DefaultCharBehaviourController.ShowInfo.IntValue > 0)
           GameDebug.Log("RequestDeactivate:" + ability);
        var abilityCtrl = EntityManager.GetComponentData<AbilityControl>(ability);
        abilityCtrl.requestDeactivate = 1;
        EntityManager.SetComponentData(ability, abilityCtrl);
    }
}
