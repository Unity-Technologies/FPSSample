using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

namespace Unity.Transforms
{
    /// <summary>
    /// Parent is added to child by system when Attached is resolved.
    /// Read-only from other systems.
    /// </summary>
    public struct Parent : ISystemStateComponentData
    {
        public Entity Value;
    }

    /// <summary>
    /// PendingFrozen is internal to the system and defines the pipeline stage
    /// between Static and Frozen. It allows for LocalToWorld to be updated once
    /// before it is Frozen.
    /// Not intended for use in other systems.
    /// </summary>
    struct PendingFrozen : ISystemStateComponentData
    {
    }

    /// <summary>
    /// Internal grouping of by graph depth for parent components. Transform hierarchy
    /// is processed bredth-first.
    /// Read-only from external systems.
    /// </summary>
    public struct Depth : ISystemStateSharedComponentData
    {
        public int Value;
    }

    /// <summary>
    /// LocalToParent is added by system when Attached is resolved for all children.
    /// Updated by system from Rotation +/- Position +/- Scale.
    /// Read-only from external systems.
    /// </summary>
    public struct LocalToParent : ISystemStateComponentData
    {
        public float4x4 Value;
    }

    /// <summary>
    /// Default TransformSystem pass. Transform components updated before EndFrameBarrier.
    /// </summary>
    [UnityEngine.ExecuteInEditMode]
    [UpdateBefore(typeof(EndFrameBarrier))]
    public class EndFrameTransformSystem : TransformSystem
    {
    }
        
    public abstract class TransformSystem : JobComponentSystem
    {
        uint LastSystemVersion = 0;
        
        // Internally tracked state of Parent->Child relationships.
        // Child->Parent relationship stored in Parent component.
        NativeMultiHashMap<Entity, Entity> ParentToChildTree;
        
        EntityArchetypeQuery NewRootQuery;
        EntityArchetypeQuery AttachQuery;
        EntityArchetypeQuery DetachQuery;
        EntityArchetypeQuery PendingFrozenQuery;
        EntityArchetypeQuery FrozenQuery;
        EntityArchetypeQuery RootLocalToWorldQuery;
        EntityArchetypeQuery InnerTreeLocalToParentQuery;
        EntityArchetypeQuery LeafLocalToParentQuery;
        EntityArchetypeQuery InnerTreeLocalToWorldQuery;
        EntityArchetypeQuery LeafLocalToWorldQuery;
        EntityArchetypeQuery DepthQuery;
        
        NativeArray<ArchetypeChunk> NewRootChunks;
        NativeArray<ArchetypeChunk> AttachChunks;
        NativeArray<ArchetypeChunk> DetachChunks;
        NativeArray<ArchetypeChunk> PendingFrozenChunks;
        NativeArray<ArchetypeChunk> FrozenChunks;
        NativeArray<ArchetypeChunk> RootLocalToWorldChunks;
        NativeArray<ArchetypeChunk> InnerTreeLocalToParentChunks;
        NativeArray<ArchetypeChunk> LeafLocalToParentChunks;
        NativeArray<ArchetypeChunk> InnerTreeLocalToWorldChunks;
        NativeArray<ArchetypeChunk> LeafLocalToWorldChunks;
        NativeArray<ArchetypeChunk> DepthChunks;

        ComponentDataFromEntity<LocalToWorld> LocalToWorldFromEntityRW;
        ComponentDataFromEntity<Parent> ParentFromEntityRO;
        ComponentDataFromEntity<Parent> ParentFromEntityRW;
        ArchetypeChunkEntityType EntityTypeRO;
        ArchetypeChunkComponentType<LocalToWorld> LocalToWorldTypeRW;
        ArchetypeChunkComponentType<Parent> ParentTypeRO;
        ArchetypeChunkComponentType<LocalToParent> LocalToParentTypeRO;
        ArchetypeChunkComponentType<LocalToParent> LocalToParentTypeRW;
        ArchetypeChunkComponentType<Scale> ScaleTypeRO;
        ArchetypeChunkComponentType<Rotation> RotationTypeRO;
        ArchetypeChunkComponentType<Position> PositionTypeRO;
        ArchetypeChunkComponentType<Attach> AttachTypeRO;
        ArchetypeChunkComponentType<Frozen> FrozenTypeRO;
        ArchetypeChunkComponentType<PendingFrozen> PendingFrozenTypeRO;
        ArchetypeChunkSharedComponentType<Depth> DepthTypeRO;

        protected override void OnCreateManager(int capacity)
        {
            ParentToChildTree = new NativeMultiHashMap<Entity, Entity>(1024, Allocator.Persistent);
            GatherQueries();
        }
        
        protected override void OnDestroyManager()
        {
            ParentToChildTree.Dispose();
        }

        bool IsChildTree(Entity entity)
        {
            NativeMultiHashMapIterator<Entity> it;
            Entity foundChild;
            return ParentToChildTree.TryGetFirstValue(entity, out foundChild, out it);
        }

        void AddChildTree(Entity parentEntity, Entity childEntity)
        {
            ParentToChildTree.Add(parentEntity, childEntity);
        }

