using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Entities
{
    public static class EntityManagerExtensions
    {
        unsafe public static Entity Instantiate(this EntityManager entityManager, GameObject srcGameObject)
        {
            if (entityManager.m_CachedComponentList == null)
                entityManager.m_CachedComponentList = new List<ComponentDataWrapperBase>();

            var components = (List<ComponentDataWrapperBase>)entityManager.m_CachedComponentList;
            srcGameObject.GetComponents(components);
            var count = components.Count;
            ComponentType* componentTypes = stackalloc ComponentType[count];

            for (var t = 0; t != count; ++t)
                componentTypes[t] = components[t].GetComponentType();

            var srcEntity = entityManager.CreateEntity(entityManager.CreateArchetype(componentTypes, count));
            for (var t = 0; t != count; ++t)
                components[t].UpdateComponentData(entityManager, srcEntity);

            return srcEntity;
        }

        public static unsafe void Instantiate(this EntityManager entityManager, GameObject srcGameObject, NativeArray<Entity> outputEntities)
        {
            if (outputEntities.Length == 0)
                return;

            var entity = entityManager.Instantiate(srcGameObject);
            outputEntities[0] = entity;

            var entityPtr = (Entity*)outputEntities.GetUnsafePtr();
            entityManager.InstantiateInternal(entity, entityPtr + 1, outputEntities.Length - 1);
        }

        public static unsafe T GetComponentObject<T>(this EntityManager entityManager, Entity entity) where T : Component
        {
            var componentType = ComponentType.Create<T>();
            entityManager.Entities->AssertEntityHasComponent(entity, componentType.TypeIndex);

            Chunk* chunk;
            int chunkIndex;
            entityManager.Entities->GetComponentChunk(entity, out chunk, out chunkIndex);
            return entityManager.ArchetypeManager.GetManagedObject(chunk, componentType, chunkIndex) as T;
        }
    }
}
