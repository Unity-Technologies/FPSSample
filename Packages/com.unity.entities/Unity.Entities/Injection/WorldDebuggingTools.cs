using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace Unity.Entities
{
    internal class WorldDebuggingTools
    {
        internal static void MatchEntityInComponentGroups(World world, Entity entity,
            List<Tuple<ScriptBehaviourManager, List<ComponentGroup>>> matchList)
        {
            using (var entityComponentTypes =
                world.GetExistingManager<EntityManager>().GetComponentTypes(entity, Allocator.Temp))
            {
                foreach (var manager in World.Active.BehaviourManagers)
                {
                    var componentGroupList = new List<ComponentGroup>();
                    var system = manager as ComponentSystemBase;
                    if (system == null) continue;
                    foreach (var componentGroup in system.ComponentGroups)
                        if (Match(componentGroup, entityComponentTypes))
                            componentGroupList.Add(componentGroup);

                    if (componentGroupList.Count > 0)
                        matchList.Add(
                            new Tuple<ScriptBehaviourManager, List<ComponentGroup>>(manager, componentGroupList));
                }
            }
        }

        private static bool Match(ComponentGroup group, NativeArray<ComponentType> entityComponentTypes)
        {
            foreach (var groupType in group.Types.Skip(1))
            {
                var found = false;
                foreach (var type in entityComponentTypes)
                {
                    if (type.TypeIndex != groupType.TypeIndex)
                        continue;
                    found = true;
                    break;
                }

                if (found == (groupType.AccessModeType == ComponentType.AccessMode.Subtractive))
                    return false;
            }

            return true;
        }
    }
}