        void RemoveChildTree(Entity parentEntity, Entity childEntity)
        {
            NativeMultiHashMapIterator<Entity> it;
            Entity foundChild;
            if (!ParentToChildTree.TryGetFirstValue(parentEntity, out foundChild, out it))
            {
                return;
            }

            do
            {
                if (foundChild == childEntity)
                {
                    ParentToChildTree.Remove(it);
                    return;
                }
            } while (ParentToChildTree.TryGetNextValue(out foundChild, ref it));

            throw new System.InvalidOperationException(string.Format("Parent not found in Hierarchy hashmap"));
        }

        void UpdateNewRootTransforms(EntityCommandBuffer entityCommandBuffer)
        {
            if (NewRootChunks.Length == 0)
            {
                NewRootChunks.Dispose();
                return;
            }

            for (int chunkIndex = 0; chunkIndex < NewRootChunks.Length; chunkIndex++)
            {
                var chunk = NewRootChunks[chunkIndex];
                var parentCount = chunk.Count;

                var chunkEntities = chunk.GetNativeArray(EntityTypeRO);

                for (int i = 0; i < parentCount; i++)
                {
                    var entity = chunkEntities[i];

                    entityCommandBuffer.AddComponent(entity, new LocalToWorld {Value = float4x4.identity});
                }
            }
            
            NewRootChunks.Dispose();
        }

        bool UpdateAttach(EntityCommandBuffer entityCommandBuffer)
        {
            if (AttachChunks.Length == 0)
            {
                AttachChunks.Dispose();
                return false;
            }

            for (int chunkIndex = 0; chunkIndex < AttachChunks.Length; chunkIndex++)
            {
                var chunk = AttachChunks[chunkIndex];
                var parentCount = chunk.Count;
                var entities = chunk.GetNativeArray(EntityTypeRO);
                var attaches = chunk.GetNativeArray(AttachTypeRO);

                for (int i = 0; i < parentCount; i++)
                {
                    var parentEntity = attaches[i].Parent;
                    var childEntity = attaches[i].Child;

                    // Does the child have a previous parent?
                    if (EntityManager.HasComponent<Parent>(childEntity))
                    {
                        var previousParent = ParentFromEntityRW[childEntity];
                        var previousParentEntity = previousParent.Value;

                        if (IsChildTree(previousParentEntity))
                        {
                            RemoveChildTree(previousParentEntity, childEntity);
                            if (!IsChildTree(previousParentEntity))
                            {
                                entityCommandBuffer.RemoveComponent<Depth>(previousParentEntity);
                            }
                        }

                        ParentFromEntityRW[childEntity] = new Parent {Value = parentEntity};
                    }
                    else
                    {
                        entityCommandBuffer.AddComponent(childEntity, new Parent {Value = parentEntity});
                        entityCommandBuffer.AddComponent(childEntity, new Attached());
                        entityCommandBuffer.AddComponent(childEntity, new LocalToParent {Value = float4x4.identity});
                    }

                    // parent wasn't previously a tree, so doesn't have depth
                    if (!IsChildTree(parentEntity))
                    {
                        entityCommandBuffer.AddSharedComponent(parentEntity, new Depth {Value = 0});
                    }

                    AddChildTree(parentEntity, childEntity);
                    
                    entityCommandBuffer.DestroyEntity(entities[i]);
                }
            }

            AttachChunks.Dispose();
            return true;
        }

        bool UpdateDetach(EntityCommandBuffer entityCommandBuffer)
        {
            if (DetachChunks.Length == 0)
            {
                DetachChunks.Dispose();
                return false;
            }

            for (int chunkIndex = 0; chunkIndex < DetachChunks.Length; chunkIndex++)
            {
                var chunk = DetachChunks[chunkIndex];

                var parentCount = chunk.Count;
                var chunkEntities = chunk.GetNativeArray(EntityTypeRO);
                var parents = chunk.GetNativeArray(ParentTypeRO);

                for (int i = 0; i < parentCount; i++)
                {
                    var entity = chunkEntities[i];
                    var parentEntity = parents[i].Value;

                    if (IsChildTree(parentEntity))
                    {
                        RemoveChildTree(parentEntity, entity);

                        if (!IsChildTree(parentEntity))
                        {
                            entityCommandBuffer.RemoveComponent<Depth>(parentEntity);
                        }
                    }

                    entityCommandBuffer.RemoveComponent<LocalToParent>(entity);
                    entityCommandBuffer.RemoveComponent<Parent>(entity);
                }
            }

            DetachChunks.Dispose();
            return true;
        }

        void UpdatePendingFrozen()
        {
            if (PendingFrozenChunks.Length == 0)
            {
                PendingFrozenChunks.Dispose();
                return;
            }
            
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            for (int chunkIndex = 0; chunkIndex < PendingFrozenChunks.Length; chunkIndex++)
            {
                var chunk = PendingFrozenChunks[chunkIndex];
                var parentCount = chunk.Count;

                var chunkEntities = chunk.GetNativeArray(EntityTypeRO);

                for (int i = 0; i < parentCount; i++)
                {
                    var entity = chunkEntities[i];

                    entityCommandBuffer.RemoveComponent<PendingFrozen>(entity);
                    entityCommandBuffer.AddComponent(entity, default(Frozen));
                }
            }

            PendingFrozenChunks.Dispose();

            entityCommandBuffer.Playback(EntityManager);
            entityCommandBuffer.Dispose();
            
        }

