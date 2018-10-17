using System;
using System.Runtime.Remoting.Messaging;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace Unity.Rendering
{
    // struct VisibleLocalToWorld : ISystemStateComponentData
    // #TODO Bulk add/remove SystemStateComponentData
    public struct VisibleLocalToWorld : IComponentData
    {
        public float4x4 Value;
    };

    struct FrustumPlanes
    {
        public float4 Left;
        public float4 Right;
        public float4 Down;
        public float4 Up;
        public float4 Near;
        public float4 Far;

        public enum InsideResult
        {
            Out,
            In,
            Partial
        };

        public FrustumPlanes(Camera camera)
        {
            Plane[] sourcePlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            Left = new float4(sourcePlanes[0].normal.x, sourcePlanes[0].normal.y, sourcePlanes[0].normal.z, sourcePlanes[0].distance);
            Right = new float4(sourcePlanes[1].normal.x, sourcePlanes[1].normal.y, sourcePlanes[1].normal.z, sourcePlanes[1].distance);
            Down = new float4(sourcePlanes[2].normal.x, sourcePlanes[2].normal.y, sourcePlanes[2].normal.z, sourcePlanes[2].distance);
            Up = new float4(sourcePlanes[3].normal.x, sourcePlanes[3].normal.y, sourcePlanes[3].normal.z, sourcePlanes[3].distance);
            Near = new float4(sourcePlanes[4].normal.x, sourcePlanes[4].normal.y, sourcePlanes[4].normal.z, sourcePlanes[4].distance);
            Far = new float4(sourcePlanes[5].normal.x, sourcePlanes[5].normal.y, sourcePlanes[5].normal.z, sourcePlanes[5].distance);
        }

        public InsideResult Inside(WorldMeshRenderBounds bounds)
        {
            var center = new float4(bounds.Center.x, bounds.Center.y, bounds.Center.z, 1.0f);

            var leftDistance = math.dot(Left, center);
            var rightDistance = math.dot(Right, center);
            var downDistance = math.dot(Down, center);
            var upDistance = math.dot(Up, center);
            var nearDistance = math.dot(Near, center);
            var farDistance = math.dot(Far, center);

            var leftOut = leftDistance < -bounds.Radius;
            var rightOut = rightDistance < -bounds.Radius;
            var downOut = downDistance < -bounds.Radius;
            var upOut = upDistance < -bounds.Radius;
            var nearOut = nearDistance < -bounds.Radius;
            var farOut = farDistance < -bounds.Radius;
            var anyOut = leftOut || rightOut || downOut || upOut || nearOut || farOut;

            var leftIn = leftDistance > bounds.Radius;
            var rightIn = rightDistance > bounds.Radius;
            var downIn = downDistance > bounds.Radius;
            var upIn = upDistance > bounds.Radius;
            var nearIn = nearDistance > bounds.Radius;
            var farIn = farDistance > bounds.Radius;
            var allIn = leftIn && rightIn && downIn && upIn && nearIn && farIn;

            
            if (anyOut)
                return InsideResult.Out;
            if (allIn)
                return InsideResult.In;
            return InsideResult.Partial;
        }
    }

    /// <summary>
    /// Renders all Entities containing both MeshInstanceRenderer & LocalToWorld components.
    /// </summary>
    [ExecuteInEditMode]
    public class MeshInstanceRendererSystem : ComponentSystem
    {
        public Camera ActiveCamera;

        private int m_LastFrozenChunksOrderVersion = -1;
        private int m_LastDynamicChunksOrderVersion = -1;
        private int m_LastLocalToWorldOrderVersion = -1;

        private NativeArray<ArchetypeChunk> m_FrozenChunks;
        private NativeArray<ArchetypeChunk> m_DynamicChunks;
        private NativeArray<WorldMeshRenderBounds> m_FrozenChunkBounds;
        
        // Instance renderer takes only batches of 1023
        Matrix4x4[] m_MatricesArray = new Matrix4x4[1023];
        private FrustumPlanes m_Planes;
        
        EntityArchetypeQuery m_FrozenChunksQuery;
        EntityArchetypeQuery m_DynamicChunksQuery;
        
        static unsafe void CopyTo(NativeSlice<VisibleLocalToWorld> transforms, int count, Matrix4x4[] outMatrices, int offset)
        {
            // @TODO: This is using unsafe code because the Unity DrawInstances API takes a Matrix4x4[] instead of NativeArray.
            Assert.AreEqual(sizeof(Matrix4x4), sizeof(VisibleLocalToWorld));
            fixed (Matrix4x4* resultMatrices = outMatrices)
            {
                VisibleLocalToWorld* sourceMatrices = (VisibleLocalToWorld*) transforms.GetUnsafeReadOnlyPtr();
                UnsafeUtility.MemCpy(resultMatrices + offset, sourceMatrices , UnsafeUtility.SizeOf<Matrix4x4>() * count);
            }
        }
        
        protected override void OnCreateManager(int capacity)
        {
            m_FrozenChunksQuery = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>(),
                All = new ComponentType[] {typeof(LocalToWorld), typeof(MeshInstanceRenderer), typeof(VisibleLocalToWorld), typeof(Frozen)}
            };
            m_DynamicChunksQuery = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = new ComponentType[] {typeof(Frozen)},
                All = new ComponentType[] {typeof(LocalToWorld), typeof(MeshInstanceRenderer), typeof(VisibleLocalToWorld)}
            };
        }

        protected override void OnDestroyManager()
        {
            if (m_FrozenChunks.IsCreated)
            {
                m_FrozenChunks.Dispose();
            }
            if (m_FrozenChunkBounds.IsCreated)
            {
                m_FrozenChunkBounds.Dispose();
            }
            if (m_DynamicChunks.IsCreated)
            {
                m_DynamicChunks.Dispose();
            }
        }

        [BurstCompile]
        struct UpdateChunkBounds : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public ArchetypeChunkComponentType<WorldMeshRenderBounds> WorldMeshRenderBoundsType;
            public NativeArray<WorldMeshRenderBounds> ChunkBounds;

            public void Execute(int index)
            {
                var chunk = Chunks[index];

                var instanceBounds = chunk.GetNativeArray(WorldMeshRenderBoundsType);
                if (instanceBounds.Length == 0)
                    return;

                // TODO: Improve this approach
                // See: https://www.inf.ethz.ch/personal/emo/DoctThesisFiles/fischer05.pdf

                var chunkBounds = new WorldMeshRenderBounds();
                for (int j = 0; j < instanceBounds.Length; j++)
                {
                    chunkBounds.Center += instanceBounds[j].Center;
                }
                chunkBounds.Center /= instanceBounds.Length;

                for (int j = 0; j < instanceBounds.Length; j++)
                {
                    float r = math.distance(chunkBounds.Center, instanceBounds[j].Center) + instanceBounds[j].Radius;
                    chunkBounds.Radius = math.select(chunkBounds.Radius, r, r > chunkBounds.Radius);
                }

                ChunkBounds[index] = chunkBounds;
            }

        }
        
        [BurstCompile]
        unsafe struct CullLODToVisible : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public ComponentDataFromEntity<ActiveLODGroupMask> ActiveLODGroupMask;
            [ReadOnly] public ArchetypeChunkComponentType<MeshLODComponent> MeshLODComponentType;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorldType;
            [ReadOnly] public ArchetypeChunkComponentType<WorldMeshRenderBounds> WorldMeshRenderBoundsType;
            [NativeDisableUnsafePtrRestriction]
            [ReadOnly] public WorldMeshRenderBounds* ChunkBounds;
            [ReadOnly] public FrustumPlanes Planes;
            public ArchetypeChunkComponentType<VisibleLocalToWorld> VisibleLocalToWorldType;
            public NativeArray<int> ChunkVisibleCount;

            float4x4* GetVisibleOutputBuffer(ArchetypeChunk chunk)
            {
                var chunkVisibleLocalToWorld = chunk.GetNativeArray(VisibleLocalToWorldType);
                return (float4x4*)chunkVisibleLocalToWorld.GetUnsafePtr();
            }
            
            float4x4* GetLocalToWorldSourceBuffer(ArchetypeChunk chunk)
            {
                var chunkLocalToWorld = chunk.GetNativeArray(LocalToWorldType);
                
                if (chunkLocalToWorld.Length > 0)
                    return (float4x4*) chunkLocalToWorld.GetUnsafeReadOnlyPtr();
                else
                    return null;
            }

            void VisibleIn(int index)
            {
                var chunk = Chunks[index];
                var chunkEntityCount = chunk.Count;
                var chunkVisibleCount = 0;
                var chunkLODs = chunk.GetNativeArray(MeshLODComponentType);
                var hasMeshLODComponentType = chunkLODs.Length > 0;

                float4x4* dstPtr = GetVisibleOutputBuffer(chunk);
                float4x4* srcPtr = GetLocalToWorldSourceBuffer(chunk);
                if (srcPtr == null)
                    return;

                if (!hasMeshLODComponentType)
                {
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount + i, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                    }

                    chunkVisibleCount = chunkEntityCount;
                }
                else
                {
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        var instanceLOD = chunkLODs[i];
                        var instanceLODValid = (ActiveLODGroupMask[instanceLOD.Group].LODMask & instanceLOD.LODMask) != 0;
                        if (instanceLODValid)
                        {
                            UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                            chunkVisibleCount++;
                        }
                    }
                }

                ChunkVisibleCount[index] = chunkVisibleCount;
            }

            void VisiblePartial(int index)
            {
                var chunk = Chunks[index];
                var chunkEntityCount = chunk.Count;
                var chunkVisibleCount = 0;
                var chunkLODs = chunk.GetNativeArray(MeshLODComponentType);
                var chunkBounds = chunk.GetNativeArray(WorldMeshRenderBoundsType);
                var hasMeshLODComponentType = chunkLODs.Length > 0;
                var hasWorldMeshRenderBounds = chunkBounds.Length > 0;
                
                float4x4* dstPtr = GetVisibleOutputBuffer(chunk);
                float4x4* srcPtr = GetLocalToWorldSourceBuffer(chunk);
                if (srcPtr == null)
                    return;

                // 00 (-WorldMeshRenderBounds -MeshLODComponentType)
                if ((!hasWorldMeshRenderBounds) && (!hasMeshLODComponentType))
                {
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount + i, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                    }

                    chunkVisibleCount = chunkEntityCount;
                }
                // 01 (-WorldMeshRenderBounds +MeshLODComponentType)
                else if ((!hasWorldMeshRenderBounds) && (hasMeshLODComponentType))
                {
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        var instanceLOD = chunkLODs[i];
                        var instanceLODValid = (ActiveLODGroupMask[instanceLOD.Group].LODMask & instanceLOD.LODMask) != 0;
                        if (instanceLODValid)
                        {
                            UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                            chunkVisibleCount++;
                        }
                    }
                }
                // 10 (+WorldMeshRenderBounds -MeshLODComponentType)
                else if ((hasWorldMeshRenderBounds) && (!hasMeshLODComponentType))
                {
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        var instanceBounds = chunkBounds[i];
                        var instanceCullValid = (Planes.Inside(instanceBounds) != FrustumPlanes.InsideResult.Out);

                        if (instanceCullValid)
                        {
                            UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                            chunkVisibleCount++;
                        }
                    }
                }
                // 11 (+WorldMeshRenderBounds +MeshLODComponentType)
                else
                {
                    
                    for (int i = 0; i < chunkEntityCount; i++)
                    {
                        var instanceLOD = chunkLODs[i];
                        var instanceLODValid = (ActiveLODGroupMask[instanceLOD.Group].LODMask & instanceLOD.LODMask) != 0;
                        if (instanceLODValid)
                        {
                            var instanceBounds = chunkBounds[i];
                            var instanceCullValid = (Planes.Inside(instanceBounds) != FrustumPlanes.InsideResult.Out);
                            if (instanceCullValid)
                            {
                                UnsafeUtility.MemCpy(dstPtr + chunkVisibleCount, srcPtr + i, UnsafeUtility.SizeOf<float4x4>());
                                chunkVisibleCount++;
                            }
                        }
                    }
                }

                ChunkVisibleCount[index] = chunkVisibleCount;
            }

            public void Execute(int index)
            {
                if (ChunkBounds == null)
                {
                    VisiblePartial(index);
                    return;
                }
                
                var chunk = Chunks[index];
                
                var hasWorldMeshRenderBounds = chunk.Has(WorldMeshRenderBoundsType);
                if (!hasWorldMeshRenderBounds)
                {
                    VisibleIn(index);
                    return;
                }
                
                var chunkBounds = ChunkBounds[index];
                var chunkInsideResult = Planes.Inside(chunkBounds);
                if (chunkInsideResult == FrustumPlanes.InsideResult.Out)
                {
                    ChunkVisibleCount[index] = 0;
                }
                else if (chunkInsideResult == FrustumPlanes.InsideResult.In)
                {
                    VisibleIn(index);
                }
                else
                {
                    VisiblePartial(index);
                }
            }
        };
        
        [BurstCompile]
        struct MapChunkRenderers : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public ArchetypeChunkSharedComponentType<MeshInstanceRenderer> MeshInstanceRendererType;
            public NativeMultiHashMap<int, int>.Concurrent ChunkRendererMap;

            public void Execute(int index)
            {
                var chunk = Chunks[index];
                var rendererSharedComponentIndex = chunk.GetSharedComponentIndex(MeshInstanceRendererType);
                ChunkRendererMap.Add(rendererSharedComponentIndex, index);
            }
        };

        [BurstCompile]
        struct GatherSortedChunks : IJob
        {
            [ReadOnly] public NativeMultiHashMap<int, int> ChunkRendererMap;
            public int SharedComponentCount;
            public NativeArray<ArchetypeChunk> SortedChunks;
            public NativeArray<ArchetypeChunk> Chunks;

            public void Execute()
            {
                int sortedIndex = 0;
                for (int i = 0; i < SharedComponentCount; i++)
                {
                    int chunkIndex = 0;

                    NativeMultiHashMapIterator<int> it;
                    if (!ChunkRendererMap.TryGetFirstValue(i, out chunkIndex, out it))
                        continue;
                    do
                    {
                        SortedChunks[sortedIndex] = Chunks[chunkIndex];
                        sortedIndex++;
                    } while (ChunkRendererMap.TryGetNextValue(out chunkIndex, ref it));
                }
            }
        };

        [BurstCompile]
        unsafe struct PackVisibleChunkIndices : IJob
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public NativeArray<int> ChunkVisibleCount;
            public NativeArray<int> PackedChunkIndices;
            [NativeDisableUnsafePtrRestriction]
            public int* PackedChunkCount;

            public void Execute()
            {
                var packedChunkCount = 0;
                for (int i = 0; i < Chunks.Length; i++)
                {
                    if (ChunkVisibleCount[i] > 0)
                    {
                        PackedChunkIndices[packedChunkCount] = i;
                        packedChunkCount++;
                    }
                }
                *PackedChunkCount = packedChunkCount;
            }

        }
        
        unsafe void UpdateFrozenInstanceRenderer()
        {
            if (m_FrozenChunks.Length == 0)
            {
                return;
            }
            
            Profiler.BeginSample("Gather Types");
            var sharedComponentCount = EntityManager.GetSharedComponentCount();
            var localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(true);
            var visibleLocalToWorldType = GetArchetypeChunkComponentType<VisibleLocalToWorld>(false);
            var meshInstanceRendererType = GetArchetypeChunkSharedComponentType<MeshInstanceRenderer>();
            var meshInstanceFlippedTagType = GetArchetypeChunkComponentType<MeshInstanceFlippedWindingTag>();
            var worldMeshRenderBoundsType = GetArchetypeChunkComponentType<WorldMeshRenderBounds>(true);
            var meshLODComponentType = GetArchetypeChunkComponentType<MeshLODComponent>(true);
            var activeLODGroupMask = GetComponentDataFromEntity<ActiveLODGroupMask>(true);

            Profiler.EndSample();
            
            Profiler.BeginSample("Allocate Temp Data");
            var chunkVisibleCount   = new NativeArray<int>(m_FrozenChunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var packedChunkIndices  = new NativeArray<int>(m_FrozenChunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            Profiler.EndSample();
                
            var cullLODToVisibleJob = new CullLODToVisible
            {
                Chunks = m_FrozenChunks,
                ActiveLODGroupMask = activeLODGroupMask,
                MeshLODComponentType = meshLODComponentType,
                LocalToWorldType = localToWorldType,
                WorldMeshRenderBoundsType = worldMeshRenderBoundsType,
                ChunkBounds = (WorldMeshRenderBounds*)m_FrozenChunkBounds.GetUnsafePtr(),
                Planes = m_Planes,
                VisibleLocalToWorldType = visibleLocalToWorldType,
                ChunkVisibleCount = chunkVisibleCount,
            };
            var cullLODToVisibleJobHandle = cullLODToVisibleJob.Schedule(m_FrozenChunks.Length, 64);

            var packedChunkCount = 0;
            var packVisibleChunkIndicesJob = new PackVisibleChunkIndices
            {
                Chunks = m_FrozenChunks,
                ChunkVisibleCount =  chunkVisibleCount,
                PackedChunkIndices = packedChunkIndices,
                PackedChunkCount = &packedChunkCount
            };
            var packVisibleChunkIndicesJobHandle = packVisibleChunkIndicesJob.Schedule(cullLODToVisibleJobHandle);
            packVisibleChunkIndicesJobHandle.Complete();
                
            Profiler.BeginSample("Process DrawMeshInstanced");
            var drawCount = 0;
            var lastRendererIndex = -1;
            var batchCount = 0;
            var flippedWinding = false;

            for (int i = 0; i < packedChunkCount; i++)
            {
                var chunkIndex = packedChunkIndices[i];
                var chunk = m_FrozenChunks[chunkIndex];
                var rendererIndex = chunk.GetSharedComponentIndex(meshInstanceRendererType);
                var activeCount = chunkVisibleCount[chunkIndex];
                var rendererChanged = rendererIndex != lastRendererIndex;
                var fullBatch = ((batchCount + activeCount) > 1023);
                var visibleTransforms = chunk.GetNativeArray(visibleLocalToWorldType);

                var newFlippedWinding = chunk.Has(meshInstanceFlippedTagType);

                if ((fullBatch || rendererChanged || (newFlippedWinding != flippedWinding)) && (batchCount > 0))
                {
                    RenderBatch(lastRendererIndex, batchCount);

                    drawCount++;
                    batchCount = 0;
                }

                CopyTo(visibleTransforms, activeCount, m_MatricesArray, batchCount);

                flippedWinding = newFlippedWinding;
                batchCount += activeCount;
                lastRendererIndex = rendererIndex;
            }

            if (batchCount > 0)
            {
                RenderBatch(lastRendererIndex, batchCount);

                drawCount++;
            }
            Profiler.EndSample();
            
            packedChunkIndices.Dispose();
            chunkVisibleCount.Dispose();
        }
        
        unsafe void UpdateDynamicInstanceRenderer()
        {
            if (m_DynamicChunks.Length == 0)
            {
                return;
            }
            
            Profiler.BeginSample("Gather Types");
            var sharedComponentCount = EntityManager.GetSharedComponentCount();
            var localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(true);
            var visibleLocalToWorldType = GetArchetypeChunkComponentType<VisibleLocalToWorld>(false);
            var meshInstanceRendererType = GetArchetypeChunkSharedComponentType<MeshInstanceRenderer>();
            var meshInstanceFlippedTagType = GetArchetypeChunkComponentType<MeshInstanceFlippedWindingTag>();
            var worldMeshRenderBoundsType = GetArchetypeChunkComponentType<WorldMeshRenderBounds>(true);
            var meshLODComponentType = GetArchetypeChunkComponentType<MeshLODComponent>(true);
            var activeLODGroupMask = GetComponentDataFromEntity<ActiveLODGroupMask>(true);
            Profiler.EndSample();
            
            Profiler.BeginSample("Allocate Temp Data");
            var chunkVisibleCount   = new NativeArray<int>(m_DynamicChunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var packedChunkIndices  = new NativeArray<int>(m_DynamicChunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            Profiler.EndSample();
                
            var cullLODToVisibleJob = new CullLODToVisible
            {
                Chunks = m_DynamicChunks,
                ActiveLODGroupMask = activeLODGroupMask,
                MeshLODComponentType = meshLODComponentType,
                LocalToWorldType = localToWorldType,
                WorldMeshRenderBoundsType = worldMeshRenderBoundsType,
                ChunkBounds = null,
                Planes = m_Planes,
                VisibleLocalToWorldType = visibleLocalToWorldType,
                ChunkVisibleCount = chunkVisibleCount,
            };
            var cullLODToVisibleJobHandle = cullLODToVisibleJob.Schedule(m_DynamicChunks.Length, 64);

            var packedChunkCount = 0;
            var packVisibleChunkIndicesJob = new PackVisibleChunkIndices
            {
                Chunks = m_DynamicChunks,
                ChunkVisibleCount =  chunkVisibleCount,
                PackedChunkIndices = packedChunkIndices,
                PackedChunkCount = &packedChunkCount
            };
            var packVisibleChunkIndicesJobHandle = packVisibleChunkIndicesJob.Schedule(cullLODToVisibleJobHandle);
            packVisibleChunkIndicesJobHandle.Complete();
                
            Profiler.BeginSample("Process DrawMeshInstanced");
            var drawCount = 0;
            var lastRendererIndex = -1;
            var batchCount = 0;
            var flippedWinding = false;

            for (int i = 0; i < packedChunkCount; i++)
            {
                var chunkIndex = packedChunkIndices[i];
                var chunk = m_DynamicChunks[chunkIndex];
                var rendererIndex = chunk.GetSharedComponentIndex(meshInstanceRendererType);
                var activeCount = chunkVisibleCount[chunkIndex];
                var rendererChanged = rendererIndex != lastRendererIndex;
                var fullBatch = ((batchCount + activeCount) > 1023);
                var visibleTransforms = chunk.GetNativeArray(visibleLocalToWorldType);

                var newFlippedWinding = chunk.Has(meshInstanceFlippedTagType);

                if ((fullBatch || rendererChanged || (newFlippedWinding != flippedWinding)) && (batchCount > 0))
                {
                    RenderBatch(lastRendererIndex, batchCount);

                    drawCount++;
                    batchCount = 0;
                }

                CopyTo(visibleTransforms, activeCount, m_MatricesArray, batchCount);

                flippedWinding = newFlippedWinding;
                batchCount += activeCount;
                lastRendererIndex = rendererIndex;
            }

            if (batchCount > 0)
            {
                RenderBatch(lastRendererIndex, batchCount);

                drawCount++;
            }
            Profiler.EndSample();
            
            packedChunkIndices.Dispose();
            chunkVisibleCount.Dispose();
        }
        
        void RenderBatch(int lastRendererIndex, int batchCount)
        {
            var renderer = EntityManager.GetSharedComponentData<MeshInstanceRenderer>(lastRendererIndex);
            if (renderer.mesh && renderer.material)
            {
                if (renderer.material.enableInstancing)
                {
                    Graphics.DrawMeshInstanced(renderer.mesh, renderer.subMesh, renderer.material,
                        m_MatricesArray,
                        batchCount, null, renderer.castShadows, renderer.receiveShadows, 0, ActiveCamera);
                }
                else
                {
                    for (int i = 0; i != batchCount; i++)
                    {
                        Graphics.DrawMesh(renderer.mesh, m_MatricesArray[i], renderer.material, 0, ActiveCamera, renderer.subMesh, null, renderer.castShadows, renderer.receiveShadows);
                    }
                    
                    if (batchCount >= 2)
                        Debug.LogWarning($"Please enable GPU instancing for better performance ({renderer.material})", renderer.material);
                }
            }
        }
        
        void UpdateFrozenChunkCache()
        {
            var visibleLocalToWorldOrderVersion = EntityManager.GetComponentOrderVersion<VisibleLocalToWorld>();
            var frozenOrderVersion = EntityManager.GetComponentOrderVersion<Frozen>();
            var staticChunksOrderVersion = math.min(visibleLocalToWorldOrderVersion, frozenOrderVersion);
            if (staticChunksOrderVersion == m_LastFrozenChunksOrderVersion)
                return;
            
            // Dispose
            if (m_FrozenChunks.IsCreated)
            {
                m_FrozenChunks.Dispose();
            }
            if (m_FrozenChunkBounds.IsCreated)
            {
                m_FrozenChunkBounds.Dispose();
            }
            
            var sharedComponentCount = EntityManager.GetSharedComponentCount();
            var meshInstanceRendererType = GetArchetypeChunkSharedComponentType<MeshInstanceRenderer>();
            var worldMeshRenderBoundsType = GetArchetypeChunkComponentType<WorldMeshRenderBounds>(true);
            
            // Allocate temp data
            var chunkRendererMap = new NativeMultiHashMap<int, int>(100000, Allocator.TempJob);
            var foundArchetypes = new NativeList<EntityArchetype>(Allocator.TempJob);

            Profiler.BeginSample("CreateArchetypeChunkArray");
            EntityManager.AddMatchingArchetypes(m_FrozenChunksQuery, foundArchetypes);
            var chunks = EntityManager.CreateArchetypeChunkArray(foundArchetypes, Allocator.TempJob);
            Profiler.EndSample();
            
            m_FrozenChunks = new NativeArray<ArchetypeChunk>(chunks.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_FrozenChunkBounds = new NativeArray<WorldMeshRenderBounds>(chunks.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            
            var mapChunkRenderersJob = new MapChunkRenderers
            {
                Chunks = chunks,
                MeshInstanceRendererType = meshInstanceRendererType,
                ChunkRendererMap = chunkRendererMap
            };
            var mapChunkRenderersJobHandle = mapChunkRenderersJob.Schedule(chunks.Length, 64);
            
            var gatherSortedChunksJob = new GatherSortedChunks
            {
                ChunkRendererMap = chunkRendererMap,
                SharedComponentCount = sharedComponentCount,
                SortedChunks = m_FrozenChunks,
                Chunks = chunks
            };
            var gatherSortedChunksJobHandle = gatherSortedChunksJob.Schedule(mapChunkRenderersJobHandle);
            
            var updateChangedChunkBoundsJob = new UpdateChunkBounds
            {
                Chunks = m_FrozenChunks,
                WorldMeshRenderBoundsType = worldMeshRenderBoundsType,
                ChunkBounds = m_FrozenChunkBounds
            };
            var updateChangedChunkBoundsJobHandle = updateChangedChunkBoundsJob.Schedule(chunks.Length, 64, gatherSortedChunksJobHandle);
            updateChangedChunkBoundsJobHandle.Complete();
            
            foundArchetypes.Dispose();
            chunkRendererMap.Dispose();
            chunks.Dispose();

            m_LastFrozenChunksOrderVersion = staticChunksOrderVersion;
        }
        
        void UpdateDynamicChunkCache()
        {
            var dynamicChunksOrderVersion = EntityManager.GetComponentOrderVersion<VisibleLocalToWorld>();
            if (dynamicChunksOrderVersion == m_LastDynamicChunksOrderVersion)
                return;
            
            // Dispose
            if (m_DynamicChunks.IsCreated)
            {
                m_DynamicChunks.Dispose();
            }
            
            var sharedComponentCount = EntityManager.GetSharedComponentCount();
            var meshInstanceRendererType = GetArchetypeChunkSharedComponentType<MeshInstanceRenderer>();
            var worldMeshRenderBoundsType = GetArchetypeChunkComponentType<WorldMeshRenderBounds>(true);
            
            // Allocate temp data
            var chunkRendererMap = new NativeMultiHashMap<int, int>(100000, Allocator.TempJob);
            var foundArchetypes = new NativeList<EntityArchetype>(Allocator.TempJob);

            Profiler.BeginSample("CreateArchetypeChunkArray");
            EntityManager.AddMatchingArchetypes(m_DynamicChunksQuery, foundArchetypes);
            var chunks = EntityManager.CreateArchetypeChunkArray(foundArchetypes, Allocator.TempJob);
            Profiler.EndSample();
            
            m_DynamicChunks = new NativeArray<ArchetypeChunk>(chunks.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            
            var mapChunkRenderersJob = new MapChunkRenderers
            {
                Chunks = chunks,
                MeshInstanceRendererType = meshInstanceRendererType,
                ChunkRendererMap = chunkRendererMap
            };
            var mapChunkRenderersJobHandle = mapChunkRenderersJob.Schedule(chunks.Length, 64);
            
            var gatherSortedChunksJob = new GatherSortedChunks
            {
                ChunkRendererMap = chunkRendererMap,
                SharedComponentCount = sharedComponentCount,
                SortedChunks = m_DynamicChunks,
                Chunks = chunks
            };
            var gatherSortedChunksJobHandle = gatherSortedChunksJob.Schedule(mapChunkRenderersJobHandle);
            gatherSortedChunksJobHandle.Complete();
            
            foundArchetypes.Dispose();
            chunkRendererMap.Dispose();
            chunks.Dispose();

            m_LastDynamicChunksOrderVersion = dynamicChunksOrderVersion;
        }

        void UpdateMissingVisibleLocalToWorld()
        {
            var localToWorldOrderVersion = EntityManager.GetComponentOrderVersion<LocalToWorld>();
            if (localToWorldOrderVersion == m_LastLocalToWorldOrderVersion)
                return;
            
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            
            var query = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = new ComponentType[] {typeof(VisibleLocalToWorld)},
                All = new ComponentType[] {typeof(MeshInstanceRenderer), typeof(LocalToWorld)}
            };
            var entityType = GetArchetypeChunkEntityType();
            var chunks = EntityManager.CreateArchetypeChunkArray(query, Allocator.TempJob);
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var entities = chunk.GetNativeArray(entityType);
                for (int j = 0; j < chunk.Count; j++)
                {
                    var entity = entities[j];
                    entityCommandBuffer.AddComponent(entity,default(VisibleLocalToWorld));
                }
            }
            
            entityCommandBuffer.Playback(EntityManager);
            entityCommandBuffer.Dispose();
            chunks.Dispose();

            m_LastLocalToWorldOrderVersion = localToWorldOrderVersion;
        }

        protected override void OnUpdate()
        {
            if (ActiveCamera != null)
            {
                m_Planes = new FrustumPlanes(ActiveCamera);

                UpdateMissingVisibleLocalToWorld();

                Profiler.BeginSample("UpdateFrozenChunkCache");
                UpdateFrozenChunkCache();
                Profiler.EndSample();
                
                Profiler.BeginSample("UpdateDynamicChunkCache");
                UpdateDynamicChunkCache();
                Profiler.EndSample();

                Profiler.BeginSample("UpdateFrozenInstanceRenderer");
                UpdateFrozenInstanceRenderer();
                Profiler.EndSample();
                
                Profiler.BeginSample("UpdateDynamicInstanceRenderer");
                UpdateDynamicInstanceRenderer();
                Profiler.EndSample();
            }
        }
    }
}