using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Profiling;

namespace Unity.Entities
{
    internal struct ComponentTypeInArchetype
    {
        public readonly int TypeIndex;
        public readonly int BufferCapacity;

        public bool IsBuffer => BufferCapacity >= 0;
        public bool IsSystemStateComponent => TypeManager.IsSystemStateComponent(TypeIndex);
        public bool IsSystemStateSharedComponent => TypeManager.IsSystemStateSharedComponent(TypeIndex);
        public bool IsSharedComponent => TypeManager.IsSharedComponent(TypeIndex);
        public bool IsZeroSized => TypeManager.GetTypeInfo(TypeIndex).IsZeroSized;
        
        public ComponentTypeInArchetype(ComponentType type)
        {
            TypeIndex = type.TypeIndex;
            BufferCapacity = type.BufferCapacity;
        }

        public static bool operator == (ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs)
        {
            return lhs.TypeIndex == rhs.TypeIndex && lhs.BufferCapacity == rhs.BufferCapacity;
        }

        public static bool operator != (ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs)
        {
            return lhs.TypeIndex != rhs.TypeIndex || lhs.BufferCapacity != rhs.BufferCapacity;
        }

        public static bool operator < (ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs)
        {
            return lhs.TypeIndex != rhs.TypeIndex
                ? lhs.TypeIndex < rhs.TypeIndex
                : lhs.BufferCapacity < rhs.BufferCapacity;
        }

        public static bool operator > (ComponentTypeInArchetype lhs, ComponentTypeInArchetype rhs)
        {
            return lhs.TypeIndex != rhs.TypeIndex
                ? lhs.TypeIndex > rhs.TypeIndex
                : lhs.BufferCapacity > rhs.BufferCapacity;
        }

        public static unsafe bool CompareArray(ComponentTypeInArchetype* type1, int typeCount1,
            ComponentTypeInArchetype* type2, int typeCount2)
        {
            if (typeCount1 != typeCount2)
                return false;
            for (var i = 0; i < typeCount1; ++i)
                if (type1[i] != type2[i])
                    return false;
            return true;
        }

