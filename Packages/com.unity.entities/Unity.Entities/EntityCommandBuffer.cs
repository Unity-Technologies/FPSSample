using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Profiling;

namespace Unity.Entities
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct BasicCommand
    {
        public int CommandType;
        public int TotalSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CreateCommand
    {
        public BasicCommand Header;
        public EntityArchetype Archetype;
        public int BatchCount;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityCommand
    {
        public BasicCommand Header;
        public Entity Entity;
        public int BatchCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityComponentCommand
    {
        public EntityCommand Header;
        public int ComponentTypeIndex;

        public int ComponentSize;
        // Data follows if command has an associated component payload
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EntityBufferCommand
    {
        public EntityCommand Header;
        public int ComponentTypeIndex;

        public int ComponentSize;
        public BufferHeader TempBuffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EntitySharedComponentCommand
    {
        public EntityCommand Header;
        public int ComponentTypeIndex;
        public int HashCode;
        public GCHandle BoxedObject;
        public EntitySharedComponentCommand* Prev;

        internal object GetBoxedObject()
        {
            if (BoxedObject.IsAllocated)
                return BoxedObject.Target;
            return null;
        }
    }

    internal unsafe struct EntityCommandBufferChain
    {
        public ECBChunk* m_Tail;
        public ECBChunk* m_Head;
        public EntitySharedComponentCommand* m_CleanupList;
        public CreateCommand*                m_PrevCreateCommand;
        public EntityCommand*                m_PrevEntityCommand;
    }

    internal enum ECBCommand
    {
        InstantiateEntity,

        CreateEntity,
        DestroyEntity,
        
        AddComponent,
        RemoveComponent,
        SetComponent,

        AddBuffer,
        SetBuffer,

        AddSharedComponentData,
        SetSharedComponentData
    }

    /// <summary>
    /// Organized in memory like a single block with Chunk header followed by Size bytes of data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ECBChunk
    {
        internal int Used;
        internal int Size;
        internal ECBChunk* Next;
        internal ECBChunk* Prev;

        internal int Capacity => Size - Used;

        internal int Bump(int size)
        {
            var off = Used;
            Used += size;
            return off;
        }
    }


    internal unsafe struct EntityCommandBufferData
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public AtomicSafetyHandle m_BufferSafety;
        public AtomicSafetyHandle m_ArrayInvalidationSafety;
#endif

        public EntityCommandBufferChain m_MainThreadChain;

        public EntityCommandBufferChain* m_ThreadedChains;

        public int m_MinimumChunkSize;

        public Allocator m_Allocator;

        public bool m_ShouldPlayback;
        public const int kMaxBatchCount = 512;

        internal void InitConcurrentAccess()
        {
            if (m_ThreadedChains != null)
                return;

            // PERF: It's be great if we had a way to actually get the number of worst-case threads so we didn't have to allocate 128.
            int allocSize = sizeof(EntityCommandBufferChain) * JobsUtility.MaxJobThreadCount;

            m_ThreadedChains = (EntityCommandBufferChain*) UnsafeUtility.Malloc(allocSize, 8, m_Allocator);
            UnsafeUtility.MemClear(m_ThreadedChains, allocSize);
        }

        internal void DestroyConcurrentAccess()
        {
            if (m_ThreadedChains != null)
            {
                UnsafeUtility.Free(m_ThreadedChains, m_Allocator);
                m_ThreadedChains = null;
            }
        }

        internal void AddCreateCommand(EntityCommandBufferChain* chain, ECBCommand op, EntityArchetype archetype)
        {
            if (chain->m_PrevCreateCommand != null &&
                chain->m_PrevCreateCommand->Archetype == archetype &&
                chain->m_PrevCreateCommand->BatchCount < kMaxBatchCount)
            {
                ++chain->m_PrevCreateCommand->BatchCount;

                var data = (CreateCommand*) Reserve(chain, sizeof(CreateCommand));

                data->Header.CommandType = (int) op;
                data->Header.TotalSize = sizeof(CreateCommand);
                data->Archetype = archetype;
                data->BatchCount = 0;
            }
            else
            {
                var data = (CreateCommand*) Reserve(chain, sizeof(CreateCommand));

                data->Header.CommandType = (int) op;
                data->Header.TotalSize = sizeof(CreateCommand);
                data->Archetype = archetype;
                data->BatchCount = 1;

                chain->m_PrevCreateCommand = data;
            }
        }

        internal void AddEntityCommand(EntityCommandBufferChain* chain, ECBCommand op, Entity e)
        {
            if (chain->m_PrevEntityCommand != null &&
                chain->m_PrevEntityCommand->Entity == e &&
                chain->m_PrevEntityCommand->BatchCount < kMaxBatchCount)
            {
                ++chain->m_PrevEntityCommand->BatchCount;

                var data = (EntityCommand*) Reserve(chain, sizeof(EntityCommand));

                data->Header.CommandType = (int) op;
                data->Header.TotalSize = sizeof(EntityCommand);
                data->Entity = e;
                data->BatchCount = 0;
            }
            else
            {
                var data = (EntityCommand*) Reserve(chain, sizeof(EntityCommand));

                data->Header.CommandType = (int) op;
                data->Header.TotalSize = sizeof(EntityCommand);
                data->Entity = e;
                data->BatchCount = 1;

                chain->m_PrevEntityCommand = data;
            }
        }

        internal void AddEntityComponentCommand<T>(EntityCommandBufferChain* chain, ECBCommand op, Entity e, T component) where T : struct
        {
            var typeSize = UnsafeUtility.SizeOf<T>();
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var sizeNeeded = Align(sizeof(EntityComponentCommand) + typeSize, 8);

            var data = (EntityComponentCommand*)Reserve(chain, sizeNeeded);

            data->Header.Header.CommandType = (int)op;
            data->Header.Header.TotalSize = sizeNeeded;
            data->Header.Entity = e;
            data->ComponentTypeIndex = typeIndex;
            data->ComponentSize = typeSize;

            UnsafeUtility.CopyStructureToPtr(ref component, (byte*)(data + 1));
        }

        internal BufferHeader* AddEntityBufferCommand<T>(EntityCommandBufferChain* chain, ECBCommand op, Entity e) where T : struct, IBufferElementData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var type = TypeManager.GetTypeInfo<T>();
            var sizeNeeded = Align(sizeof(EntityBufferCommand) + type.SizeInChunk, 8);

            var data = (EntityBufferCommand*)Reserve(chain, sizeNeeded);

            data->Header.Header.CommandType = (int)op;
            data->Header.Header.TotalSize = sizeNeeded;
            data->Header.Entity = e;
            data->ComponentTypeIndex = typeIndex;
            data->ComponentSize = type.SizeInChunk;

            BufferHeader.Initialize(&data->TempBuffer, type.BufferCapacity);
            return &data->TempBuffer;
        }

        internal static int Align(int size, int alignmentPowerOfTwo)
        {
            return (size + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);
        }

        internal void AddEntityComponentTypeCommand(EntityCommandBufferChain* chain, ECBCommand op, Entity e, ComponentType t)
        {
            var sizeNeeded = Align(sizeof(EntityComponentCommand), 8);

            var data = (EntityComponentCommand*)Reserve(chain, sizeNeeded);

            data->Header.Header.CommandType = (int)op;
            data->Header.Header.TotalSize = sizeNeeded;
            data->Header.Entity = e;
            data->ComponentTypeIndex = t.TypeIndex;
        }

        internal void AddEntitySharedComponentCommand<T>(EntityCommandBufferChain* chain, ECBCommand op, Entity e, int hashCode, object boxedObject)
            where T : struct
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var sizeNeeded = Align(sizeof(EntitySharedComponentCommand), 8);

            var data = (EntitySharedComponentCommand*)Reserve(chain, sizeNeeded);

            data->Header.Header.CommandType = (int)op;
            data->Header.Header.TotalSize = sizeNeeded;
            data->Header.Entity = e;
            data->ComponentTypeIndex = typeIndex;
            data->HashCode = hashCode;

            if (boxedObject != null)
            {
                data->BoxedObject = GCHandle.Alloc(boxedObject);
                // We need to store all GCHandles on a cleanup list so we can dispose them later, regardless of if playback occurs or not.
                data->Prev = chain->m_CleanupList;
                chain->m_CleanupList = data;
            }
            else
            {
                data->BoxedObject = new GCHandle();
            }
        }

        internal byte* Reserve(EntityCommandBufferChain* chain, int size)
        {
            if (chain->m_Tail == null || chain->m_Tail->Capacity < size)
            {
                var chunkSize = math.max(m_MinimumChunkSize, size);

                var c = (ECBChunk*)UnsafeUtility.Malloc(sizeof(ECBChunk) + chunkSize, 16, m_Allocator);
                var prev = chain->m_Tail;
                c->Next = null;
                c->Prev = prev;
                c->Used = 0;
                c->Size = chunkSize;

                if (prev != null) prev->Next = c;

                if (chain->m_Head == null) chain->m_Head = c;

                chain->m_Tail = c;
            }

            var offset = chain->m_Tail->Bump(size);
            var ptr = (byte*)chain->m_Tail + sizeof(ECBChunk) + offset;
            return ptr;
        }

        public DynamicBuffer<T> CreateBufferCommand<T>(ECBCommand commandType, EntityCommandBufferChain* chain, Entity e) where T : struct, IBufferElementData
        {
            BufferHeader* header = AddEntityBufferCommand<T>(chain, commandType, e);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = m_BufferSafety;
            AtomicSafetyHandle.UseSecondaryVersion(ref safety);
            var arraySafety = m_ArrayInvalidationSafety;
            return new DynamicBuffer<T>(header, safety, arraySafety);
#else
            return new DynamicBuffer<T>(header);
#endif
        }

    }

    /// <summary>
    ///     A thread-safe command buffer that can buffer commands that affect entities and components for later playback.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public unsafe struct EntityCommandBuffer : IDisposable
    {
        /// <summary>
        ///     The minimum chunk size to allocate from the job allocator.
        /// </summary>
        /// We keep this relatively small as we don't want to overload the temp allocator in case people make a ton of command buffers.
        private const int kDefaultMinimumChunkSize = 4 * 1024;

        [NativeDisableUnsafePtrRestriction] private EntityCommandBufferData* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule] private DisposeSentinel m_DisposeSentinel;
#endif

        /// <summary>
        ///     Allows controlling the size of chunks allocated from the temp job allocator to back the command buffer.
        /// </summary>
        /// Larger sizes are more efficient, but create more waste in the allocator.
        public int MinimumChunkSize
        {
            get => m_Data->m_MinimumChunkSize > 0 ? m_Data->m_MinimumChunkSize : kDefaultMinimumChunkSize;
            set => m_Data->m_MinimumChunkSize = Math.Max(0, value);
        }

        /// <summary>
        /// Controls whether this command buffer should play back.
        /// </summary>
        ///
        /// This property is normally true, but can be useful to prevent
        /// the buffer from playing back when the user code is not in control
        /// of the site of playback.
        ///
        /// For example, is a buffer has been aquired from a barrier and partially
        /// filled in with data, but it is discovered that the work should be aborted,
        /// this property can be set to true to prevent the buffer from playing back.
        public bool ShouldPlayback
        {
            get { return m_Data != null ? m_Data->m_ShouldPlayback : false; }
            set { if (m_Data != null) m_Data->m_ShouldPlayback = value; }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void EnforceSingleThreadOwnership()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }

        /// <summary>
        ///  Creates a new command buffer.
        /// </summary>
        /// <param name="label">Memory allocator to use for chunks and data</param>
        public EntityCommandBuffer(Allocator label)
        {
            m_Data = (EntityCommandBufferData*)UnsafeUtility.Malloc(sizeof(EntityCommandBufferData),
                UnsafeUtility.AlignOf<EntityCommandBufferData>(), label);
            m_Data->m_Allocator = label;
            m_Data->m_MinimumChunkSize = kDefaultMinimumChunkSize;
            m_Data->m_ShouldPlayback = true;

            m_Data->m_MainThreadChain.m_CleanupList = null;
            m_Data->m_MainThreadChain.m_Tail = null;
            m_Data->m_MainThreadChain.m_Head = null;
            m_Data->m_MainThreadChain.m_PrevCreateCommand = null;
            m_Data->m_MainThreadChain.m_PrevEntityCommand = null;

            m_Data->m_ThreadedChains = null;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Used for all buffers returned from the API, so we can invalidate them once Playback() has been called.
            m_Data->m_BufferSafety = AtomicSafetyHandle.Create();
            // Used to invalidate array aliases to buffers
            m_Data->m_ArrayInvalidationSafety = AtomicSafetyHandle.Create();
#if UNITY_2018_3_OR_NEWER
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 8, label);
#else
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 8);
#endif
#endif
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if UNITY_2018_3_OR_NEWER
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#else
            DisposeSentinel.Dispose(m_Safety, ref m_DisposeSentinel);
#endif
#endif

            if (m_Data != null)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(m_Data->m_ArrayInvalidationSafety);
                AtomicSafetyHandle.Release(m_Data->m_BufferSafety);
#endif

                FreeChain(&m_Data->m_MainThreadChain);

                if (m_Data->m_ThreadedChains != null)
                {
                    for (int i = 0; i < JobsUtility.MaxJobThreadCount; ++i)
                    {
                        FreeChain(&m_Data->m_ThreadedChains[i]);
                    }

                    m_Data->DestroyConcurrentAccess();
                }

                UnsafeUtility.Free(m_Data, m_Data->m_Allocator);
                m_Data = null;
            }
        }

        private void FreeChain(EntityCommandBufferChain* chain)
        {
            var cleanup_list = chain->m_CleanupList;
            while (cleanup_list != null)
            {
                cleanup_list->BoxedObject.Free();
                cleanup_list = cleanup_list->Prev;
            }

            chain->m_CleanupList = null;

            while (chain->m_Tail != null)
            {
                var prev = chain->m_Tail->Prev;
                UnsafeUtility.Free(chain->m_Tail, m_Data->m_Allocator);
                chain->m_Tail = prev;
            }

            chain->m_Head = null;
        }

        public void CreateEntity()
        {
            EnforceSingleThreadOwnership();
            m_Data->AddCreateCommand(&m_Data->m_MainThreadChain, ECBCommand.CreateEntity, new EntityArchetype());
        }

        public void CreateEntity(EntityArchetype archetype)
        {
            EnforceSingleThreadOwnership();
            m_Data->AddCreateCommand(&m_Data->m_MainThreadChain, ECBCommand.CreateEntity, archetype);
        }

        public void Instantiate(Entity e)
        {
            EnforceSingleThreadOwnership();
            m_Data->AddEntityCommand(&m_Data->m_MainThreadChain, ECBCommand.InstantiateEntity, e);
        }
        
        public void DestroyEntity(Entity e)
        {
            EnforceSingleThreadOwnership();
            m_Data->AddEntityCommand(&m_Data->m_MainThreadChain, ECBCommand.DestroyEntity, e);
        }

        public DynamicBuffer<T> AddBuffer<T>() where T : struct, IBufferElementData
        {
            return AddBuffer<T>(Entity.Null);
        }

        public DynamicBuffer<T> AddBuffer<T>(Entity e) where T : struct, IBufferElementData
        {
            EnforceSingleThreadOwnership();
            return m_Data->CreateBufferCommand<T>(ECBCommand.AddBuffer, &m_Data->m_MainThreadChain, e);
        }

        public DynamicBuffer<T> SetBuffer<T>() where T : struct, IBufferElementData
        {
            return SetBuffer<T>(Entity.Null);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity e) where T : struct, IBufferElementData
        {
            EnforceSingleThreadOwnership();
            return m_Data->CreateBufferCommand<T>(ECBCommand.SetBuffer, &m_Data->m_MainThreadChain, e);
        }

        public void AddComponent<T>(Entity e, T component) where T : struct, IComponentData
        {
            EnforceSingleThreadOwnership();
            m_Data->AddEntityComponentCommand(&m_Data->m_MainThreadChain, ECBCommand.AddComponent, e, component);
        }

        public void AddComponent<T>(T component) where T : struct, IComponentData
        {
            AddComponent(Entity.Null, component);
        }

        public void SetComponent<T>(T component) where T : struct, IComponentData
        {
            SetComponent(Entity.Null, component);
        }

        public void SetComponent<T>(Entity e, T component) where T : struct, IComponentData
        {
            EnforceSingleThreadOwnership();
            m_Data->AddEntityComponentCommand(&m_Data->m_MainThreadChain, ECBCommand.SetComponent, e, component);
        }

        public void RemoveComponent<T>(Entity e)
        {
            RemoveComponent(e, ComponentType.Create<T>());
        }

        public void RemoveComponent(Entity e, ComponentType componentType)
        {
            EnforceSingleThreadOwnership();
            m_Data->AddEntityComponentTypeCommand(&m_Data->m_MainThreadChain, ECBCommand.RemoveComponent, e, componentType);
        }
        
        
        private static bool IsDefaultObject<T>(ref T component, out int hashCode) where T : struct, ISharedComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var typeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
            var defaultValue = default(T);
            hashCode = FastEquality.GetHashCode(ref component, typeInfo);
            return FastEquality.Equals(ref defaultValue, ref component, typeInfo);
        }

        public void AddSharedComponent<T>(T component) where T : struct, ISharedComponentData
        {
            AddSharedComponent(Entity.Null, component);
        }

        public void AddSharedComponent<T>(Entity e, T component) where T : struct, ISharedComponentData
        {
            EnforceSingleThreadOwnership();
            int hashCode;
            if (IsDefaultObject(ref component, out hashCode))
                m_Data->AddEntitySharedComponentCommand<T>(&m_Data->m_MainThreadChain, ECBCommand.AddSharedComponentData, e, hashCode, null);
            else
                m_Data->AddEntitySharedComponentCommand<T>(&m_Data->m_MainThreadChain, ECBCommand.AddSharedComponentData, e, hashCode, component);
        }

        public void SetSharedComponent<T>(T component) where T : struct, ISharedComponentData
        {
            SetSharedComponent(Entity.Null, component);
        }

        public void SetSharedComponent<T>(Entity e, T component) where T : struct, ISharedComponentData
        {
            EnforceSingleThreadOwnership();
            int hashCode;
            if (IsDefaultObject(ref component, out hashCode))
                m_Data->AddEntitySharedComponentCommand<T>(&m_Data->m_MainThreadChain, ECBCommand.SetSharedComponentData, e, hashCode, null);
            else
                m_Data->AddEntitySharedComponentCommand<T>(&m_Data->m_MainThreadChain, ECBCommand.SetSharedComponentData, e, hashCode, component);
        }

        /// <summary>
        /// Play back all recorded operations against an entity manager.
        /// </summary>
        /// <param name="mgr">The entity manager that will receive the operations</param>
        public void Playback(EntityManager mgr)
        {
            if (mgr == null)
                throw new NullReferenceException($"{nameof(mgr)} cannot be null");

            EnforceSingleThreadOwnership();

            if (!ShouldPlayback || m_Data == null)
                return;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Data->m_BufferSafety);
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Data->m_ArrayInvalidationSafety);
#endif

            Profiler.BeginSample("EntityCommandBuffer.Playback");

            PlaybackChain(mgr, &m_Data->m_MainThreadChain);

            var threadedChains = m_Data->m_ThreadedChains;
            if (threadedChains != null)
            {
                for (int i = 0; i < JobsUtility.MaxJobThreadCount; ++i)
                {
                    var chain = &threadedChains[i];
                    PlaybackChain(mgr, chain);
                }
            }

            Profiler.EndSample();
        }

        private static unsafe void PlaybackChain(EntityManager mgr, EntityCommandBufferChain* chain)
        {
            var head = chain->m_Head;

            Entity currentEntity = new Entity();

            int createEntityBatchOffset = 0;
            int instantiateEntityBatchOffset = 0;
            Entity* createEntityBatch = stackalloc Entity[EntityCommandBufferData.kMaxBatchCount];
            Entity* instantiateEntityBatch = stackalloc Entity[EntityCommandBufferData.kMaxBatchCount];

            while (head != null)
            {
                var off = 0;
                var buf = (byte*)head + sizeof(ECBChunk);

                while (off < head->Used)
                {
                    var header = (BasicCommand*)(buf + off);

                    switch ((ECBCommand)header->CommandType)
                    {
                        case ECBCommand.DestroyEntity:
                            {
                                mgr.DestroyEntity(((EntityCommand*)header)->Entity);
                            }
                            break;

                        case ECBCommand.RemoveComponent:
                            {
                                var cmd = (EntityComponentCommand*)header;
                                var entity = cmd->Header.Entity == Entity.Null ? currentEntity : cmd->Header.Entity;
                                mgr.RemoveComponent(entity, TypeManager.GetType(cmd->ComponentTypeIndex));
                            }
                            break;

                        case ECBCommand.CreateEntity:
                            {
                                var cmd = (CreateCommand*)header;

                                if (cmd->BatchCount > 0)
                                {
                                    createEntityBatchOffset = 0;
                                    EntityArchetype at = cmd->Archetype;
                                    
                                    if (!at.Valid)
                                    {
                                        at = mgr.CreateArchetype(null, 0);
                                    }
                                    
                                    mgr.CreateEntityInternal(at, createEntityBatch, cmd->BatchCount);
                                }

                                currentEntity = createEntityBatch[createEntityBatchOffset++];
                            }
                            break;
                        
                        case ECBCommand.InstantiateEntity:
                            {
                                var cmd = (EntityCommand*)header;
    
                                if (cmd->BatchCount > 0)
                                {
                                    instantiateEntityBatchOffset = 0;
                                    mgr.InstantiateInternal(cmd->Entity, instantiateEntityBatch, cmd->BatchCount);
                                }
    
                                currentEntity = instantiateEntityBatch[instantiateEntityBatchOffset++];
                            }
                            break;
                        case ECBCommand.AddComponent:
                            {
                                var cmd = (EntityComponentCommand*)header;
                                var componentType = (ComponentType)TypeManager.GetType(cmd->ComponentTypeIndex);
                                var entity = cmd->Header.Entity == Entity.Null ? currentEntity : cmd->Header.Entity;
                                mgr.AddComponent(entity, componentType);
                                mgr.SetComponentDataRaw(entity, cmd->ComponentTypeIndex, cmd + 1, cmd->ComponentSize);
                            }
                            break;

                        case ECBCommand.SetComponent:
                            {
                                var cmd = (EntityComponentCommand*)header;
                                var entity = cmd->Header.Entity == Entity.Null ? currentEntity : cmd->Header.Entity;
                                mgr.SetComponentDataRaw(entity, cmd->ComponentTypeIndex, cmd + 1, cmd->ComponentSize);
                            }
                            break;

                        case ECBCommand.AddBuffer:
                            {
                                var cmd = (EntityBufferCommand*)header;
                                var entity = cmd->Header.Entity == Entity.Null ? currentEntity : cmd->Header.Entity;
                                mgr.AddComponent(entity, ComponentType.FromTypeIndex(cmd->ComponentTypeIndex));
                                mgr.SetBufferRaw(entity, cmd->ComponentTypeIndex, &cmd->TempBuffer, cmd->ComponentSize);
                            }
                            break;

                        case ECBCommand.SetBuffer:
                            {
                                var cmd = (EntityBufferCommand*)header;
                                var entity = cmd->Header.Entity == Entity.Null ? currentEntity : cmd->Header.Entity;
                                mgr.SetBufferRaw(entity, cmd->ComponentTypeIndex, &cmd->TempBuffer, cmd->ComponentSize);
                            }
                            break;

                        case ECBCommand.AddSharedComponentData:
                            {
                                var cmd = (EntitySharedComponentCommand*)header;
                                var entity = cmd->Header.Entity == Entity.Null ? currentEntity : cmd->Header.Entity;
                                mgr.AddSharedComponentDataBoxed(entity, cmd->ComponentTypeIndex, cmd->HashCode,
                                    cmd->GetBoxedObject());
                            }
                            break;

                        case ECBCommand.SetSharedComponentData:
                            {
                                var cmd = (EntitySharedComponentCommand*)header;
                                var entity = cmd->Header.Entity == Entity.Null ? currentEntity : cmd->Header.Entity;
                                mgr.SetSharedComponentDataBoxed(entity, cmd->ComponentTypeIndex, cmd->HashCode,
                                    cmd->GetBoxedObject());
                            }
                            break;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        default:
                            throw new System.InvalidOperationException("Invalid command buffer");
#endif
                    }

                    off += header->TotalSize;
                }

                head = head->Next;
            }
        }

        /// <summary>
        /// Allows concurrent (non-deterministic) command buffer recording.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        unsafe public struct Concurrent
        {
			[NativeDisableUnsafePtrRestriction] EntityCommandBufferData* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            private AtomicSafetyHandle m_Safety;
#endif

		    [NativeSetThreadIndex]
			private int m_ThreadIndex;

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckWriteAccess()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            }

            unsafe public static implicit operator EntityCommandBuffer.Concurrent(EntityCommandBuffer buffer)
			{
				EntityCommandBuffer.Concurrent concurrent;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
				AtomicSafetyHandle.CheckWriteAndThrow(buffer.m_Safety);
				concurrent.m_Safety = buffer.m_Safety;
				AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety);
