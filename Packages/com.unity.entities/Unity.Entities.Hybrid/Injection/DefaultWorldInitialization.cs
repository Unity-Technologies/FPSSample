using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Unity.Entities
{
    static class DefaultWorldInitialization
    {
        static void DomainUnloadShutdown()
        {
            World.DisposeAllWorlds();
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop();
        }

        static void GetBehaviourManagerAndLogException(World world, Type type)
        {
            try
            {
                world.GetOrCreateManager(type);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void Initialize(string worldName, bool editorWorld)
        {
            var world = new World(worldName);
            World.Active = world;

            // Register hybrid injection hooks
            InjectionHookSupport.RegisterHook(new GameObjectArrayInjectionHook());
            InjectionHookSupport.RegisterHook(new TransformAccessArrayInjectionHook());
            InjectionHookSupport.RegisterHook(new ComponentArrayInjectionHook());

            PlayerLoopManager.RegisterDomainUnload(DomainUnloadShutdown, 10000);

            IEnumerable<Type> allTypes;

            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    allTypes = ass.GetTypes();

                }
                catch (ReflectionTypeLoadException e)
                {
                    allTypes = e.Types.Where(t => t != null);
                    Debug.LogWarning("DefaultWorldInitialization failed loading assembly: " + ass.Location);
                }

                // Create all ComponentSystem
                CreateBehaviourManagersForMatchingTypes(editorWorld, allTypes, world);
            }
            
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
        }

        static void CreateBehaviourManagersForMatchingTypes(bool editorWorld, IEnumerable<Type> allTypes, World world)
        {
            var systemTypes = allTypes.Where(t =>
                t.IsSubclassOf(typeof(ComponentSystemBase)) &&
                !t.IsAbstract &&
                !t.ContainsGenericParameters &&
                t.GetCustomAttributes(typeof(DisableAutoCreationAttribute), true).Length == 0);
            foreach (var type in systemTypes)
            {
                if (editorWorld && type.GetCustomAttributes(typeof(ExecuteInEditMode), true).Length == 0)
                    continue;

                GetBehaviourManagerAndLogException(world, type);
            }
        }
    }
}