        void UpdateFrozen()
        {
            if (FrozenChunks.Length == 0)
            {
                FrozenChunks.Dispose();
                return;
            }
            
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            for (int chunkIndex = 0; chunkIndex < FrozenChunks.Length; chunkIndex++)
            {
                var chunk = FrozenChunks[chunkIndex];
                var parentCount = chunk.Count;

                var chunkEntities = chunk.GetNativeArray(EntityTypeRO);

                for (int i = 0; i < parentCount; i++)
                {
                    var entity = chunkEntities[i];

                    entityCommandBuffer.AddComponent(entity, default(PendingFrozen));
                }
            }

            FrozenChunks.Dispose();

            entityCommandBuffer.Playback(EntityManager);
            entityCommandBuffer.Dispose();
        }
        
        private static readonly ProfilerMarker k_ProfileUpdateNewRootTransforms = new ProfilerMarker("UpdateNewRootTransforms");
        private static readonly ProfilerMarker k_ProfileUpdateDAGAttachDetach = new ProfilerMarker("UpdateDAG.AttachDetach");
        private static readonly ProfilerMarker k_ProfileUpdateDAGPlayback = new ProfilerMarker("UpdateDAG.Playback");
        bool UpdateDAG()
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            k_ProfileUpdateNewRootTransforms.Begin();
            UpdateNewRootTransforms(entityCommandBuffer);
            k_ProfileUpdateNewRootTransforms.End();
            
            k_ProfileUpdateDAGAttachDetach.Begin();
            bool changedAttached = UpdateAttach(entityCommandBuffer);
            bool changedDetached = UpdateDetach(entityCommandBuffer);
            k_ProfileUpdateDAGAttachDetach.End();
            
            k_ProfileUpdateDAGPlayback.Begin();
            entityCommandBuffer.Playback(EntityManager);
            entityCommandBuffer.Dispose();
            k_ProfileUpdateDAGPlayback.End();

            return changedAttached || changedDetached;
        }

        [BurstCompile]
        struct RootLocalToWorld : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            [ReadOnly] public ArchetypeChunkComponentType<Rotation> rotationType;
            [ReadOnly] public ArchetypeChunkComponentType<Position> positionType;
            [ReadOnly] public ArchetypeChunkComponentType<Scale> scaleType;
            public ArchetypeChunkComponentType<LocalToWorld> localToWorldType;
            public uint lastSystemVersion;