#endif
				concurrent.m_Data = buffer.m_Data;
				concurrent.m_ThreadIndex = 0;

                if (concurrent.m_Data != null)
                {
                    concurrent.m_Data->InitConcurrentAccess();
                }

				return concurrent;
			}

            private EntityCommandBufferChain* ThreadChain
            {
                get { return &m_Data->m_ThreadedChains[m_ThreadIndex]; }
            }

            public void CreateEntity()
            {
                CheckWriteAccess();
                m_Data->AddCreateCommand(ThreadChain, ECBCommand.CreateEntity, new EntityArchetype());
            }

            public void CreateEntity(EntityArchetype archetype)
            {
                CheckWriteAccess();
                m_Data->AddCreateCommand(ThreadChain, ECBCommand.CreateEntity, archetype);
            }

            public void Instantiate(Entity e)
            {
                CheckWriteAccess();
                m_Data->AddEntityCommand(ThreadChain, ECBCommand.InstantiateEntity, e);
            }

            public void DestroyEntity(Entity e)
            {
                CheckWriteAccess();
                m_Data->AddEntityCommand(ThreadChain, ECBCommand.DestroyEntity, e);
            }

            public void AddComponent<T>(Entity e, T component) where T : struct, IComponentData
            {
                CheckWriteAccess();
                m_Data->AddEntityComponentCommand(ThreadChain, ECBCommand.AddComponent, e, component);
            }

            public DynamicBuffer<T> AddBuffer<T>() where T : struct, IBufferElementData
            {
                return AddBuffer<T>(Entity.Null);
            }

            public DynamicBuffer<T> AddBuffer<T>(Entity e) where T : struct, IBufferElementData
            {
                CheckWriteAccess();
                return m_Data->CreateBufferCommand<T>(ECBCommand.AddBuffer, ThreadChain, e);
            }

            public DynamicBuffer<T> SetBuffer<T>() where T : struct, IBufferElementData
            {
                return SetBuffer<T>(Entity.Null);
            }

            public DynamicBuffer<T> SetBuffer<T>(Entity e) where T : struct, IBufferElementData
            {
                CheckWriteAccess();
                return m_Data->CreateBufferCommand<T>(ECBCommand.SetBuffer, ThreadChain, e);
            }

            public void AddComponent<T>(T component) where T : struct, IComponentData
            {
                AddComponent(Entity.Null, component);
            }

            public void SetComponent<T>(T component) where T : struct, IComponentData
            {
                SetComponent(Entity.Null, component);
            }

            public void SetComponent<T>(Entity e, T component) where T : struct, IComponentData
            {
                CheckWriteAccess();
                m_Data->AddEntityComponentCommand(ThreadChain, ECBCommand.SetComponent, e, component);
            }

            public void RemoveComponent<T>(Entity e)
            {
                RemoveComponent(e, ComponentType.Create<T>());
            }

            public void RemoveComponent(Entity e, ComponentType componentType)
            {
                CheckWriteAccess();
                m_Data->AddEntityComponentTypeCommand(ThreadChain, ECBCommand.RemoveComponent, e, componentType);
            }
            
            public void AddSharedComponent<T>(T component) where T : struct, ISharedComponentData
            {
                AddSharedComponent(Entity.Null, component);
            }

            public void AddSharedComponent<T>(Entity e, T component) where T : struct, ISharedComponentData
            {
                CheckWriteAccess();
                int hashCode;
                if (IsDefaultObject(ref component, out hashCode))
                    m_Data->AddEntitySharedComponentCommand<T>(ThreadChain, ECBCommand.AddSharedComponentData, e, hashCode, null);
                else
                    m_Data->AddEntitySharedComponentCommand<T>(ThreadChain, ECBCommand.AddSharedComponentData, e, hashCode, component);
            }

            public void SetSharedComponent<T>(T component) where T : struct, ISharedComponentData
            {
                SetSharedComponent(Entity.Null, component);
            }

            public void SetSharedComponent<T>(Entity e, T component) where T : struct, ISharedComponentData
            {
                CheckWriteAccess();
                int hashCode;
                if (IsDefaultObject(ref component, out hashCode))
                    m_Data->AddEntitySharedComponentCommand<T>(ThreadChain, ECBCommand.SetSharedComponentData, e, hashCode, null);
                else
                    m_Data->AddEntitySharedComponentCommand<T>(ThreadChain, ECBCommand.SetSharedComponentData, e, hashCode, component);
            }
        }
    }
}
