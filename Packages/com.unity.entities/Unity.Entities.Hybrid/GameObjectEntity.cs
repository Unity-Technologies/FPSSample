using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RequireComponent = UnityEngine.RequireComponent;
using SerializeField = UnityEngine.SerializeField;
using MonoBehaviour = UnityEngine.MonoBehaviour;
using DisallowMultipleComponent = UnityEngine.DisallowMultipleComponent;
using GameObject = UnityEngine.GameObject;
using Component = UnityEngine.Component;

namespace Unity.Entities
{
    //@TODO: This should be fully implemented in C++ for efficiency
    [RequireComponent(typeof(GameObjectEntity))]
    public abstract class ComponentDataWrapperBase : MonoBehaviour, ISerializationCallbackReceiver
    {
        internal abstract ComponentType GetComponentType();
        internal abstract void UpdateComponentData(EntityManager manager, Entity entity);
        internal abstract void UpdateSerializedData(EntityManager manager, Entity entity);

        internal abstract int InsertSharedComponent(EntityManager manager);
        internal abstract void UpdateSerializedData(EntityManager manager, int sharedComponentIndex);

        internal bool CanSynchronizeWithEntityManager(out EntityManager entityManager, out Entity entity)
        {
            entityManager = null;
            entity = Entity.Null;
            var gameObjectEntity = GetComponent<GameObjectEntity>();
            if (gameObjectEntity == null)
                return false;
            if (gameObjectEntity.EntityManager == null)
                return false;
            if (!gameObjectEntity.EntityManager.Exists(gameObjectEntity.Entity))
                return false;
            if (!gameObjectEntity.EntityManager.HasComponent(gameObjectEntity.Entity, GetComponentType()))
                return false;
            entityManager = gameObjectEntity.EntityManager;
            entity = gameObjectEntity.Entity;
            return true;
        }
        
        void OnValidate()
        {
            if (CanSynchronizeWithEntityManager(out var entityManager, out var entity))
                UpdateComponentData(entityManager, entity);
        }
        
        public void OnBeforeSerialize()
        {
            if (CanSynchronizeWithEntityManager(out var entityManager, out var entity))
                UpdateSerializedData(entityManager, entity);
        }

        public void OnAfterDeserialize() { }
    }

    internal sealed class WrappedComponentDataAttribute : PropertyAttribute
    {
    }

    //@TODO: This should be fully implemented in C++ for efficiency
    public class ComponentDataWrapper<T> : ComponentDataWrapperBase where T : struct, IComponentData
    {
        [SerializeField, WrappedComponentData]
        T m_SerializedData;

        public T Value
        {
            get
            {
                return m_SerializedData;
            }
            set
            {
                m_SerializedData = value;
                if (CanSynchronizeWithEntityManager(out var entityManager, out var entity))
                    UpdateComponentData(entityManager, entity);
            }
        }


        internal override ComponentType GetComponentType()
        {
            return ComponentType.Create<T>();
        }

        internal override void UpdateComponentData(EntityManager manager, Entity entity)
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var componentType = ComponentType.FromTypeIndex(typeIndex);
            if (componentType.IsZeroSized)
                return;

            manager.SetComponentData(entity, m_SerializedData);
        }
        
        internal override void UpdateSerializedData(EntityManager manager, Entity entity)
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var componentType = ComponentType.FromTypeIndex(typeIndex);
            if (componentType.IsZeroSized) 
                return;
                