            public void Execute(int chunkIndex)
            {
                var chunk = chunks[chunkIndex];
                var parentCount = chunk.Count;

                var chunkRotations = chunk.GetNativeArray(rotationType);
                var chunkPositions = chunk.GetNativeArray(positionType);
                var chunkScales = chunk.GetNativeArray(scaleType);
                var chunkLocalToWorlds = chunk.GetNativeArray(localToWorldType);

                var chunkRotationsChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(rotationType), lastSystemVersion);
                var chunkPositionsChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(positionType), lastSystemVersion);
                var chunkScalesChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(scaleType), lastSystemVersion);
                var chunkAnyChanged = chunkRotationsChanged || chunkPositionsChanged || chunkScalesChanged;

                if (!chunkAnyChanged)
                  return;

                var chunkRotationsExist = chunkRotations.Length > 0;
                var chunkPositionsExist = chunkPositions.Length > 0;
                var chunkScalesExist = chunkScales.Length > 0;

                // 001
                if ((!chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToWorlds[i] = new LocalToWorld
                        {
                            Value = float4x4.scale(chunkScales[i].Value)
                        };
                    }
                }
                // 010
                else if ((!chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToWorlds[i] = new LocalToWorld
                        {
                            Value = new float4x4(chunkRotations[i].Value, new float3())
                        };
                    }
                }
                // 011
                else if ((!chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToWorlds[i] = new LocalToWorld
                        {
                            Value = math.mul(new float4x4(chunkRotations[i].Value, new float3()),
                                float4x4.scale(chunkScales[i].Value))
                        };
                    }
                }
                // 100
                else if ((chunkPositionsExist) && (!chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToWorlds[i] = new LocalToWorld
                        {
                            Value = float4x4.translate(chunkPositions[i].Value)
                        };
                    }
                }
                // 101
                else if ((chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToWorlds[i] = new LocalToWorld
                        {
                            Value = math.mul(float4x4.translate(chunkPositions[i].Value),
                                float4x4.scale(chunkScales[i].Value))
                        };
                    }
                }
                // 110
                else if ((chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToWorlds[i] = new LocalToWorld
                        {
                            Value = new float4x4(chunkRotations[i].Value, chunkPositions[i].Value)
                        };
                    }
                }
                // 111
                else if ((chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToWorlds[i] = new LocalToWorld
                        {
                            Value = math.mul(new float4x4(chunkRotations[i].Value, chunkPositions[i].Value),
                                float4x4.scale(chunkScales[i].Value))
                        };
                    }
                }
            }
        }

        JobHandle UpdateRootLocalToWorld(JobHandle inputDeps)
        {
            if (RootLocalToWorldChunks.Length == 0)
            {
                RootLocalToWorldChunks.Dispose();
                return inputDeps;
            }

            var rootsLocalToWorldJob = new RootLocalToWorld
            {
                chunks = RootLocalToWorldChunks,
                rotationType = RotationTypeRO,
                positionType = PositionTypeRO,
                scaleType = ScaleTypeRO,
                localToWorldType = LocalToWorldTypeRW,
                lastSystemVersion = LastSystemVersion,
            };
            var rootsLocalToWorldJobHandle = rootsLocalToWorldJob.Schedule(RootLocalToWorldChunks.Length, 4, inputDeps);
            return rootsLocalToWorldJobHandle;
        }

        [BurstCompile]
        struct InnerTreeLocalToParent : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            [ReadOnly] public ArchetypeChunkComponentType<Rotation> rotationType;
            [ReadOnly] public ArchetypeChunkComponentType<Position> positionType;
            [ReadOnly] public ArchetypeChunkComponentType<Scale> scaleType;
            public ArchetypeChunkComponentType<LocalToParent> localToParentType;
            public uint lastSystemVersion;

            public void Execute(int chunkIndex)
            {
                var chunk = chunks[chunkIndex];
                var parentCount = chunk.Count;

                var chunkRotations = chunk.GetNativeArray(rotationType);
                var chunkPositions = chunk.GetNativeArray(positionType);
                var chunkScales = chunk.GetNativeArray(scaleType);
                var chunkLocalToParents = chunk.GetNativeArray(localToParentType);

                var chunkRotationsChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(rotationType), lastSystemVersion);
                var chunkPositionsChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(positionType), lastSystemVersion);
                var chunkScalesChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(scaleType), lastSystemVersion);
                var chunkAnyChanged = chunkRotationsChanged || chunkPositionsChanged || chunkScalesChanged;

                if (!chunkAnyChanged)
                  return;

                var chunkRotationsExist = chunkRotations.Length > 0;
                var chunkPositionsExist = chunkPositions.Length > 0;
                var chunkScalesExist = chunkScales.Length > 0;

                // 001
                if ((!chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = float4x4.scale(chunkScales[i].Value)
                        };
                    }
                }
                // 010
                else if ((!chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = new float4x4(chunkRotations[i].Value, new float3())
                        };
                    }
                }
                // 011
                else if ((!chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = math.mul(new float4x4(chunkRotations[i].Value, new float3()),
                                float4x4.scale(chunkScales[i].Value))
                        };
                    }
                }
                // 100
                else if ((chunkPositionsExist) && (!chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = float4x4.translate(chunkPositions[i].Value)
                        };
                    }
                }
                // 101
                else if ((chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = math.mul(float4x4.translate(chunkPositions[i].Value),
                                float4x4.scale(chunkScales[i].Value))
                        };
                    }
                }
                // 110
                else if ((chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = new float4x4(chunkRotations[i].Value, chunkPositions[i].Value)
                        };
                    }
                }
                // 111
                else if ((chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = math.mul(new float4x4(chunkRotations[i].Value, chunkPositions[i].Value),
                                float4x4.scale(chunkScales[i].Value))
                        };
                    }
                }
            }
        }

        JobHandle UpdateInnerTreeLocalToParent(JobHandle inputDeps)
        {
            if (InnerTreeLocalToParentChunks.Length == 0)
            {
                InnerTreeLocalToParentChunks.Dispose();
                return inputDeps;
            }

            var innerTreeLocalToParentJob = new InnerTreeLocalToParent
            {
                chunks = InnerTreeLocalToParentChunks,
                rotationType = RotationTypeRO,
                positionType = PositionTypeRO,
                scaleType = ScaleTypeRO,
                localToParentType = LocalToParentTypeRW,
                lastSystemVersion = LastSystemVersion
            };
            var innerTreeLocalToParentJobHandle = innerTreeLocalToParentJob.Schedule(InnerTreeLocalToParentChunks.Length, 4, inputDeps);
            return innerTreeLocalToParentJobHandle;
        }

        [BurstCompile]
        struct LeafLocalToParent : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            [ReadOnly] public ArchetypeChunkComponentType<Rotation> rotationType;
            [ReadOnly] public ArchetypeChunkComponentType<Position> positionType;
            [ReadOnly] public ArchetypeChunkComponentType<Scale> scaleType;
            public ArchetypeChunkComponentType<LocalToParent> localToParentType;
            public uint lastSystemVersion;

            public void Execute(int chunkIndex)
            {
                var chunk = chunks[chunkIndex];
                var parentCount = chunk.Count;

                var chunkRotations = chunk.GetNativeArray(rotationType);
                var chunkPositions = chunk.GetNativeArray(positionType);
                var chunkScales = chunk.GetNativeArray(scaleType);
                var chunkLocalToParents = chunk.GetNativeArray(localToParentType);

                var chunkRotationsChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(rotationType), lastSystemVersion);
                var chunkPositionsChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(positionType), lastSystemVersion);
                var chunkScalesChanged = ChangeVersionUtility.DidAddOrChange(chunk.GetComponentVersion(scaleType), lastSystemVersion);
                var chunkAnyChanged = chunkRotationsChanged || chunkPositionsChanged || chunkScalesChanged;

                if (!chunkAnyChanged)
                  return;

                var chunkRotationsExist = chunkRotations.Length > 0;
                var chunkPositionsExist = chunkPositions.Length > 0;
                var chunkScalesExist = chunkScales.Length > 0;

                // 001
                if ((!chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = float4x4.scale(chunkScales[i].Value)
                        };
                    }
                }
                // 010
                else if ((!chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = new float4x4(chunkRotations[i].Value, new float3())
                        };
                    }
                }
                // 011
                else if ((!chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = math.mul(new float4x4(chunkRotations[i].Value, new float3()),
                                float4x4.scale(chunkScales[i].Value))
                        };
                    }
                }
                // 100
                else if ((chunkPositionsExist) && (!chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = float4x4.translate(chunkPositions[i].Value)
                        };
                    }
                }
                // 101
                else if ((chunkPositionsExist) && (!chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = math.mul(float4x4.translate(chunkPositions[i].Value),
                                float4x4.scale(chunkScales[i].Value))
                        };
                    }
                }
                // 110
                else if ((chunkPositionsExist) && (chunkRotationsExist) && (!chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = new float4x4(chunkRotations[i].Value, chunkPositions[i].Value)
                        };
                    }
                }
                // 111
                else if ((chunkPositionsExist) && (chunkRotationsExist) && (chunkScalesExist))
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        chunkLocalToParents[i] = new LocalToParent
                        {
                            Value = math.mul(new float4x4(chunkRotations[i].Value, chunkPositions[i].Value),
                                float4x4.scale(chunkScales[i].Value))
                        };
                    }
                }
            }
        }

        JobHandle UpdateLeafLocalToParent(JobHandle inputDeps)
        {
            if (LeafLocalToParentChunks.Length == 0)
            {
                LeafLocalToParentChunks.Dispose();
                return inputDeps;
            }

            var leafToLocalParentJob = new LeafLocalToParent
            {
                chunks = LeafLocalToParentChunks,
                rotationType = RotationTypeRO,
                positionType = PositionTypeRO,
                scaleType = ScaleTypeRO,
                localToParentType = LocalToParentTypeRW,
                lastSystemVersion = LastSystemVersion
            };
            var leafToLocalParentJobHandle = leafToLocalParentJob.Schedule(LeafLocalToParentChunks.Length, 4, inputDeps);
            return leafToLocalParentJobHandle;
        }

        [BurstCompile]
        struct InnerTreeLocalToWorld : IJob
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<int> chunkIndices;

            [NativeDisableParallelForRestriction] [DeallocateOnJobCompletion] [ReadOnly]
            public NativeArray<ArchetypeChunk> chunks;

            [ReadOnly] public ArchetypeChunkComponentType<Parent> parentType;
            [ReadOnly] public ArchetypeChunkEntityType entityType;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToParent> localToParentType;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<LocalToWorld> localToWorldFromEntity;
            public uint lastSystemVersion;

            public void Execute()
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    var chunkIndex = chunkIndices[i];
                    var chunk = chunks[chunkIndex];
                    var chunkLocalToParents = chunk.GetNativeArray(localToParentType);

                    var chunkParents = chunk.GetNativeArray(parentType);
                    var chunkEntities = chunk.GetNativeArray(entityType);
                    var previousParentEntity = Entity.Null;
                    var parentLocalToWorldMatrix = new float4x4();

                    for (int j = 0; j < chunk.Count; j++)
                    {
                        var parentEntity = chunkParents[j].Value;
                        if (parentEntity != previousParentEntity)
                        {
                            parentLocalToWorldMatrix = localToWorldFromEntity[parentEntity].Value;
                            previousParentEntity = parentEntity;
                        }

                        var entity = chunkEntities[j];
                        localToWorldFromEntity[entity] = new LocalToWorld
                        {
                            Value = math.mul(parentLocalToWorldMatrix, chunkLocalToParents[j].Value)
                        };
                    }
                }
            }
        }

        [BurstCompile]
        struct SortDepths : IJob
        {
            [DeallocateOnJobCompletion] [NativeDisableParallelForRestriction] [ReadOnly]
            public NativeArray<int> depths;

            [NativeDisableParallelForRestriction] [ReadOnly]
            public NativeArray<ArchetypeChunk> chunks;

            [ReadOnly] public ArchetypeChunkSharedComponentType<Depth> depthType;
            public int maxDepth;

            public NativeArray<int> chunkIndices;

            public void Execute()
            {
                // Slow and dirty sort inner tree by depth
                var chunkIndex = 0;
                
                for (int depth = -1; depth <= maxDepth; depth++)
                {
                    for (int i = 0; i < chunks.Length; i++)
                    {
                        var chunk = chunks[i];
                        var chunkDepthSharedIndex = chunk.GetSharedComponentIndex(depthType);
                        var chunkDepth = -1;
                        
                        // -1 = Depth has been removed, but still matching archetype for some reason. #todo
                        if (chunkDepthSharedIndex != -1)
                        {
                            chunkDepth = depths[chunkDepthSharedIndex];
                        }

                        if (chunkDepth == depth)
                        {
                            chunkIndices[chunkIndex] = i;
                            chunkIndex++;
                        }
                    }
                }
            }
        }

        JobHandle UpdateInnerTreeLocalToWorld(JobHandle inputDeps)
        {
            if (InnerTreeLocalToWorldChunks.Length == 0)
            {
                InnerTreeLocalToWorldChunks.Dispose();
                return inputDeps;
            }
            
            var sharedDepths = new List<Depth>();
            var sharedDepthIndices = new List<int>();

            var sharedComponentCount = EntityManager.GetSharedComponentCount();

            EntityManager.GetAllUniqueSharedComponentData(sharedDepths, sharedDepthIndices);

            var depthCount = sharedDepths.Count;
            var depths = new NativeArray<int>(sharedComponentCount, Allocator.TempJob);
            var maxDepth = 0;
            
            for (int i = 0; i < depthCount; i++)
            {
                var index = sharedDepthIndices[i];
                var depth = sharedDepths[i].Value;
                if (depth > maxDepth)
                {
                    maxDepth = depth;
                }

                depths[index] = depth;
            }
            
            var chunkIndices = new NativeArray<int>(InnerTreeLocalToWorldChunks.Length, Allocator.TempJob);
            var sortDepthsJob = new SortDepths
            {
                depths = depths,
                chunks = InnerTreeLocalToWorldChunks,
                depthType = DepthTypeRO,
                maxDepth = maxDepth,
                chunkIndices = chunkIndices
            };
            var sortDepthsJobHandle = sortDepthsJob.Schedule(inputDeps);

            var innerTreeLocalToWorldJob = new InnerTreeLocalToWorld
            {
                chunkIndices = chunkIndices,
                chunks = InnerTreeLocalToWorldChunks,
                parentType = ParentTypeRO,
                entityType = EntityTypeRO,
                localToParentType = LocalToParentTypeRO,
                localToWorldFromEntity = LocalToWorldFromEntityRW,
                lastSystemVersion = LastSystemVersion
            };
            var innerTreeLocalToWorldJobHandle = innerTreeLocalToWorldJob.Schedule(sortDepthsJobHandle);
            
            return innerTreeLocalToWorldJobHandle;
        }

        [BurstCompile]
        struct LeafLocalToWorld : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            [ReadOnly] public ArchetypeChunkEntityType entityType;
            [ReadOnly] public ArchetypeChunkComponentType<Parent> parentType;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToParent> localToParentType;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<LocalToWorld> localToWorldFromEntity;
            public uint lastSystemVersion;

            public void Execute(int i)
            {
                var chunk = chunks[i];
                var chunkLocalToParents = chunk.GetNativeArray(localToParentType);
                var chunkEntities = chunk.GetNativeArray(entityType);
                var chunkParents = chunk.GetNativeArray(parentType);
                var previousParentEntity = Entity.Null;
                var parentLocalToWorldMatrix = new float4x4();

                for (int j = 0; j < chunk.Count; j++)
                {
                    var parentEntity = chunkParents[j].Value;
                    if (parentEntity != previousParentEntity)
                    {
                        parentLocalToWorldMatrix = localToWorldFromEntity[parentEntity].Value;
                        previousParentEntity     = parentEntity;
                    }

                    var entity = chunkEntities[j];
                    localToWorldFromEntity[entity] = new LocalToWorld
                    {
                        Value = math.mul(parentLocalToWorldMatrix, chunkLocalToParents[j].Value)
                    };
                }
            }
        }

        JobHandle UpdateLeafLocalToWorld(JobHandle inputDeps)
        {
            if (LeafLocalToWorldChunks.Length == 0)
            {
                LeafLocalToWorldChunks.Dispose();
                return inputDeps;
            }

            var updateLeafLocalToWorldJob = new LeafLocalToWorld
            {
                chunks = LeafLocalToWorldChunks,
                entityType = EntityTypeRO,
                parentType = ParentTypeRO,
                localToParentType = LocalToParentTypeRO,
                localToWorldFromEntity = LocalToWorldFromEntityRW,
                lastSystemVersion = LastSystemVersion
            };
            var updateLeafToWorldJobHandle = updateLeafLocalToWorldJob.Schedule(LeafLocalToWorldChunks.Length, 4, inputDeps);
            return updateLeafToWorldJobHandle;
        }

        int ParentCount(Entity entity)
        {
            if (!EntityManager.HasComponent<Parent>(entity))
            {
                return 0;
            }

            return 1 + ParentCount(ParentFromEntityRO[entity].Value);
        }

        private static readonly ProfilerMarker k_ProfileUpdateDepthChunks = new ProfilerMarker("UpdateDepth.Chunks");
        private static readonly ProfilerMarker k_ProfileUpdateDepthPlayback = new ProfilerMarker("UpdateDepth.Playback");
        
        void UpdateDepth()
        {
            if (DepthChunks.Length == 0)
            {
                DepthChunks.Dispose();
                return;
            }
            
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            k_ProfileUpdateDepthChunks.Begin();
            for (int i = 0; i < DepthChunks.Length; i++)
            {
                var chunk = DepthChunks[i];
                var entityCount = chunk.Count;
                var parents = chunk.GetNativeArray(ParentTypeRO);
                var entities = chunk.GetNativeArray(EntityTypeRO);
                for (int j = 0; j < entityCount; j++)
                {
                    var entity = entities[j];
                    var parentEntity = parents[j].Value;
                    var parentCount = 1 + ParentCount(parentEntity);
                    entityCommandBuffer.SetSharedComponent(entity, new Depth { Value = parentCount });
                }
            }
            k_ProfileUpdateDepthChunks.End();
            
            k_ProfileUpdateDepthPlayback.Begin();
            entityCommandBuffer.Playback(EntityManager);
            entityCommandBuffer.Dispose();
            k_ProfileUpdateDepthPlayback.End();
            
            DepthChunks.Dispose();
        }
        
        void GatherQueries()
        {
            NewRootQuery = new EntityArchetypeQuery
            {
                Any = new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)}, 
                None = new ComponentType[] {typeof(Frozen), typeof(Parent), typeof(LocalToWorld), typeof(Depth)},
                All = Array.Empty<ComponentType>(),
            };
            AttachQuery = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(), 
                None = Array.Empty<ComponentType>(),
                All = new ComponentType[] {typeof(Attach)},
            };
            DetachQuery = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = new ComponentType[] {typeof(Attached)},
                All = new ComponentType[] {typeof(Parent)},
            };
            PendingFrozenQuery = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = new ComponentType[] {typeof(Frozen)},
                All = new ComponentType[] {typeof(LocalToWorld), typeof(Static),typeof(PendingFrozen)},
            };
            FrozenQuery = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = new ComponentType[] {typeof(PendingFrozen), typeof(Frozen)},
                All = new ComponentType[] {typeof(LocalToWorld), typeof(Static)},
            };
            RootLocalToWorldQuery = new EntityArchetypeQuery
            {
                Any = new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)},
                None = new ComponentType[] {typeof(Frozen), typeof(Parent)},
                All = new ComponentType[] {typeof(LocalToWorld)},
            };
            InnerTreeLocalToParentQuery = new EntityArchetypeQuery
            {
                Any = new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)},
                None = new ComponentType[] {typeof(Frozen)},
                All = new ComponentType[] {typeof(LocalToParent), typeof(Parent) },
            };
            LeafLocalToParentQuery = new EntityArchetypeQuery
            {
                Any = new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)},
                None = new ComponentType[] {typeof(Frozen)},
                All = new ComponentType[] {typeof(LocalToParent), typeof(Parent)},
            };
            InnerTreeLocalToWorldQuery = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = new ComponentType[] {typeof(Frozen)},
                All = new ComponentType[] {typeof(Depth), typeof(LocalToParent), typeof(Parent), typeof(LocalToWorld)},
            };
            LeafLocalToWorldQuery = new EntityArchetypeQuery
            {
                Any = new ComponentType[] {typeof(Rotation), typeof(Position), typeof(Scale)},
                None = new ComponentType[] {typeof(Frozen), typeof(Depth)},
                All = new ComponentType[] {typeof(LocalToParent), typeof(Parent)},
            };
            DepthQuery = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>(),
                All = new ComponentType[] {typeof(Depth), typeof(Parent)},
            };
        }
        
        void GatherFrozenChunks()
        {
            PendingFrozenChunks = EntityManager.CreateArchetypeChunkArray(PendingFrozenQuery, Allocator.TempJob);
            FrozenChunks = EntityManager.CreateArchetypeChunkArray(FrozenQuery, Allocator.TempJob);
        }
        
        void GatherDAGChunks()
        {
            NewRootChunks = EntityManager.CreateArchetypeChunkArray(NewRootQuery, Allocator.TempJob);
            AttachChunks = EntityManager.CreateArchetypeChunkArray(AttachQuery, Allocator.TempJob);
            DetachChunks = EntityManager.CreateArchetypeChunkArray(DetachQuery, Allocator.TempJob);
        }

        void GatherDepthChunks()
        {
            DepthChunks = EntityManager.CreateArchetypeChunkArray(DepthQuery, Allocator.TempJob);
        }

        void GatherUpdateChunks()
        {
            RootLocalToWorldChunks = EntityManager.CreateArchetypeChunkArray(RootLocalToWorldQuery, Allocator.TempJob);
            InnerTreeLocalToParentChunks = EntityManager.CreateArchetypeChunkArray(InnerTreeLocalToParentQuery, Allocator.TempJob);
            LeafLocalToParentChunks = EntityManager.CreateArchetypeChunkArray(LeafLocalToParentQuery, Allocator.TempJob);
            InnerTreeLocalToWorldChunks = EntityManager.CreateArchetypeChunkArray(InnerTreeLocalToWorldQuery, Allocator.TempJob);
            LeafLocalToWorldChunks = EntityManager.CreateArchetypeChunkArray(LeafLocalToWorldQuery, Allocator.TempJob);
        }

        void GatherTypes()
        {
            ParentFromEntityRW = GetComponentDataFromEntity<Parent>(false);
            LocalToWorldFromEntityRW = GetComponentDataFromEntity<LocalToWorld>(false);
            LocalToWorldTypeRW = GetArchetypeChunkComponentType<LocalToWorld>(false);
            LocalToParentTypeRW = GetArchetypeChunkComponentType<LocalToParent>(false);
            
            ParentFromEntityRO = GetComponentDataFromEntity<Parent>(true);
            EntityTypeRO = GetArchetypeChunkEntityType();
            ParentTypeRO = GetArchetypeChunkComponentType<Parent>(true);
            LocalToParentTypeRO = GetArchetypeChunkComponentType<LocalToParent>(true);
            DepthTypeRO = GetArchetypeChunkSharedComponentType<Depth>();
            RotationTypeRO = GetArchetypeChunkComponentType<Rotation>(true);
            PositionTypeRO = GetArchetypeChunkComponentType<Position>(true);
            ScaleTypeRO = GetArchetypeChunkComponentType<Scale>(true);
            AttachTypeRO = GetArchetypeChunkComponentType<Attach>(true);
            
            FrozenTypeRO = GetArchetypeChunkComponentType<Frozen>(true);
            PendingFrozenTypeRO = GetArchetypeChunkComponentType<PendingFrozen>(true);
        }

        private static readonly ProfilerMarker k_ProfileGatherDAGChunks = new ProfilerMarker("GatherDAGChunks");
        private static readonly ProfilerMarker k_ProfileUpdateDAG = new ProfilerMarker("UpdateDAG");
        private static readonly ProfilerMarker k_ProfileGatherDepthChunks = new ProfilerMarker("GatherDepthChunks");
        private static readonly ProfilerMarker k_ProfileUpdateDepth = new ProfilerMarker("UpdateDepth");
        private static readonly ProfilerMarker k_ProfileGatherFrozenChunks = new ProfilerMarker("GatherFrozenChunks");
        private static readonly ProfilerMarker k_ProfileUpdateFrozen = new ProfilerMarker("UpdateFrozen");
        private static readonly ProfilerMarker k_ProfileGatherUpdateChunks = new ProfilerMarker("GatherUpdateChunks");
        private static readonly ProfilerMarker k_ProfileUpdateRootLocalToWorld = new ProfilerMarker("UpdateRootLocalToWorld");
        private static readonly ProfilerMarker k_ProfileUpdateInnerTreeLocalToParent = new ProfilerMarker("UpdateInnerTreeLocalToParent");
        private static readonly ProfilerMarker k_ProfileUpdateLeafLocalToParent = new ProfilerMarker("UpdateLeafLocalToParent");
        private static readonly ProfilerMarker k_ProfileUpdateInnerTreeLocalToWorld = new ProfilerMarker("UpdateInnerTreeLocalToWorld");
        private static readonly ProfilerMarker k_ProfileUpdateLeafLocalToWorld = new ProfilerMarker("UpdateLeafLocalToWorld");
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            // #todo When add new Parent, recalc local space

            // Update DAG
            using (k_ProfileGatherDAGChunks.Auto())
            {
                GatherTypes();
                GatherDAGChunks();
            }

            k_ProfileUpdateDAG.Begin();
            var changedDepthStructure = UpdateDAG();
            k_ProfileUpdateDAG.End();

            // Update Transforms

            if (changedDepthStructure)
            {
                using (k_ProfileGatherDepthChunks.Auto())
                {
                    GatherTypes();
                    GatherDepthChunks();
                }

                using (k_ProfileUpdateDepth.Auto())
                {
                    UpdateDepth();
                }
            }

            using (k_ProfileGatherFrozenChunks.Auto())
            {
                GatherTypes();
                GatherFrozenChunks();
            }

            k_ProfileUpdateFrozen.Begin();
            UpdatePendingFrozen();
            UpdateFrozen();
            k_ProfileUpdateFrozen.End();

            k_ProfileGatherUpdateChunks.Begin();
            GatherTypes();
            GatherUpdateChunks();
            k_ProfileGatherUpdateChunks.End();

            k_ProfileUpdateRootLocalToWorld.Begin();
            var updateRootLocalToWorldJobHandle = UpdateRootLocalToWorld(inputDeps);
            k_ProfileUpdateRootLocalToWorld.End();
            
            k_ProfileUpdateInnerTreeLocalToParent.Begin();
            var updateInnerTreeLocalToParentJobHandle = UpdateInnerTreeLocalToParent(updateRootLocalToWorldJobHandle);
            k_ProfileUpdateInnerTreeLocalToParent.End();
            
            k_ProfileUpdateLeafLocalToParent.Begin();
            var updateLeafLocaltoParentJobHandle = UpdateLeafLocalToParent(updateInnerTreeLocalToParentJobHandle);
            k_ProfileUpdateLeafLocalToParent.End();

            k_ProfileUpdateInnerTreeLocalToWorld.Begin();
            var updateInnerTreeLocalToWorldJobHandle = UpdateInnerTreeLocalToWorld(updateLeafLocaltoParentJobHandle);
            k_ProfileUpdateInnerTreeLocalToWorld.End();
            
            k_ProfileUpdateLeafLocalToWorld.Begin();
            var updateLeafLocalToWorldJobHandle = UpdateLeafLocalToWorld(updateInnerTreeLocalToWorldJobHandle);
            k_ProfileUpdateLeafLocalToWorld.End();
            
            LastSystemVersion = GlobalSystemVersion;
            return updateLeafLocalToWorldJobHandle;
        }
    }
}