        public ComponentType ToComponentType()
        {
            ComponentType type;
            type.BufferCapacity = BufferCapacity;
            type.TypeIndex = TypeIndex;
            type.AccessModeType = ComponentType.AccessMode.ReadWrite;
            return type;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public override string ToString()
        {
            return ToComponentType().ToString();
        }
#endif
        public override bool Equals(object obj)
        {
            if (obj is ComponentTypeInArchetype) return (ComponentTypeInArchetype) obj == this;

            return false;
        }

        public override int GetHashCode()
        {
            return (TypeIndex * 5819) ^ BufferCapacity;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Chunk
    {
        // NOTE: Order of the UnsafeLinkedListNode is required to be this in order
        //       to allow for casting & grabbing Chunk* from nodes...
        public UnsafeLinkedListNode ChunkListNode; // 16 | 8
        public UnsafeLinkedListNode ChunkListWithEmptySlotsNode; // 32 | 16

        public Archetype* Archetype; // 40 | 20
        public int* SharedComponentValueArray; // 48 | 24

        // This is meant as read-only.
        // ArchetypeManager.SetChunkCount should be used to change the count.
        public int Count; // 52 | 28
        public int Capacity; // 56 | 32

        public int ManagedArrayIndex; // 60 | 36

        public int Padding0; // 64 | 40
        public uint* ChangeVersion; // 72 | 44
        public void* Padding2; // 80 | 48


        // Component data buffer
        public fixed byte Buffer[4];


        public const int kChunkSize = 16 * 1024;
        public const int kMaximumEntitiesPerChunk = kChunkSize / 8;

        public static int GetChunkBufferSize(int numComponents, int numSharedComponents)
        {
            var bufferSize = kChunkSize -
                             (sizeof(Chunk) - 4 + numSharedComponents * sizeof(int) + numComponents * sizeof(uint));
            return bufferSize;
        }

        public static int GetSharedComponentOffset(int numSharedComponents)
        {
            return kChunkSize - numSharedComponents * sizeof(int);
        }

        public static int GetChangedComponentOffset(int numComponents, int numSharedComponents)
        {
            return GetSharedComponentOffset(numSharedComponents) - numComponents * sizeof(uint);
        }

        public bool MatchesFilter(MatchingArchetypes* match, ref ComponentGroupFilter filter)
        {
            if ((filter.Type & FilterType.SharedComponent) != 0)
            {
                var sharedComponentsInChunk = SharedComponentValueArray;
                var filteredCount = filter.Shared.Count;

                fixed (int* indexInComponentGroupPtr = filter.Shared.IndexInComponentGroup, sharedComponentIndexPtr =
                    filter.Shared.SharedComponentIndex)
                {
                    for (var i = 0; i < filteredCount; ++i)
                    {
                        var indexInComponentGroup = indexInComponentGroupPtr[i];
                        var sharedComponentIndex = sharedComponentIndexPtr[i];
                        var componentIndexInArcheType = match->IndexInArchetype[indexInComponentGroup];
                        var componentIndexInChunk = match->Archetype->SharedComponentOffset[componentIndexInArcheType];
                        if (sharedComponentsInChunk[componentIndexInChunk] != sharedComponentIndex)
                            return false;
                    }
                }

                return true;
            }

            if ((filter.Type & FilterType.Changed) != 0)
            {
                var changedCount = filter.Changed.Count;

                var requiredVersion = filter.RequiredChangeVersion;
                fixed (int* indexInComponentGroupPtr = filter.Changed.IndexInComponentGroup)
                {
                    for (var i = 0; i < changedCount; ++i)
                    {
                        var indexInArchetype = match->IndexInArchetype[indexInComponentGroupPtr[i]];

                        var changeVersion = ChangeVersion[indexInArchetype];
                        if (ChangeVersionUtility.DidChange(changeVersion, requiredVersion))
                            return true;
                    }
                }

                return false;
            }

            return true;
        }

        public int GetSharedComponentIndex(MatchingArchetypes* match, int indexInComponentGroup)
        {
            var sharedComponentsInChunk = SharedComponentValueArray;

            var componentIndexInArcheType = match->IndexInArchetype[indexInComponentGroup];
            var componentIndexInChunk = match->Archetype->SharedComponentOffset[componentIndexInArcheType];
            return sharedComponentsInChunk[componentIndexInChunk];
        }
    }

    internal unsafe struct Archetype
    {
        public UnsafeLinkedListNode ChunkList;
        public UnsafeLinkedListNode ChunkListWithEmptySlots;
        public ChunkListMap FreeChunksBySharedComponents;

        public int EntityCount;
        public int ChunkCapacity;
        public int ChunkCount;

        public ComponentTypeInArchetype* Types;
        public int TypesCount;

        // Index matches archetype types
        public int* Offsets;
        public int* SizeOfs;

        // TypesCount indices into Types/Offsets/SizeOfs in the order that the
        // components are laid out in memory.
        public int* TypeMemoryOrder;

        public int* ManagedArrayOffset;
        public int NumManagedArrays;

        public int* SharedComponentOffset;
        public int NumSharedComponents;

        public Archetype* PrevArchetype;
        public Archetype* InstantiableArchetype;

        public EntityRemapUtility.EntityPatchInfo* ScalarEntityPatches;
        public int                                 ScalarEntityPatchCount;

        public EntityRemapUtility.BufferEntityPatchInfo* BufferEntityPatches;
        public int                                       BufferEntityPatchCount;

        public bool SystemStateCleanupComplete;
        public bool SystemStateCleanupNeeded;
        public bool Disabled;
        public bool Prefab;
    }

    internal unsafe class ArchetypeManager : IDisposable
    {
        private readonly UnsafeLinkedListNode* m_EmptyChunkPool;

        private readonly SharedComponentDataManager m_SharedComponentManager;
        private ChunkAllocator m_ArchetypeChunkAllocator;

        internal Archetype* m_LastArchetype;
        private ManagedArrayStorage[] m_ManagedArrays = new ManagedArrayStorage[1];
        private NativeMultiHashMap<uint, IntPtr> m_TypeLookup;
        // lastChunkWithSharedComponentsAllocatedInto is used to speed up allocations from the same archetype/
        // shared component combination, if this chunk matches we can avoid searching through all chunks belonging
        // to the archetype
        private Chunk* lastChunkWithSharedComponentsAllocatedInto;

        public ArchetypeManager(SharedComponentDataManager sharedComponentManager)
        {
            m_SharedComponentManager = sharedComponentManager;
            m_TypeLookup = new NativeMultiHashMap<uint, IntPtr>(256, Allocator.Persistent);

            m_EmptyChunkPool = (UnsafeLinkedListNode*) m_ArchetypeChunkAllocator.Allocate(sizeof(UnsafeLinkedListNode),
                UnsafeUtility.AlignOf<UnsafeLinkedListNode>());
            UnsafeLinkedListNode.InitializeList(m_EmptyChunkPool);

#if UNITY_ASSERTIONS
            // Buffer should be 16 byte aligned to ensure component data layout itself can gurantee being aligned
            var offset = UnsafeUtility.GetFieldOffset(typeof(Chunk).GetField("Buffer"));
            Assert.IsTrue(offset % 16 == 0, "Chunk buffer must be 16 byte aligned");
#endif
        }

        public void Dispose()
        {
            // Move all chunks to become pooled chunks
            while (m_LastArchetype != null)
            {
                while (!m_LastArchetype->ChunkList.IsEmpty)
                {
                    var chunk = (Chunk*)m_LastArchetype->ChunkList.Begin;
                    SetChunkCount(chunk, 0);
                }

                m_LastArchetype->FreeChunksBySharedComponents.Dispose();
                m_LastArchetype = m_LastArchetype->PrevArchetype;
            }

            // And all pooled chunks
            while (!m_EmptyChunkPool->IsEmpty)
            {
                var chunk = m_EmptyChunkPool->Begin;
                chunk->Remove();
                UnsafeUtility.Free(chunk, Allocator.Persistent);
            }

            m_ManagedArrays = null;
            m_TypeLookup.Dispose();
            m_ArchetypeChunkAllocator.Dispose();
        }

        private void DeallocateManagedArrayStorage(int index)
        {
            Assert.IsTrue(m_ManagedArrays[index].ManagedArray != null);
            m_ManagedArrays[index].ManagedArray = null;
        }

        private int AllocateManagedArrayStorage(int length)
        {
            for (var i = 0; i < m_ManagedArrays.Length; i++)
                if (m_ManagedArrays[i].ManagedArray == null)
                {
                    m_ManagedArrays[i].ManagedArray = new object[length];
                    return i;
                }

            var oldLength = m_ManagedArrays.Length;
            Array.Resize(ref m_ManagedArrays, m_ManagedArrays.Length * 2);

            m_ManagedArrays[oldLength].ManagedArray = new object[length];

            return oldLength;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AssertArchetypeComponents(ComponentTypeInArchetype* types, int count)
        {
            if (count < 1)
                throw new ArgumentException($"Invalid component count");
            if (types[0].TypeIndex == 0)
                throw new ArgumentException($"Component type may not be null");
            if (types[0].TypeIndex != TypeManager.GetTypeIndex<Entity>())
                throw new ArgumentException($"The Entity ID must always be the first component");

            for (var i = 1; i < count; i++)
            {
                if (types[i - 1].TypeIndex == types[i].TypeIndex)
                    throw new ArgumentException(
                        $"It is not allowed to have two components of the same type on the same entity. ({types[i - 1]} and {types[i]})");
            }
        }

        public Archetype* GetExistingArchetype(ComponentTypeInArchetype* types, int count)
        {
            IntPtr typePtr;
            NativeMultiHashMapIterator<uint> it;

            if (!m_TypeLookup.TryGetFirstValue(GetHash(types, count), out typePtr, out it))
                return null;

            do
            {
                var type = (Archetype*) typePtr;
                if (ComponentTypeInArchetype.CompareArray(type->Types, type->TypesCount, types, count))
                    return type;
            } while (m_TypeLookup.TryGetNextValue(out typePtr, ref it));

            return null;
        }

        private static uint GetHash(ComponentTypeInArchetype* types, int count)
        {
            var hash = HashUtility.Fletcher32((ushort*) types,
                count * sizeof(ComponentTypeInArchetype) / sizeof(ushort));
            return hash;
        }

        public Archetype* GetOrCreateArchetype(ComponentTypeInArchetype* types, int count,
            EntityGroupManager groupManager)
        {
            var srcArchetype = GetOrCreateArchetypeInternal(types, count, groupManager);

            var removedTypes = 0;
            var prefabTypeIndex = TypeManager.GetTypeIndex<Prefab>();
            for (var t = 0; t < srcArchetype->TypesCount; ++t)
            {
                var type = srcArchetype->Types[t];
                var skip = type.IsSystemStateComponent || type.IsSystemStateSharedComponent || (type.TypeIndex == prefabTypeIndex);
                if (skip)
                    ++removedTypes;
                else
                    types[t - removedTypes] = srcArchetype->Types[t];
            }

            srcArchetype->InstantiableArchetype = srcArchetype;
            if (removedTypes > 0)
            {
                var instantiableArchetype = GetOrCreateArchetypeInternal(types, count-removedTypes, groupManager);

                srcArchetype->InstantiableArchetype = instantiableArchetype;
                instantiableArchetype->InstantiableArchetype = instantiableArchetype;
            }

            return srcArchetype;
        }

        private Archetype* GetOrCreateArchetypeInternal(ComponentTypeInArchetype* types, int count, 
            EntityGroupManager groupManager)
        {
            var type = GetExistingArchetype(types, count);
            if (type != null)
                return type;

            AssertArchetypeComponents(types, count);

            // This is a new archetype, allocate it and add it to the hash map
            type = (Archetype*) m_ArchetypeChunkAllocator.Allocate(sizeof(Archetype), 8);
            type->TypesCount = count;
            type->Types =
                (ComponentTypeInArchetype*) m_ArchetypeChunkAllocator.Construct(
                    sizeof(ComponentTypeInArchetype) * count, 4, types);
            type->EntityCount = 0;
            type->ChunkCount = 0;

            type->NumSharedComponents = 0;
            type->SharedComponentOffset = null;

            var disabledTypeIndex = TypeManager.GetTypeIndex<Disabled>();
            var prefabTypeIndex = TypeManager.GetTypeIndex<Prefab>();
            type->Disabled = false;
            type->Prefab = false;
            for (var i = 0; i < count; ++i)
            {
                if (TypeManager.GetTypeInfo(types[i].TypeIndex).Category == TypeManager.TypeCategory.ISharedComponentData)
                    ++type->NumSharedComponents;
                if (types[i].TypeIndex == disabledTypeIndex)
                    type->Disabled = true;
                if (types[i].TypeIndex == prefabTypeIndex)
                    type->Prefab = true;
            }

            // Compute how many IComponentData types store Entities and need to be patched.
            // Types can have more than one entity, which means that this count is not necessarily
            // the same as the type count.
            int scalarEntityPatchCount = 0;
            int bufferEntityPatchCount = 0;
            for (var i = 0; i < count; ++i)
            {
                var ct = TypeManager.GetTypeInfo(types[i].TypeIndex);
                var entityOffsets = ct.EntityOffsets;
                if (entityOffsets == null)
                    continue;

                if (ct.BufferCapacity >= 0)
                {
                    bufferEntityPatchCount += entityOffsets.Length;
                }
                else
                {
                    scalarEntityPatchCount += entityOffsets.Length;

                }
            }

            var chunkDataSize = Chunk.GetChunkBufferSize(type->TypesCount, type->NumSharedComponents);

            // FIXME: proper alignment
            type->Offsets = (int*) m_ArchetypeChunkAllocator.Allocate(sizeof(int) * count, 4);
            type->SizeOfs = (int*) m_ArchetypeChunkAllocator.Allocate(sizeof(int) * count, 4);
            type->TypeMemoryOrder = (int*) m_ArchetypeChunkAllocator.Allocate(sizeof(int) * count, 4);
            type->ScalarEntityPatches = (EntityRemapUtility.EntityPatchInfo*) m_ArchetypeChunkAllocator.Allocate(sizeof(EntityRemapUtility.EntityPatchInfo) * scalarEntityPatchCount, 4);
            type->ScalarEntityPatchCount = scalarEntityPatchCount;
            type->BufferEntityPatches = (EntityRemapUtility.BufferEntityPatchInfo*) m_ArchetypeChunkAllocator.Allocate(sizeof(EntityRemapUtility.BufferEntityPatchInfo) * bufferEntityPatchCount, 4);
            type->BufferEntityPatchCount = bufferEntityPatchCount;

            var bytesPerInstance = 0;

            for (var i = 0; i < count; ++i)
            {
                var cType = TypeManager.GetTypeInfo(types[i].TypeIndex);
                var sizeOf = cType.SizeInChunk; // Note that this includes internal capacity and header overhead for buffers.
                type->SizeOfs[i] = sizeOf;

                bytesPerInstance += sizeOf;
            }

            type->ChunkCapacity = chunkDataSize / bytesPerInstance;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (bytesPerInstance > chunkDataSize)
                throw new ArgumentException(
                    $"Entity archetype component data is too large. The maximum component data is {chunkDataSize} but the component data is {bytesPerInstance}");

            Assert.IsTrue(Chunk.kMaximumEntitiesPerChunk >= type->ChunkCapacity);
#endif

            // For serialization a stable ordering of the components in the
            // chunk is desired. The type index is not stable, since it depends
            // on the order in which types are added to the TypeManager.
            // A permutation of the types ordered by a TypeManager-generated
            // memory ordering is used instead.
            var memoryOrderings = new NativeArray<UInt64>(count, Allocator.Temp);
            for (int i = 0; i < count; ++i)
                memoryOrderings[i] = TypeManager.GetTypeInfo(types[i].TypeIndex).MemoryOrdering;
            for (int i = 0; i < count; ++i)
            {
                int index = i;
                while (index > 1 && memoryOrderings[i] < memoryOrderings[type->TypeMemoryOrder[index - 1]])
                {
                    type->TypeMemoryOrder[index] = type->TypeMemoryOrder[index - 1];
                    --index;
                }
                type->TypeMemoryOrder[index] = i;
            }
            memoryOrderings.Dispose();

            var usedBytes = 0;
            for (var i = 0; i < count; ++i)
            {
                var index = type->TypeMemoryOrder[i];
                var sizeOf = type->SizeOfs[index];

                type->Offsets[index] = usedBytes;

                usedBytes += sizeOf * type->ChunkCapacity;
            }

            type->NumManagedArrays = 0;
            type->ManagedArrayOffset = null;

            for (var i = 0; i < count; ++i)
                if (TypeManager.GetTypeInfo(types[i].TypeIndex).Category == TypeManager.TypeCategory.Class)
                    ++type->NumManagedArrays;

            if (type->NumManagedArrays > 0)
            {
                type->ManagedArrayOffset = (int*) m_ArchetypeChunkAllocator.Allocate(sizeof(int) * count, 4);
                var mi = 0;
                for (var i = 0; i < count; ++i)
                {
                    var cType = TypeManager.GetTypeInfo(types[i].TypeIndex);
                    if (cType.Category == TypeManager.TypeCategory.Class)
                        type->ManagedArrayOffset[i] = mi++;
                    else
                        type->ManagedArrayOffset[i] = -1;
                }
            }

            if (type->NumSharedComponents > 0)
            {
                type->SharedComponentOffset = (int*) m_ArchetypeChunkAllocator.Allocate(sizeof(int) * count, 4);
                var mi = 0;
                for (var i = 0; i < count; ++i)
                {
                    var cType = TypeManager.GetTypeInfo(types[i].TypeIndex);
                    if (cType.Category == TypeManager.TypeCategory.ISharedComponentData)
                        type->SharedComponentOffset[i] = mi++;
                    else
                        type->SharedComponentOffset[i] = -1;
                }
            }

            // Fill in arrays of scalar and buffer entity patches
            var scalarPatchInfo = type->ScalarEntityPatches;
            var bufferPatchInfo = type->BufferEntityPatches;
            for (var i = 0; i != count; i++)
            {
                var ct = TypeManager.GetTypeInfo(types[i].TypeIndex);
                var offsets = ct.EntityOffsets;
                if (ct.BufferCapacity >= 0)
                {
                    bufferPatchInfo = EntityRemapUtility.AppendBufferEntityPatches(bufferPatchInfo, offsets, type->Offsets[i], type->SizeOfs[i], ct.ElementSize);
                }
                else
                {
                    scalarPatchInfo = EntityRemapUtility.AppendEntityPatches(scalarPatchInfo, offsets, type->Offsets[i], type->SizeOfs[i]);
                }
            }
            type->ScalarEntityPatchCount = scalarEntityPatchCount;
            type->BufferEntityPatchCount = bufferEntityPatchCount;

            // Update the list of all created archetypes
            type->PrevArchetype = m_LastArchetype;
            m_LastArchetype = type;

            UnsafeLinkedListNode.InitializeList(&type->ChunkList);
            UnsafeLinkedListNode.InitializeList(&type->ChunkListWithEmptySlots);
            type->FreeChunksBySharedComponents.Init(8);

            m_TypeLookup.Add(GetHash(types, count), (IntPtr) type);

            type->SystemStateCleanupComplete = ArchetypeSystemStateCleanupComplete(type);
            type->SystemStateCleanupNeeded = ArchetypeSystemStateCleanupNeeded(type);

            groupManager.OnArchetypeAdded(type);

            return type;
        }

        private bool ArchetypeSystemStateCleanupComplete(Archetype* archetype)
        {
            if (archetype->TypesCount == 2 && archetype->Types[1].TypeIndex == TypeManager.GetTypeIndex<CleanupEntity>()) return true;
            return false;
        }

        private bool ArchetypeSystemStateCleanupNeeded(Archetype* archetype)
        {
            for (var t = 1; t < archetype->TypesCount; ++t)
            {
                var type = archetype->Types[t];
                if (type.IsSystemStateComponent || type.IsSystemStateSharedComponent)
                {
                    return true;
                }
            }

            return false;
        }

        public static Chunk* GetChunkFromEmptySlotNode(UnsafeLinkedListNode* node)
        {
            return (Chunk*) (node - 1);
        }

        public void AddExistingChunk(Chunk* chunk)
        {
            var archetype = chunk->Archetype;
            archetype->ChunkList.Add(&chunk->ChunkListNode);
            archetype->ChunkCount += 1;
            archetype->EntityCount += chunk->Count;
            for (var i = 0; i < archetype->NumSharedComponents; ++i)
                m_SharedComponentManager.AddReference(chunk->SharedComponentValueArray[i]);

            if (chunk->Count < chunk->Capacity)
            {
                if (archetype->NumSharedComponents == 0)
                {
                    archetype->ChunkListWithEmptySlots.Add(&chunk->ChunkListWithEmptySlotsNode);
                }
                else
                {
                    archetype->FreeChunksBySharedComponents.Add(chunk);
                }
            }
        }

        public void ConstructChunk(Archetype* archetype, Chunk* chunk, int* sharedComponentDataIndices)
        {
            chunk->Archetype = archetype;

            chunk->Count = 0;
            chunk->Capacity = archetype->ChunkCapacity;
            chunk->ChunkListNode = new UnsafeLinkedListNode();
            chunk->ChunkListWithEmptySlotsNode = new UnsafeLinkedListNode();

            var numSharedComponents = archetype->NumSharedComponents;
            var numTypes = archetype->TypesCount;
            var sharedComponentOffset = Chunk.GetSharedComponentOffset(numSharedComponents);
            var changeVersionOffset = Chunk.GetChangedComponentOffset(numTypes, numSharedComponents);

            chunk->SharedComponentValueArray = (int*) ((byte*) chunk + sharedComponentOffset);
            chunk->ChangeVersion = (uint*) ((byte*) chunk + changeVersionOffset);

            archetype->ChunkList.Add(&chunk->ChunkListNode);
            archetype->ChunkCount += 1;

            Assert.IsTrue(!archetype->ChunkList.IsEmpty);
            Assert.IsTrue(chunk == (Chunk*) archetype->ChunkList.Back);

            if (numSharedComponents == 0)
            {
                archetype->ChunkListWithEmptySlots.Add(&chunk->ChunkListWithEmptySlotsNode);
                Assert.IsTrue(chunk == GetChunkFromEmptySlotNode(archetype->ChunkListWithEmptySlots.Back));
                Assert.IsTrue(!archetype->ChunkListWithEmptySlots.IsEmpty);
            }
            else
            {
                var sharedComponentValueArray = chunk->SharedComponentValueArray;
                UnsafeUtility.MemCpy(sharedComponentValueArray, sharedComponentDataIndices, archetype->NumSharedComponents*sizeof(int));

                for (var i = 0; i < archetype->NumSharedComponents; ++i)
                {
                    var sharedComponentIndex = sharedComponentValueArray[i];
                    m_SharedComponentManager.AddReference(sharedComponentIndex);
                }

                archetype->FreeChunksBySharedComponents.Add(chunk);
                Assert.IsTrue(archetype->FreeChunksBySharedComponents.GetChunkWithEmptySlots(sharedComponentDataIndices, archetype->NumSharedComponents) != null);
            }

            if (archetype->NumManagedArrays > 0)
                chunk->ManagedArrayIndex = AllocateManagedArrayStorage(archetype->NumManagedArrays * chunk->Capacity);
            else
                chunk->ManagedArrayIndex = -1;

            for (var i = 0; i < archetype->TypesCount; i++)
                chunk->ChangeVersion[i] = 0;
        }

        private static bool ChunkHasSharedComponents(Chunk* chunk, int* sharedComponentDataIndices)
        {
            var sharedComponentValueArray = chunk->SharedComponentValueArray;
            var numSharedComponents = chunk->Archetype->NumSharedComponents;
            return UnsafeUtility.MemCmp(sharedComponentDataIndices, sharedComponentValueArray, numSharedComponents * sizeof(int)) == 0;
        }

        public Chunk* GetChunkWithEmptySlots(Archetype* archetype, int* sharedComponentDataIndices)
        {
            if (archetype->NumSharedComponents == 0)
            {
                if (!archetype->ChunkListWithEmptySlots.IsEmpty)
                {
                    var chunk = GetChunkFromEmptySlotNode(archetype->ChunkListWithEmptySlots.Begin);
                    Assert.AreNotEqual(chunk->Count, chunk->Capacity);
                    return chunk;
                }
            }
            else
            {
                var chunk = archetype->FreeChunksBySharedComponents.GetChunkWithEmptySlots(sharedComponentDataIndices,
                    archetype->NumSharedComponents);
                if (chunk != null)
                {
                    return chunk;
                }
            }

            // Try existing archetype chunks
            if (!archetype->ChunkListWithEmptySlots.IsEmpty)
            {
                if (lastChunkWithSharedComponentsAllocatedInto != null &&
                    lastChunkWithSharedComponentsAllocatedInto->Archetype == archetype &&
                    lastChunkWithSharedComponentsAllocatedInto->Count < lastChunkWithSharedComponentsAllocatedInto->Capacity)
                {
                    if (ChunkHasSharedComponents(lastChunkWithSharedComponentsAllocatedInto, sharedComponentDataIndices))
                    {
                        return lastChunkWithSharedComponentsAllocatedInto;
                    }
                }

                if (archetype->NumSharedComponents == 0)
                {
                    var chunk = GetChunkFromEmptySlotNode(archetype->ChunkListWithEmptySlots.Begin);
                    Assert.AreNotEqual(chunk->Count, chunk->Capacity);
                    return chunk;
                }
            }

            Chunk* newChunk;
            // Try empty chunk pool
            if (m_EmptyChunkPool->IsEmpty)
            {
                // Allocate new chunk
                newChunk = (Chunk*)UnsafeUtility.Malloc(Chunk.kChunkSize, 64, Allocator.Persistent);
            }
            else
            {
                newChunk = (Chunk*) m_EmptyChunkPool->Begin;
                newChunk->ChunkListNode.Remove();
            }

            ConstructChunk(archetype, newChunk, sharedComponentDataIndices);

            if (archetype->NumSharedComponents > 0)
            {
                lastChunkWithSharedComponentsAllocatedInto = newChunk;
            }
            return newChunk;
        }

        public int AllocateIntoChunk(Chunk* chunk)
        {
            int outIndex;
            var res = AllocateIntoChunk(chunk, 1, out outIndex);
            Assert.AreEqual(1, res);
            return outIndex;
        }

        public int AllocateIntoChunk(Chunk* chunk, int count, out int outIndex)
        {
            var allocatedCount = Math.Min(chunk->Capacity - chunk->Count, count);
            outIndex = chunk->Count;
            SetChunkCount(chunk, chunk->Count + allocatedCount);
            chunk->Archetype->EntityCount += allocatedCount;
            return allocatedCount;
        }

        public void SetChunkCount(Chunk* chunk, int newCount)
        {
            Assert.AreNotEqual(newCount, chunk->Count);

            var capacity = chunk->Capacity;

            // Chunk released to empty chunk pool
            if (newCount == 0)
            {
                // Remove references to shared components
                if (chunk->Archetype->NumSharedComponents > 0)
                {
                    var sharedComponentValueArray = chunk->SharedComponentValueArray;

                    for (var i = 0; i < chunk->Archetype->NumSharedComponents; ++i)
                        m_SharedComponentManager.RemoveReference(sharedComponentValueArray[i]);
                }

                if (chunk->ManagedArrayIndex != -1)
                {
                    DeallocateManagedArrayStorage(chunk->ManagedArrayIndex);
                    chunk->ManagedArrayIndex = -1;
                }

                chunk->Archetype->ChunkCount -= 1;
                chunk->Archetype = null;
                chunk->ChunkListNode.Remove();
                chunk->ChunkListWithEmptySlotsNode.Remove();

                m_EmptyChunkPool->Add(&chunk->ChunkListNode);
            }
            // Chunk is now full
            else if (newCount == capacity)
            {
                chunk->ChunkListWithEmptySlotsNode.Remove();
            }
            // Chunk is no longer full
            else if (chunk->Count == capacity)
            {
                Assert.IsTrue(newCount < chunk->Count);
                if (chunk->Archetype->NumSharedComponents == 0)
                    chunk->Archetype->ChunkListWithEmptySlots.Add(&chunk->ChunkListWithEmptySlotsNode);
                else
                    chunk->Archetype->FreeChunksBySharedComponents.Add(chunk);

            }

            chunk->Count = newCount;
        }

        public object GetManagedObject(Chunk* chunk, ComponentType type, int index)
        {
            var typeOfs = ChunkDataUtility.GetIndexInTypeArray(chunk->Archetype, type.TypeIndex);
            if (typeOfs < 0 || chunk->Archetype->ManagedArrayOffset[typeOfs] < 0)
                throw new InvalidOperationException("Trying to get managed object for non existing component");
            return GetManagedObject(chunk, typeOfs, index);
        }

        internal object GetManagedObject(Chunk* chunk, int type, int index)
        {
            var managedStart = chunk->Archetype->ManagedArrayOffset[type] * chunk->Capacity;
            return m_ManagedArrays[chunk->ManagedArrayIndex].ManagedArray[index + managedStart];
        }

        public object[] GetManagedObjectRange(Chunk* chunk, int type, out int rangeStart, out int rangeLength)
        {
            rangeStart = chunk->Archetype->ManagedArrayOffset[type] * chunk->Capacity;
            rangeLength = chunk->Count;
            return m_ManagedArrays[chunk->ManagedArrayIndex].ManagedArray;
        }

        public void SetManagedObject(Chunk* chunk, int type, int index, object val)
        {
            var managedStart = chunk->Archetype->ManagedArrayOffset[type] * chunk->Capacity;
            m_ManagedArrays[chunk->ManagedArrayIndex].ManagedArray[index + managedStart] = val;
        }

        public void SetManagedObject(Chunk* chunk, ComponentType type, int index, object val)
        {
            var typeOfs = ChunkDataUtility.GetIndexInTypeArray(chunk->Archetype, type.TypeIndex);
            if (typeOfs < 0 || chunk->Archetype->ManagedArrayOffset[typeOfs] < 0)
                throw new InvalidOperationException("Trying to set managed object for non existing component");
            SetManagedObject(chunk, typeOfs, index, val);
        }

        public int CountEntities()
        {
            int entityCount = 0;
            var archetype = m_LastArchetype;
            while (archetype != null)
            {
                entityCount += archetype->EntityCount; 
                archetype = archetype->PrevArchetype;
            }

            return entityCount;
        }

        [BurstCompile]
        struct MoveChunksJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public EntityDataManager* srcEntityDataManager;
            [NativeDisableUnsafePtrRestriction]
            public EntityDataManager* dstEntityDataManager;
            public NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping;

            public void Execute()
            {
                dstEntityDataManager->AllocateEntitiesForRemapping(srcEntityDataManager, ref entityRemapping);
                srcEntityDataManager->FreeAllEntities();
            }
        }

        struct RemapChunk
        {
            public Chunk* chunk;
            public Archetype* dstArchetype;
        }

        [BurstCompile]
        struct RemapChunksJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<RemapChunk> remapChunks;
            [ReadOnly] public NativeArray<int> remapShared;

            [NativeDisableUnsafePtrRestriction]
            public EntityDataManager* dstEntityDataManager;

            public void Execute(int index)
            {
                Chunk* chunk = remapChunks[index].chunk;
                Archetype* dstArchetype = remapChunks[index].dstArchetype;

                dstEntityDataManager->RemapChunk(dstArchetype, chunk, 0, chunk->Count, ref entityRemapping);
                EntityRemapUtility.PatchEntities(dstArchetype->ScalarEntityPatches + 1, dstArchetype->ScalarEntityPatchCount - 1, dstArchetype->BufferEntityPatches, dstArchetype->BufferEntityPatchCount, chunk->Buffer, chunk->Count, ref entityRemapping);
                chunk->Archetype = dstArchetype;

                for (int i = 0; i < dstArchetype->NumSharedComponents; ++i)
                {
                    var componentIndex = chunk->SharedComponentValueArray[i];
                    componentIndex = remapShared[componentIndex];
                    chunk->SharedComponentValueArray[i] = componentIndex;
                }
            }
        }

        struct RemapArchetype
        {
            public Archetype* srcArchetype;
            public Archetype* dstArchetype;
        }

        [BurstCompile]
        struct RemapArchetypesJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<RemapArchetype> remapArchetypes;

            // This must be run after chunks have been remapped since FreeChunksBySharedComponents needs the shared component
            // indices in the chunks to be remapped
            public void Execute(int index)
            {
                var srcArchetype = remapArchetypes[index].srcArchetype;
                var dstArchetype = remapArchetypes[index].dstArchetype;

                UnsafeLinkedListNode.InsertListBefore(dstArchetype->ChunkList.End, &srcArchetype->ChunkList);

                if (srcArchetype->NumSharedComponents == 0)
                {
                    if (!srcArchetype->ChunkListWithEmptySlots.IsEmpty)
                        UnsafeLinkedListNode.InsertListBefore(dstArchetype->ChunkListWithEmptySlots.End,
                            &srcArchetype->ChunkListWithEmptySlots);
                }
                else
                {
                    remapArchetypes[index].dstArchetype->FreeChunksBySharedComponents.AppendFrom(&remapArchetypes[index].srcArchetype->FreeChunksBySharedComponents);
                }

                dstArchetype->EntityCount += srcArchetype->EntityCount;
                dstArchetype->ChunkCount += srcArchetype->ChunkCount;
                srcArchetype->EntityCount = 0;
                srcArchetype->ChunkCount = 0;
            }
        }

        public static void MoveChunks(EntityManager srcEntities, ArchetypeManager dstArchetypeManager,
            EntityGroupManager dstGroupManager, EntityDataManager* dstEntityDataManager, SharedComponentDataManager dstSharedComponents, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            var srcArchetypeManager = srcEntities.ArchetypeManager;
            var srcEntityDataManager = srcEntities.Entities;
            var srcSharedComponents = srcEntities.m_SharedComponentManager;

            var entityRemapping = new NativeArray<EntityRemapUtility.EntityRemapInfo>(srcEntityDataManager->Capacity, Allocator.TempJob);

            var moveChunksJob = new MoveChunksJob
            {
                srcEntityDataManager = srcEntityDataManager,
                dstEntityDataManager = dstEntityDataManager,
                entityRemapping = entityRemapping
            }.Schedule();
            JobHandle.ScheduleBatchedJobs();

            var samplerShared = CustomSampler.Create("MoveAllSharedComponents");
            samplerShared.Begin();
            var remapShared = dstSharedComponents.MoveAllSharedComponents(srcSharedComponents, Allocator.TempJob);
            samplerShared.End();

            Archetype* srcArchetype;

            int chunkCount = 0;
            int archetypeCount = 0;
            srcArchetype = srcArchetypeManager.m_LastArchetype;
            while (srcArchetype != null)
            {
                archetypeCount++;
                chunkCount += srcArchetype->ChunkCount;
                srcArchetype = srcArchetype->PrevArchetype;
            }

            var remapChunks = new NativeArray<RemapChunk>(chunkCount, Allocator.TempJob);
            var remapArchetypes = new NativeArray<RemapArchetype>(archetypeCount, Allocator.TempJob);

            int chunkIndex = 0;
            int archetypeIndex = 0;
            srcArchetype = srcArchetypeManager.m_LastArchetype;
            while (srcArchetype != null)
            {
                if (srcArchetype->ChunkCount != 0)
                {
                    if (srcArchetype->NumManagedArrays != 0)
                        throw new ArgumentException("MoveEntitiesFrom is not supported with managed arrays");
                    
                    // Make copy. GetOrCreateArchetype writes to type array.
                    UnsafeUtility.MemCpy(componentTypeInArchetypeArray, srcArchetype->Types, UnsafeUtility.SizeOf<ComponentTypeInArchetype>() * srcArchetype->TypesCount);
                    
                    var dstArchetype = dstArchetypeManager.GetOrCreateArchetype(componentTypeInArchetypeArray, srcArchetype->TypesCount, dstGroupManager);

                    remapArchetypes[archetypeIndex] = new RemapArchetype {srcArchetype = srcArchetype, dstArchetype = dstArchetype};

                    for (var c = srcArchetype->ChunkList.Begin; c != srcArchetype->ChunkList.End; c = c->Next)
                    {
                        remapChunks[chunkIndex] = new RemapChunk { chunk = (Chunk*)c, dstArchetype = dstArchetype };
                        chunkIndex++;
                    }

                    archetypeIndex++;

                    dstEntityDataManager->IncrementComponentTypeOrderVersion(dstArchetype);
                }

                srcArchetype  = srcArchetype->PrevArchetype;
            }

            var remapChunksJob = new RemapChunksJob
            {
                dstEntityDataManager = dstEntityDataManager,
                remapChunks = remapChunks,
                remapShared = remapShared,
                entityRemapping = entityRemapping
            }.Schedule(remapChunks.Length, 1, moveChunksJob);

            var remapArchetypesJob = new RemapArchetypesJob
            {
                remapArchetypes = remapArchetypes
            }.Schedule(archetypeIndex, 1, remapChunksJob);

            remapArchetypesJob.Complete();

            entityRemapping.Dispose();
            remapShared.Dispose();
        }

        public int CheckInternalConsistency()
        {
            var archetype = m_LastArchetype;
            var totalCount = 0;
            while (archetype != null)
            {
                var countInArchetype = 0;
                var chunkCount = 0;
                for (var c = archetype->ChunkList.Begin; c != archetype->ChunkList.End; c = c->Next)
                {
                    var chunk = (Chunk*) c;
                    Assert.IsTrue(chunk->Archetype == archetype);
                    Assert.IsTrue(chunk->Capacity >= chunk->Count);
                    Assert.AreEqual(chunk->ChunkListWithEmptySlotsNode.IsInList, chunk->Capacity != chunk->Count);

                    countInArchetype += chunk->Count;
                    chunkCount++;
                }

                Assert.AreEqual(countInArchetype, archetype->EntityCount);
                Assert.AreEqual(chunkCount, archetype->ChunkCount);

                totalCount += countInArchetype;
                archetype = archetype->PrevArchetype;
            }

            return totalCount;
        }

        internal SharedComponentDataManager GetSharedComponentDataManager()
        {
            return m_SharedComponentManager;
        }

        private struct ManagedArrayStorage
        {
            public object[] ManagedArray;
        }
    }
}