            m_SerializedData = manager.GetComponentData<T>(entity);
        }
        
        internal override int InsertSharedComponent(EntityManager manager)
        {
            throw new InvalidOperationException();
        }

        internal override void UpdateSerializedData(EntityManager manager, int sharedComponentIndex)
        {
            throw new InvalidOperationException();
        }
    }

    //@TODO: This should be fully implemented in C++ for efficiency
    public class SharedComponentDataWrapper<T> : ComponentDataWrapperBase where T : struct, ISharedComponentData
    {
        [SerializeField, WrappedComponentData]
        T m_SerializedData;

        public T Value
        {
            get
            {
                return m_SerializedData;
            }
            set
            {
                m_SerializedData = value;
                if (CanSynchronizeWithEntityManager(out var entityManager, out var entity))
                    UpdateComponentData(entityManager, entity);
            }
        }


        internal override ComponentType GetComponentType()
        {
            return ComponentType.Create<T>();
        }

        internal override void UpdateComponentData(EntityManager manager, Entity entity)
        {
            manager.SetSharedComponentData(entity, m_SerializedData);
        }
        
        internal override void UpdateSerializedData(EntityManager manager, Entity entity)
        {
            m_SerializedData = manager.GetSharedComponentData<T>(entity);
        }

        internal override int InsertSharedComponent(EntityManager manager)
        {
            return manager.m_SharedComponentManager.InsertSharedComponent(m_SerializedData);
        }

        internal override void UpdateSerializedData(EntityManager manager, int sharedComponentIndex)
        {
            m_SerializedData = manager.m_SharedComponentManager.GetSharedComponentData<T>(sharedComponentIndex);
        }
    }

    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class GameObjectEntity : MonoBehaviour
    {
        public EntityManager EntityManager { get; private set; }

        public Entity Entity { get; private set; }

        //@TODO: Very wrong error messages when creating entity with empty ComponentType array?

        public static Entity AddToEntityManager(EntityManager entityManager, GameObject gameObject)
        {
            ComponentType[] types;
            Component[] components;
            GetComponents(gameObject, true, out types, out components);

            var archetype = entityManager.CreateArchetype(types);
            var entity = CreateEntity(entityManager, archetype, components, types);

            return entity;
        }

        static void GetComponents(GameObject gameObject, bool includeGameObjectComponents, out ComponentType[] types, out Component[] components)
        {
            components = gameObject.GetComponents<Component>();

            var componentCount = 0;
            if (includeGameObjectComponents)
            {
                var gameObjectEntityComponent = gameObject.GetComponent<GameObjectEntity>();
                componentCount = gameObjectEntityComponent == null ? components.Length : components.Length - 1;
            }
            else
            {
                for (var i = 0; i != components.Length; i++)
                {
                    if (components[i] is ComponentDataWrapperBase)
                        componentCount++;
                }
            }

            types = new ComponentType[componentCount];

            var t = 0;
            for (var i = 0; i != components.Length; i++)
            {
                var com = components[i];
                var componentData = com as ComponentDataWrapperBase;

                if (componentData != null)
                    types[t++] = componentData.GetComponentType();
                else if (includeGameObjectComponents && !(com is GameObjectEntity))
                    types[t++] = com.GetType();
            }
        }

        static Entity CreateEntity(EntityManager entityManager, EntityArchetype archetype, IReadOnlyList<Component> components, IReadOnlyList<ComponentType> types)
        {
            var entity = entityManager.CreateEntity(archetype);
            var t = 0;
            for (var i = 0; i != components.Count; i++)
            {
                var com = components[i];
                var componentDataWrapper = com as ComponentDataWrapperBase;

                if (componentDataWrapper != null)
                {
                    componentDataWrapper.UpdateComponentData(entityManager, entity);
                    t++;
                }
                else if (!(com is GameObjectEntity))
                {
                    entityManager.SetComponentObject(entity, types[t], com);
                    t++;
                }
            }
            return entity;
        }

        public void OnEnable()
        {
            #if UNITY_EDITOR
            if (World.Active == null)
            {
                // * OnDisable (Serialize monobehaviours in temporary backup)
                // * unload domain
                // * load new domain
                // * OnEnable (Deserialize monobehaviours in temporary backup)
                // * mark entered playmode / load scene
                // * OnDisable / OnDestroy
                // * OnEnable (Loading object from scene...)
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    // We are just gonna ignore this enter playmode reload.
                    // Can't see a situation where it would be useful to create something inbetween.
                    // But we really need to solve this at the root. The execution order is kind if crazy.
                    if (!EditorApplication.isPlaying)
                        return;
                    
//                    Debug.LogError("Loading GameObjectEntity in Playmode but there is no active World");
                    return;
                }
                else
                {
#if UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP
                    return;
#else
                    DefaultWorldInitialization.Initialize("Editor World", true);
#endif
                }
            }
            #endif

            EntityManager = World.Active.GetOrCreateManager<EntityManager>();
            Entity = AddToEntityManager(EntityManager, gameObject);
        }

        public void OnDisable()
        {
            if (EntityManager != null && EntityManager.IsCreated && EntityManager.Exists(Entity))
                EntityManager.DestroyEntity(Entity);

            EntityManager = null;
            Entity = new Entity();
        }
    }
}
