using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.LowLevel;
using UnityEngine.Experimental.PlayerLoop;

namespace Unity.Entities
{
    // Updating before or after an engine system guarantees it is in the same update phase as the dependency
    // Update After a phase means in that pase but after all engine systems, Before a phase means in that phase but before all engine systems
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UpdateBeforeAttribute : Attribute
    {
        public UpdateBeforeAttribute(Type systemType)
        {
            SystemType = systemType;
        }

        public Type SystemType { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UpdateAfterAttribute : Attribute
    {
        public UpdateAfterAttribute(Type systemType)
        {
            SystemType = systemType;
        }

        public Type SystemType { get; }
    }

    // Updating in a group means all dependencies from that group are inherited. A system can be in multiple goups
    // There is nothing preventing systems from being in multiple groups, it can be added if there is a use-case for it
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateInGroupAttribute : Attribute
    {
        public UpdateInGroupAttribute(Type groupType)
        {
            GroupType = groupType;
        }

        public Type GroupType { get; }
    }

    public static class ScriptBehaviourUpdateOrder
    {
        // Try to find a system of the specified type in the default playerloop and update the min / max insertion position
        private static void UpdateInsertionPos(DependantBehavior target, Type dep, PlayerLoopSystem defaultPlayerLoop,
            bool after)
        {
            var pos = 0;
            foreach (var sys in defaultPlayerLoop.subSystemList)
            {
                ++pos;
                if (sys.type == dep)
                {
                    if (after)
                    {
                        pos += sys.subSystemList.Length;
                        if (target.MinInsertPos < pos)
                            target.MinInsertPos = pos;
                        if (target.MaxInsertPos == 0 || target.MaxInsertPos > pos)
                            target.MaxInsertPos = pos;
                    }
                    else
                    {
                        if (target.MinInsertPos < pos)
                            target.MinInsertPos = pos;
                        if (target.MaxInsertPos == 0 || target.MaxInsertPos > pos)
                            target.MaxInsertPos = pos;
                    }

                    return;
                }

                var beginPos = pos;
                var endPos = pos + sys.subSystemList.Length;
                foreach (var subsys in sys.subSystemList)
                {
                    if (subsys.type == dep)
                    {
                        if (after)
                        {
                            ++pos;
                            if (target.MinInsertPos < pos)
                                target.MinInsertPos = pos;
                            if (target.MaxInsertPos == 0 || target.MaxInsertPos > endPos)
                                target.MaxInsertPos = endPos;
                        }
                        else
                        {
                            if (target.MinInsertPos < beginPos)
                                target.MinInsertPos = beginPos;
                            if (target.MaxInsertPos == 0 || target.MaxInsertPos > pos)
                                target.MaxInsertPos = pos;
                        }

                        return;
                    }

                    ++pos;
                }
            }

            // System was not found
        }

        private static void AddDependencies(DependantBehavior targetSystem,
            IReadOnlyDictionary<Type, DependantBehavior> dependencies,
            IReadOnlyDictionary<Type, ScriptBehaviourGroup> allGroups, PlayerLoopSystem defaultPlayerLoop)
        {
            var target = targetSystem.Manager.GetType();
            var attribs = target.GetCustomAttributes(typeof(UpdateAfterAttribute), true);
            foreach (var attr in attribs)
            {
                var attribDep = attr as UpdateAfterAttribute;
                DependantBehavior otherSystem;
                ScriptBehaviourGroup otherGroup;
                if (dependencies.TryGetValue(attribDep.SystemType, out otherSystem))
                {
                    targetSystem.UpdateAfter.Add(attribDep.SystemType);
                    otherSystem.UpdateBefore.Add(target);
                }
                else if (allGroups.TryGetValue(attribDep.SystemType, out otherGroup))
                {
                    otherGroup.AddUpdateBeforeToAllChildBehaviours(targetSystem, dependencies);
                }
                else
                {
                    UpdateInsertionPos(targetSystem, attribDep.SystemType, defaultPlayerLoop, true);
                }
            }

            attribs = target.GetCustomAttributes(typeof(UpdateBeforeAttribute), true);
            foreach (var attr in attribs)
            {
                var attribDep = attr as UpdateBeforeAttribute;
                DependantBehavior otherSystem;
                ScriptBehaviourGroup otherGroup;
                if (dependencies.TryGetValue(attribDep.SystemType, out otherSystem))
                {
                    targetSystem.UpdateBefore.Add(attribDep.SystemType);
                    otherSystem.UpdateAfter.Add(target);
                }
                else if (allGroups.TryGetValue(attribDep.SystemType, out otherGroup))
                {
                    otherGroup.AddUpdateAfterToAllChildBehaviours(targetSystem, dependencies);
                }
                else
                {
                    UpdateInsertionPos(targetSystem, attribDep.SystemType, defaultPlayerLoop, false);
                }
            }

            attribs = target.GetCustomAttributes(typeof(UpdateInGroupAttribute), true);
            foreach (var attr in attribs)
            {
                var attribDep = attr as UpdateInGroupAttribute;
                ScriptBehaviourGroup group;
                if (!allGroups.TryGetValue(attribDep.GroupType, out group))
                    continue;

                DependantBehavior otherSystem;
                ScriptBehaviourGroup otherGroup;
                foreach (var dep in group.UpdateAfter)
                    if (dependencies.TryGetValue(dep, out otherSystem))
                    {
                        targetSystem.UpdateAfter.Add(dep);
                        otherSystem.UpdateBefore.Add(target);
                    }
                    else if (allGroups.TryGetValue(dep, out otherGroup))
                    {
                        otherGroup.AddUpdateBeforeToAllChildBehaviours(targetSystem, dependencies);
                    }
                    else
                    {
                        UpdateInsertionPos(targetSystem, dep, defaultPlayerLoop, true);
                    }

                foreach (var dep in group.UpdateBefore)
                    if (dependencies.TryGetValue(dep, out otherSystem))
                    {
                        targetSystem.UpdateBefore.Add(dep);
                        otherSystem.UpdateAfter.Add(target);
                    }
                    else if (allGroups.TryGetValue(dep, out otherGroup))
                    {
                        otherGroup.AddUpdateAfterToAllChildBehaviours(targetSystem, dependencies);
                    }
                    else
                    {
                        UpdateInsertionPos(targetSystem, dep, defaultPlayerLoop, false);
                    }
            }
        }

        private static void CollectGroups(IEnumerable<ScriptBehaviourManager> activeManagers,
            out Dictionary<Type, ScriptBehaviourGroup> allGroups, out Dictionary<Type, DependantBehavior> dependencies)
        {
            allGroups = new Dictionary<Type, ScriptBehaviourGroup>();
            dependencies = new Dictionary<Type, DependantBehavior>();
            foreach (var manager in activeManagers)
            {
                var attribs = manager.GetType().GetCustomAttributes(typeof(UpdateInGroupAttribute), true);
                foreach (var attr in attribs)
                {
                    var grp = attr as UpdateInGroupAttribute;
                    ScriptBehaviourGroup groupData;
                    if (!allGroups.TryGetValue(grp.GroupType, out groupData))
                        groupData = new ScriptBehaviourGroup(grp.GroupType, allGroups);
                    groupData.Managers.Add(manager.GetType());
                }

                var dep = new DependantBehavior(manager);
                dependencies.Add(manager.GetType(), dep);
            }
        }

        private static Dictionary<Type, DependantBehavior> BuildSystemGraph(
            IEnumerable<ScriptBehaviourManager> activeManagers, PlayerLoopSystem defaultPlayerLoop)
        {
            // Collect all groups and create empty dependency data
            Dictionary<Type, ScriptBehaviourGroup> allGroups;
            Dictionary<Type, DependantBehavior> dependencies;
            CollectGroups(activeManagers, out allGroups, out dependencies);

            // @TODO: apply additional sideloaded constraints here

            // Apply the update before / after dependencies
            foreach (var manager in dependencies)
                // @TODO: need to deal with extracting dependencies for GenericProcessComponentSystem
                AddDependencies(manager.Value, dependencies, allGroups, defaultPlayerLoop);

            ValidateAndFixSystemGraph(dependencies);

            return dependencies;
        }

        private static void ValidateAndFixSystemGraph(Dictionary<Type, DependantBehavior> dependencyGraph)
        {
            // Check for simple over constraints on engine systems
            foreach (var typeAndSystem in dependencyGraph)
            {
                var system = typeAndSystem.Value;
                if (system.MinInsertPos > system.MaxInsertPos)
                {
                    Debug.LogError(
                        $"{system.Manager.GetType()} is over constrained with engine containts - ignoring dependencies");
                    system.MinInsertPos = system.MaxInsertPos = 0;
                }

                system.UnvalidatedSystemsUpdatingBefore = system.UpdateAfter.Count;
                system.LongestSystemsUpdatingBeforeChain = 0;
                system.LongestSystemsUpdatingAfterChain = 0;
            }

            // Check for circular dependencies, start with all systems updating first, mark all systems it updates after as having one more validated dep and start over
            var progress = true;
            while (progress)
            {
                progress = false;
                foreach (var typeAndSystem in dependencyGraph)
                {
                    var system = typeAndSystem.Value;
                    if (system.UnvalidatedSystemsUpdatingBefore != 0)
                        continue;

                    system.UnvalidatedSystemsUpdatingBefore = -1;
                    foreach (var nextInChain in system.UpdateBefore)
                    {
                        --dependencyGraph[nextInChain].UnvalidatedSystemsUpdatingBefore;
                        progress = true;
                    }
                }
            }

            // If some systems were found to have circular dependencies, drop all of them. This is a bit over aggressive - but it only happens on badly setup dependency chains
            foreach (var typeAndSystem in dependencyGraph)
            {
                var system = typeAndSystem.Value;
                if (system.UnvalidatedSystemsUpdatingBefore <= 0)
                    continue;

                Debug.LogError(
                    $"{system.Manager.GetType()} is in a chain of circular dependencies - ignoring dependencies");
                foreach (var after in system.UpdateAfter)
                    dependencyGraph[after].UpdateBefore.Remove(system.Manager.GetType());
                system.UpdateAfter.Clear();
            }

            // Validate that the chains are not over constrained with combinations of system and engine dependencies
            foreach (var typeAndSystem in dependencyGraph)
            {
                var system = typeAndSystem.Value;
                if (system.UpdateBefore.Count == 0)
                    ValidateAndFixSingleChainMaxPos(system, dependencyGraph, system.MaxInsertPos);
                if (system.UpdateAfter.Count == 0)
                    ValidateAndFixSingleChainMinPos(system, dependencyGraph, system.MinInsertPos);
            }
        }

        private static void ValidateAndFixSingleChainMinPos(DependantBehavior system,
            IReadOnlyDictionary<Type, DependantBehavior> dependencyGraph, int minInsertPos)
        {
            foreach (var nextInChain in system.UpdateBefore)
            {
                var nextSys = dependencyGraph[nextInChain];
                if (system.LongestSystemsUpdatingBeforeChain >= nextSys.LongestSystemsUpdatingBeforeChain)
                    nextSys.LongestSystemsUpdatingBeforeChain = system.LongestSystemsUpdatingBeforeChain + 1;
                if (nextSys.MinInsertPos < minInsertPos)
                    nextSys.MinInsertPos = minInsertPos;
                if (nextSys.MaxInsertPos > 0 && nextSys.MaxInsertPos < nextSys.MinInsertPos)
                {
                    Debug.LogError(
                        $"{nextInChain} is over constrained with engine and system containts - ignoring dependencies");
                    nextSys.MaxInsertPos = nextSys.MinInsertPos;
                }

                ValidateAndFixSingleChainMinPos(nextSys, dependencyGraph, nextSys.MinInsertPos);
            }
        }

        private static void ValidateAndFixSingleChainMaxPos(DependantBehavior system,
            Dictionary<Type, DependantBehavior> dependencyGraph, int maxInsertPos)
        {
            foreach (var prevInChain in system.UpdateAfter)
            {
                var prevSys = dependencyGraph[prevInChain];
                if (system.LongestSystemsUpdatingAfterChain >= prevSys.LongestSystemsUpdatingAfterChain)
                    prevSys.LongestSystemsUpdatingAfterChain = system.LongestSystemsUpdatingAfterChain + 1;
                if (prevSys.MaxInsertPos == 0 || prevSys.MaxInsertPos > maxInsertPos)
                    prevSys.MaxInsertPos = maxInsertPos;
                if (prevSys.MaxInsertPos > 0 && prevSys.MaxInsertPos < prevSys.MinInsertPos)
                {
                    Debug.LogError(
                        $"{prevInChain} is over constrained with engine and system containts - ignoring dependencies");
                    prevSys.MinInsertPos = prevSys.MaxInsertPos;
                }

                ValidateAndFixSingleChainMaxPos(prevSys, dependencyGraph, prevSys.MaxInsertPos);
            }
        }

        private static void MarkSchedulingAndWaitingJobs(Dictionary<Type, DependantBehavior> dependencyGraph)
        {
            // @TODO: sync rules for read-only
            var schedulers = new HashSet<DependantBehavior>();
            foreach (var systemKeyValue in dependencyGraph)
            {
                var system = systemKeyValue.Value;
                // @TODO: GenericProcessComponentSystem
                // @TODO: attribute
                if (!(system.Manager is JobComponentSystem))
                    continue;
                system.spawnsJobs = true;
                schedulers.Add(system);
            }

            foreach (var systemKeyValue in dependencyGraph)
            {
                var system = systemKeyValue.Value;
                // @TODO: attribute for sync
                if ((system.Manager as ComponentSystem)?.ComponentGroups == null)
                    continue;

                var waitComponent = new HashSet<int>();
                foreach (var componentGroup in ((ComponentSystem) system.Manager).ComponentGroups)
                foreach (var type in componentGroup.Types)
                    if (type.RequiresJobDependency)
                        waitComponent.Add(type.TypeIndex);
                foreach (var scheduler in schedulers)
                {
                    if (!(scheduler.Manager is ComponentSystem))
                        continue;
                    // Check if the component groups overlaps
                    var scheduleComponent = new HashSet<int>();
                    foreach (var componentGroup in ((ComponentSystem) scheduler.Manager).ComponentGroups)
                    foreach (var type in componentGroup.Types)
                        if (type.RequiresJobDependency)
                            scheduleComponent.Add(type.TypeIndex);
                    var overlap = false;
                    foreach (var waitComp in waitComponent)
                    {
                        if (!scheduleComponent.Contains(waitComp))
                            continue;

                        overlap = true;
                        break;
                    }

                    if (!overlap)
                        continue;

                    system.WaitsForJobs = true;
                    break;
                }
            }
        }

        private static PlayerLoopSystem InsertWorldManagersInPlayerLoop(PlayerLoopSystem defaultPlayerLoop,
            params World[] worlds)
        {
            var systemList = new List<InsertionBucket>();
            foreach (var world in worlds)
            {
                if (world.BehaviourManagers.Count() == 0)
                    continue;
                systemList.AddRange(CreateSystemDependencyList(world.BehaviourManagers, defaultPlayerLoop));
            }

            var ecsPlayerLoop = CreatePlayerLoop(systemList, defaultPlayerLoop);
            return ecsPlayerLoop;
        }

        internal static PlayerLoopSystem InsertManagersInPlayerLoop(IEnumerable<ScriptBehaviourManager> activeManagers,
            PlayerLoopSystem defaultPlayerLoop)
        {
            if (activeManagers.Count() == 0)
                return defaultPlayerLoop;

            var list = CreateSystemDependencyList(activeManagers, defaultPlayerLoop);
            return CreatePlayerLoop(list, defaultPlayerLoop);
        }

        private static List<InsertionBucket> CreateSystemDependencyList(
            IEnumerable<ScriptBehaviourManager> activeManagers, PlayerLoopSystem defaultPlayerLoop)
        {
            var dependencyGraph = BuildSystemGraph(activeManagers, defaultPlayerLoop);

            MarkSchedulingAndWaitingJobs(dependencyGraph);

            // Figure out which systems should be inserted early or late
            var earlyUpdates = new HashSet<DependantBehavior>();
            var normalUpdates = new HashSet<DependantBehavior>();
            var lateUpdates = new HashSet<DependantBehavior>();
            foreach (var dependency in dependencyGraph)
            {
                var system = dependency.Value;
                if (system.spawnsJobs)
                    earlyUpdates.Add(system);
                else if (system.WaitsForJobs)
                    lateUpdates.Add(system);
                else
                    normalUpdates.Add(system);
            }

            var depsToAdd = new List<DependantBehavior>();
            while (true)
            {
                foreach (var sys in earlyUpdates)
                foreach (var depType in sys.UpdateAfter)
                {
                    var depSys = dependencyGraph[depType];
                    if (normalUpdates.Remove(depSys) || lateUpdates.Remove(depSys))
                        depsToAdd.Add(depSys);
                }

                if (depsToAdd.Count == 0)
                    break;
                foreach (var dep in depsToAdd)
                    earlyUpdates.Add(dep);
                depsToAdd.Clear();
            }

            while (true)
            {
                foreach (var sys in lateUpdates)
                foreach (var depType in sys.UpdateBefore)
                {
                    var depSys = dependencyGraph[depType];
                    if (normalUpdates.Remove(depSys))
                        depsToAdd.Add(depSys);
                }

                if (depsToAdd.Count == 0)
                    break;
                foreach (var dep in depsToAdd)
                    lateUpdates.Add(dep);
                depsToAdd.Clear();
            }

            var defaultPos = 0;
            foreach (var sys in defaultPlayerLoop.subSystemList)
            {
                defaultPos += 1 + sys.subSystemList.Length;
                if (sys.type == typeof(Update))
                    break;
            }

            var insertionBucketDict = new Dictionary<int, InsertionBucket>();
            // increase the number of dependencies allowed by 1, starting from 0 and add all systems with that many at the first or last possible pos
            // bucket idx is insertion point << 2 | 0,1,2
            // When adding propagate min or max through the chain
            var processedChainLength = 0;
            while (earlyUpdates.Count > 0 || lateUpdates.Count > 0)
            {
                foreach (var sys in earlyUpdates)
                {
                    if (sys.LongestSystemsUpdatingBeforeChain != processedChainLength)
                        continue;

                    if (sys.MinInsertPos == 0)
                        sys.MinInsertPos = defaultPos;
                    sys.MaxInsertPos = sys.MinInsertPos;
                    depsToAdd.Add(sys);
                    foreach (var nextSys in sys.UpdateBefore)
                        if (dependencyGraph[nextSys].MinInsertPos < sys.MinInsertPos)
                            dependencyGraph[nextSys].MinInsertPos = sys.MinInsertPos;
                }

                foreach (var sys in lateUpdates)
                {
                    if (sys.LongestSystemsUpdatingAfterChain != processedChainLength)
                        continue;

                    if (sys.MaxInsertPos == 0)
                        sys.MaxInsertPos = defaultPos;
                    sys.MinInsertPos = sys.MaxInsertPos;
                    depsToAdd.Add(sys);
                    foreach (var prevSys in sys.UpdateAfter)
                        if (dependencyGraph[prevSys].MaxInsertPos == 0 ||
                            dependencyGraph[prevSys].MaxInsertPos > sys.MaxInsertPos)
                            dependencyGraph[prevSys].MaxInsertPos = sys.MaxInsertPos;
                }

                foreach (var sys in depsToAdd)
                {
                    earlyUpdates.Remove(sys);
                    var isLate = lateUpdates.Remove(sys);
                    var subIndex = isLate ? 2 : 0;

                    // Bucket to insert in is minPos == maxPos
                    var bucketIndex = (sys.MinInsertPos << 2) | subIndex;
                    InsertionBucket bucket;
                    if (!insertionBucketDict.TryGetValue(bucketIndex, out bucket))
                    {
                        bucket = new InsertionBucket
                        {
                            InsertPos = sys.MinInsertPos,
                            InsertSubPos = subIndex
                        };
                        insertionBucketDict.Add(bucketIndex, bucket);
                    }

                    bucket.Systems.Add(sys);
                }

                depsToAdd.Clear();
                ++processedChainLength;
            }

            processedChainLength = 0;
            while (normalUpdates.Count > 0)
            {
                foreach (var sys in normalUpdates)
                {
                    if (sys.LongestSystemsUpdatingBeforeChain != processedChainLength)
                        continue;

                    if (sys.MinInsertPos == 0)
                        sys.MinInsertPos = defaultPos;
                    sys.MaxInsertPos = sys.MinInsertPos;
                    depsToAdd.Add(sys);
                    foreach (var nextSys in sys.UpdateBefore)
                        if (dependencyGraph[nextSys].MinInsertPos < sys.MinInsertPos)
                            dependencyGraph[nextSys].MinInsertPos = sys.MinInsertPos;
                }

                foreach (var sys in depsToAdd)
                {
                    const int subIndex = 1;
                    normalUpdates.Remove(sys);

                    // Bucket to insert in is minPos == maxPos
                    var bucketIndex = (sys.MinInsertPos << 2) | subIndex;
                    InsertionBucket bucket;
                    if (!insertionBucketDict.TryGetValue(bucketIndex, out bucket))
                    {
                        bucket = new InsertionBucket();
                        bucket.InsertPos = sys.MinInsertPos;
                        bucket.InsertSubPos = subIndex;
                        insertionBucketDict.Add(bucketIndex, bucket);
                    }

                    bucket.Systems.Add(sys);
                }

                depsToAdd.Clear();
                ++processedChainLength;
            }

            return new List<InsertionBucket>(insertionBucketDict.Values);
        }

        private static PlayerLoopSystem CreatePlayerLoop(List<InsertionBucket> insertionBuckets,
            PlayerLoopSystem defaultPlayerLoop)
        {
            insertionBuckets.Sort();

            // Insert the buckets at the appropriate place
            var currentPos = 0;
            var ecsPlayerLoop = new PlayerLoopSystem
            {
                subSystemList = new PlayerLoopSystem[defaultPlayerLoop.subSystemList.Length]
            };
            var currentBucket = 0;
            for (var i = 0; i < defaultPlayerLoop.subSystemList.Length; ++i)
            {
                var firstPos = currentPos + 1;
                var lastPos = firstPos + defaultPlayerLoop.subSystemList[i].subSystemList.Length;
                // Find all new things to insert here
                var systemsToInsert = 0;
                foreach (var bucket in insertionBuckets)
                    if (bucket.InsertPos >= firstPos && bucket.InsertPos <= lastPos)
                        systemsToInsert += bucket.Systems.Count;
                ecsPlayerLoop.subSystemList[i] = defaultPlayerLoop.subSystemList[i];
                if (systemsToInsert > 0)
                {
                    ecsPlayerLoop.subSystemList[i].subSystemList =
                        new PlayerLoopSystem[defaultPlayerLoop.subSystemList[i].subSystemList.Length + systemsToInsert];
                    var dstPos = 0;
                    for (var srcPos = 0;
                        srcPos < defaultPlayerLoop.subSystemList[i].subSystemList.Length;
                        ++srcPos, ++dstPos)
                    {
                        while (currentBucket < insertionBuckets.Count &&
                               insertionBuckets[currentBucket].InsertPos <= firstPos + srcPos)
                        {
                            foreach (var insert in insertionBuckets[currentBucket].Systems)
                            {
                                ecsPlayerLoop.subSystemList[i].subSystemList[dstPos].type = insert.Manager.GetType();
                                var tmp = new DummyDelagateWrapper(insert.Manager);
                                ecsPlayerLoop.subSystemList[i].subSystemList[dstPos].updateDelegate = tmp.TriggerUpdate;
                                ++dstPos;
                            }
                            ++currentBucket;
                        }

                        ecsPlayerLoop.subSystemList[i].subSystemList[dstPos] =
                            defaultPlayerLoop.subSystemList[i].subSystemList[srcPos];
                    }

                    while (currentBucket < insertionBuckets.Count &&
                           insertionBuckets[currentBucket].InsertPos <= lastPos)
                    {
                        foreach (var insert in insertionBuckets[currentBucket].Systems)
                        {
                            ecsPlayerLoop.subSystemList[i].subSystemList[dstPos].type = insert.Manager.GetType();
                            var tmp = new DummyDelagateWrapper(insert.Manager);
                            ecsPlayerLoop.subSystemList[i].subSystemList[dstPos].updateDelegate = tmp.TriggerUpdate;
                            ++dstPos;
                        }

                        ++currentBucket;
                    }
                }

                currentPos = lastPos;
            }

            return ecsPlayerLoop;
        }

        public static void UpdatePlayerLoop(params World[] worlds)
        {
            var defaultLoop = PlayerLoop.GetDefaultPlayerLoop();

            if (worlds?.Length > 0)
            {
                var ecsLoop = InsertWorldManagersInPlayerLoop(defaultLoop, worlds.Where(x => x != null).ToArray());
                SetPlayerLoop(ecsLoop);
            }
            else
            {
                SetPlayerLoop(defaultLoop);
            }
        }

        public static PlayerLoopSystem CurrentPlayerLoop => currentPlayerLoop;
        private static PlayerLoopSystem currentPlayerLoop;

        private static void SetPlayerLoop(PlayerLoopSystem playerLoop)
        {
            PlayerLoop.SetPlayerLoop(playerLoop);
            currentPlayerLoop = playerLoop;
        }

        // FIXME: HACK! - mono 4.6 has problems invoking virtual methods as delegates from native, so wrap the invocation in a non-virtual class
        internal class DummyDelagateWrapper
        {

            internal ScriptBehaviourManager Manager => m_Manager;
            private readonly ScriptBehaviourManager m_Manager;

            public DummyDelagateWrapper(ScriptBehaviourManager man)
            {
                m_Manager = man;
            }

            public void TriggerUpdate()
            {
                m_Manager.Update();
            }
        }

        private class ScriptBehaviourGroup
        {
            private readonly List<ScriptBehaviourGroup> m_Groups = new List<ScriptBehaviourGroup>();
            public readonly List<Type> Managers = new List<Type>();
            public readonly HashSet<Type> UpdateAfter = new HashSet<Type>();
            public readonly HashSet<Type> UpdateBefore = new HashSet<Type>();

            private readonly Type m_GroupType;
            private readonly List<ScriptBehaviourGroup> m_Parents = new List<ScriptBehaviourGroup>();

            public ScriptBehaviourGroup(Type grpType, IDictionary<Type, ScriptBehaviourGroup> allGroups,
                HashSet<Type> circularCheck = null)
            {
                m_GroupType = grpType;

                var attribs = grpType.GetCustomAttributes(typeof(UpdateAfterAttribute), true);
                foreach (var attr in attribs)
                {
                    var attribDep = attr as UpdateAfterAttribute;
                    UpdateAfter.Add(attribDep.SystemType);
                }

                attribs = grpType.GetCustomAttributes(typeof(UpdateBeforeAttribute), true);
                foreach (var attr in attribs)
                {
                    var attribDep = attr as UpdateBeforeAttribute;
                    UpdateBefore.Add(attribDep.SystemType);
                }

                allGroups.Add(m_GroupType, this);

                attribs = m_GroupType.GetCustomAttributes(typeof(UpdateInGroupAttribute), true);
                foreach (var attr in attribs)
                {
                    if (circularCheck == null) circularCheck = new HashSet<Type> {m_GroupType};
                    var parentGrp = attr as UpdateInGroupAttribute;
                    if (!circularCheck.Add(parentGrp.GroupType))
                    {
                        // Found circular dependency
                        var msg = "Found circular chain in update groups involving: ";
                        var firstType = true;
                        foreach (var circularType in circularCheck)
                        {
                            msg += (firstType ? "" : ", ") + circularType;
                            firstType = false;
                        }

                        Debug.LogError(msg);
                    }

                    ScriptBehaviourGroup parentGroupData;
                    if (!allGroups.TryGetValue(parentGrp.GroupType, out parentGroupData))
                        parentGroupData = new ScriptBehaviourGroup(parentGrp.GroupType, allGroups, circularCheck);
                    circularCheck.Remove(parentGrp.GroupType);
                    parentGroupData.m_Groups.Add(this);
                    m_Parents.Add(parentGroupData);

                    foreach (var dep in parentGroupData.UpdateBefore)
                        UpdateBefore.Add(dep);
                    foreach (var dep in parentGroupData.UpdateAfter)
                        UpdateAfter.Add(dep);
                }
            }

            public void AddUpdateBeforeToAllChildBehaviours(DependantBehavior target,
                IReadOnlyDictionary<Type, DependantBehavior> dependencies)
            {
                var dep = target.Manager.GetType();
                foreach (var manager in Managers)
                {
                    DependantBehavior managerDep;
                    if (!dependencies.TryGetValue(manager, out managerDep))
                        continue;

                    target.UpdateAfter.Add(manager);
                    managerDep.UpdateBefore.Add(dep);
                }

                foreach (var group in m_Groups)
                    group.AddUpdateBeforeToAllChildBehaviours(target, dependencies);
            }

            public void AddUpdateAfterToAllChildBehaviours(DependantBehavior target,
                IReadOnlyDictionary<Type, DependantBehavior> dependencies)
            {
                var dep = target.Manager.GetType();
                foreach (var manager in Managers)
                {
                    DependantBehavior managerDep;
                    if (!dependencies.TryGetValue(manager, out managerDep))
                        continue;

                    target.UpdateBefore.Add(manager);
                    managerDep.UpdateAfter.Add(dep);
                }

                foreach (var group in m_Groups)
                    group.AddUpdateAfterToAllChildBehaviours(target, dependencies);
            }
        }

        private class DependantBehavior
        {
            public readonly ScriptBehaviourManager Manager;
            public readonly HashSet<Type> UpdateAfter = new HashSet<Type>();
            public readonly HashSet<Type> UpdateBefore = new HashSet<Type>();
            public int LongestSystemsUpdatingAfterChain;
            public int LongestSystemsUpdatingBeforeChain;
            public int MaxInsertPos;

            public int MinInsertPos;
            public bool spawnsJobs;

            public int UnvalidatedSystemsUpdatingBefore;
            public bool WaitsForJobs;

            public DependantBehavior(ScriptBehaviourManager man)
            {
                Manager = man;
                MinInsertPos = 0;
                MaxInsertPos = 0;
                spawnsJobs = false;
                WaitsForJobs = false;

                UnvalidatedSystemsUpdatingBefore = 0;
                LongestSystemsUpdatingBeforeChain = 0;
                LongestSystemsUpdatingAfterChain = 0;
            }
        }

        private class InsertionBucket : IComparable
        {
            public readonly List<DependantBehavior> Systems;
            public int InsertPos;
            public int InsertSubPos;

            public InsertionBucket()
            {
                InsertPos = 0;
                InsertSubPos = 0;
                Systems = new List<DependantBehavior>();
            }

            public int CompareTo(object other)
            {
                var otherBucket = other as InsertionBucket;
                if (InsertPos == otherBucket.InsertPos)
                    return InsertSubPos - otherBucket.InsertSubPos;
                return InsertPos - otherBucket.InsertPos;
            }
        }
    }
}
