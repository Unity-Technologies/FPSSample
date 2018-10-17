using Unity.Entities.Properties;
using UnityEngine;

namespace Unity.Entities.Editor
{
    public class EntitySelectionProxy : ScriptableObject
    {
        public EntityContainer Container { get; private set; }
        public Entity Entity { get; private set; }
        public EntityManager EntityManager { get; private set; }
        public World World { get; private set; }

        public bool Exists => EntityManager != null && EntityManager.IsCreated && EntityManager.Exists(Entity);

        public void SetEntity(World world, Entity entity)
        {
            this.World = world;
            this.Entity = entity;
            this.EntityManager = world.GetExistingManager<EntityManager>();
            this.Container = new EntityContainer(EntityManager, Entity);
        }
    }
}
