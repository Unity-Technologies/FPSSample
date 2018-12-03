using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif



[CreateAssetMenu(fileName = "ItemTypeDefinition", menuName = "FPS Sample/Item/TypeDefinition")]
public class ItemTypeDefinition : ReplicatedEntityFactory
{
    public WeakAssetReference prefabServer;
    public WeakAssetReference prefabClient;
    public WeakAssetReference prefab1P;
    
    public CharBehaviorFactory abilityPrimFire;
    public CharBehaviorFactory abilitySecFire;
    
    
    public struct InternalState : IComponentData
    {
        public Entity abilityPrimFire;
        public Entity abilitySecFire;
    }

    public struct State : IComponentData, INetSerialized
    {
        public Entity character;
        
        public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
        {
            refSerializer.SerializeReference(ref writer,"character",character);
        }

        public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
        {
            refSerializer.DeserializeReference(ref reader, ref character);
        }
    }

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
            
        entityManager.AddComponentData(entity, new State());
        
        var internalState = new InternalState
        {
            abilityPrimFire = abilityPrimFire.Create(entityManager, entities),
            abilitySecFire = abilitySecFire.Create(entityManager, entities),
        };
        entityManager.AddComponentData(entity, internalState);
        
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
[CustomEditor(typeof(ItemTypeDefinition))]
public class ItemTypeDefinitionEditor : ReplicatedFactoryEntryEditor<ItemTypeDefinition>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawDefaultInspector();
    }
}
#endif



[DisableAutoCreation]
public class HandleItemSpawn : InitializeComponentDataSystem<ItemTypeDefinition.State, HandleItemSpawn.Initialized>
{
    public struct Initialized : IComponentData{}
    
    public HandleItemSpawn(GameWorld gameWorld) : base(gameWorld)
    {}

    protected override void Initialize(Entity entity, ItemTypeDefinition.State component)
    {
        // Update character references on all behaviours
        var buffer = EntityManager.GetBuffer<EntityGroupChildren>(entity);
        for (int j = 0; j < buffer.Length; j++)
        {
            var childEntity = buffer[j].entity;
            if (EntityManager.HasComponent<CharBehaviour>(childEntity))
            {
                var charBehaviour = EntityManager.GetComponentData<CharBehaviour>(childEntity);
                charBehaviour.character = component.character;
                EntityManager.SetComponentData(childEntity, charBehaviour);
            }
        }
        
        // TODO (mogensh) this is a very hacked approach to setting item abilities. Make more general way that supports item swap
        var internalState = EntityManager.GetComponentData<ItemTypeDefinition.InternalState>(entity);
        var character = EntityManager.GetComponentObject<Character>(component.character);
        character.item = entity;
        var behaviourCtrlInternalState =
            EntityManager.GetComponentData<DefaultCharBehaviourController.InternalState>(character.behaviourController);

        behaviourCtrlInternalState.abilityPrimFire = internalState.abilityPrimFire;
        behaviourCtrlInternalState.abilitySecFire = internalState.abilitySecFire;
        EntityManager.SetComponentData(character.behaviourController, behaviourCtrlInternalState);
        
//        var behaviourCtrlBuffer = EntityManager.GetBuffer<EntityGroupChildren>(character.behaviourController);
//        behaviourCtrlBuffer.Add(new EntityGroupChildren { entity = internalState.abilityPrimFire });
//        behaviourCtrlBuffer.Add(new EntityGroupChildren { entity = internalState.abilitySecFire });
    }
}
