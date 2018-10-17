using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Assertions;
using Unity.Collections;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.Serialization
{
    public static class SerializeUtility
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct BufferPatchRecord
        {
            public int ChunkOffset;
            public int AllocSizeBytes;
        }

        public static int CurrentFileFormatVersion = 6;

        public static unsafe void DeserializeWorld(ExclusiveEntityTransaction manager, BinaryReader reader)
        {
            if (manager.ArchetypeManager.CountEntities() != 0)
            {
                throw new ArgumentException(
                    $"DeserializeWorld can only be used on completely empty EntityManager. Please create a new empty World and use EntityManager.MoveEntitiesFrom to move the loaded entities into the destination world instead.");
            }
            
            int storedVersion = reader.ReadInt();
            if (storedVersion != CurrentFileFormatVersion)
            {
                throw new ArgumentException(
                    $"Attempting to read a entity scene stored in an old file format version (stored version : {storedVersion}, current version : {CurrentFileFormatVersion})");
            }

            var types = ReadTypeArray(reader);
            var archetypes = ReadArchetypes(reader, types, manager, out var totalEntityCount);
            manager.AllocateConsecutiveEntitiesForLoading(totalEntityCount);

            int totalChunkCount = reader.ReadInt();

            for (int i = 0; i < totalChunkCount; ++i)
            {
                var chunk = (Chunk*) UnsafeUtility.Malloc(Chunk.kChunkSize, 64, Allocator.Persistent);
                reader.ReadBytes(chunk, Chunk.kChunkSize);

                chunk->Archetype = archetypes[(int)chunk->Archetype].Archetype;
                // Fixup the pointer to the shared component values
                // todo: more generic way of fixing up pointers?
                chunk->SharedComponentValueArray = (int*)((byte*)(chunk) + Chunk.GetSharedComponentOffset(chunk->Archetype->NumSharedComponents));
                chunk->ChangeVersion = (uint*) ((byte*) chunk +
                                                Chunk.GetChangedComponentOffset(chunk->Archetype->TypesCount,
                                                    chunk->Archetype->NumSharedComponents));

                // Allocate additional heap memory for buffers that have overflown into the heap, and read their data.
                int bufferAllocationCount = reader.ReadInt();
                if (bufferAllocationCount > 0)
                {
                    var bufferPatches = new NativeArray<BufferPatchRecord>(bufferAllocationCount, Allocator.Temp);
                    reader.ReadArray(bufferPatches, bufferPatches.Length);

                    // TODO: PERF: Batch malloc interface.
                    for (int pi = 0; pi < bufferAllocationCount; ++pi)
                    {
                        var target = (BufferHeader*)OffsetFromPointer(chunk->Buffer, bufferPatches[pi].ChunkOffset);

                        // TODO: Alignment
                        target->Pointer = (byte*) UnsafeUtility.Malloc(bufferPatches[pi].AllocSizeBytes, 8, Allocator.Persistent);

                        reader.ReadBytes(target->Pointer, bufferPatches[pi].AllocSizeBytes);
                    }

                    bufferPatches.Dispose();
                }

                manager.AddExistingChunk(chunk);
            }

            archetypes.Dispose();
        }

        private static unsafe NativeArray<EntityArchetype> ReadArchetypes(BinaryReader reader, NativeArray<int> types, ExclusiveEntityTransaction entityManager,
            out int totalEntityCount)
        {
            int archetypeCount = reader.ReadInt();
            var archetypes = new NativeArray<EntityArchetype>(archetypeCount, Allocator.Temp);
            var archetypeEntityCounts = new NativeArray<int>(archetypeCount, Allocator.Temp);
            totalEntityCount = 0;
            var tempComponentTypes = new NativeList<ComponentType>(Allocator.Temp);
            for (int i = 0; i < archetypeCount; ++i)
            {
                totalEntityCount += archetypeEntityCounts[i] = reader.ReadInt();
                int archetypeComponentTypeCount = reader.ReadInt();
                tempComponentTypes.Clear();
                for (int iType = 0; iType < archetypeComponentTypeCount; ++iType)
                {
                    int typeIndex = types[reader.ReadInt()];

                    tempComponentTypes.Add(ComponentType.FromTypeIndex(typeIndex));
                }

                archetypes[i] = entityManager.CreateArchetype((ComponentType*) tempComponentTypes.GetUnsafePtr(),
                    tempComponentTypes.Length);
            }

            tempComponentTypes.Dispose();
            types.Dispose();
            archetypeEntityCounts.Dispose();
            return archetypes;
        }

        private static NativeArray<int> ReadTypeArray(BinaryReader reader)
        {
            int typeCount = reader.ReadInt();
            var typeHashBuffer = new NativeArray<int>(typeCount, Allocator.Temp);

            reader.ReadArray(typeHashBuffer, typeCount);

            int nameBufferSize = reader.ReadInt();
            var nameBuffer = new NativeArray<byte>(nameBufferSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            reader.ReadBytes(nameBuffer, nameBufferSize);
            var types = new NativeArray<int>(typeCount, Allocator.Temp);
            int offset = 0;
            for (int i = 0; i < typeCount; ++i)
            {
                string typeName = StringFromNativeBytes(nameBuffer, offset);
                var type = Type.GetType(typeName);
                types[i] = TypeManager.GetTypeIndex(type);

                if (typeHashBuffer[i] != TypeManager.GetTypeInfo(types[i]).FastEqualityTypeInfo.Hash)
                {
                    throw new ArgumentException($"Type layout has changed: '{type.Name}'");
                }

                if (types[i] == 0)
                {
                    throw new ArgumentException("Unknown type '" + typeName + "'");
                }

                offset += typeName.Length + 1;
            }

            nameBuffer.Dispose();
            typeHashBuffer.Dispose();
            return types;
        }

        internal static unsafe void GetAllArchetypes(ArchetypeManager archetypeManager, out Dictionary<EntityArchetype, int> archetypeToIndex, out EntityArchetype[] archetypeArray)
        {
            var archetypeList = new List<EntityArchetype>();
            var currentArcheType = archetypeManager.m_LastArchetype;
            while (currentArcheType != null)
            {
                if (currentArcheType->EntityCount >= 0)
                {
                    archetypeList.Add(new EntityArchetype{Archetype = currentArcheType});
                }
                currentArcheType = currentArcheType->PrevArchetype;
            }
            //todo: sort archetypes to get deterministic indices
            archetypeToIndex = new Dictionary<EntityArchetype, int>();
            for (int i = 0; i < archetypeList.Count; ++i)
            {
                archetypeToIndex.Add(archetypeList[i],i);
            }

            archetypeArray = archetypeList.ToArray();
        }

        static unsafe void ClearUnusedChunkData(Chunk* chunk)
        {
            var arch = chunk->Archetype;
            int bufferSize = Chunk.GetChunkBufferSize(arch->TypesCount, arch->NumSharedComponents);
            byte* buffer = chunk->Buffer;
            int count = chunk->Count;

            for (int i = 0; i<arch->TypesCount-1; ++i)
            {
                int index = arch->TypeMemoryOrder[i];
                int nextIndex = arch->TypeMemoryOrder[i + 1];
                int startOffset = arch->Offsets[index] + count * arch->SizeOfs[index];
                int endOffset = arch->Offsets[nextIndex];
                UnsafeUtility.MemClear(buffer + startOffset, endOffset - startOffset);
            }
            int lastIndex = arch->TypeMemoryOrder[arch->TypesCount - 1];
            int lastStartOffset = arch->Offsets[lastIndex] + count * arch->SizeOfs[lastIndex];
            UnsafeUtility.MemClear(buffer + lastStartOffset, bufferSize - lastStartOffset);
        }

        public static unsafe void SerializeWorld(EntityManager entityManager, BinaryWriter writer, out int[] sharedComponentsToSerialize)
        {
            writer.Write(CurrentFileFormatVersion);
            var archetypeManager = entityManager.ArchetypeManager;

            GetAllArchetypes(archetypeManager, out var archetypeToIndex, out var archetypeArray);

            var typeindices = new HashSet<int>();
            foreach (var archetype in archetypeArray)
            {
                for (int iType = 0; iType < archetype.Archetype->TypesCount; ++iType)
                {
                    typeindices.Add(archetype.Archetype->Types[iType].TypeIndex);
                }
            }

            var typeArray = typeindices.Select(index =>
            {
                var type = TypeManager.GetType(index);
                var name = TypeManager.GetType(index).AssemblyQualifiedName;
                var hash = TypeManager.GetTypeInfo(index).FastEqualityTypeInfo.Hash;
                return new
                {
                    index,
                    type,
                    name,
                    hash,
                    asciiName = Encoding.ASCII.GetBytes(name)
                };
            }).OrderBy(t => t.name).ToArray();

            int typeNameBufferSize = typeArray.Sum(t => t.asciiName.Length + 1);
            writer.Write(typeArray.Length);
            foreach (var n in typeArray)
            {
                writer.Write(n.hash);
            }

            writer.Write(typeNameBufferSize);
            foreach(var n in typeArray)
            {
                writer.Write(n.asciiName);
                writer.Write((byte)0);
            }

            var typeIndexMap = new Dictionary<int, int>();
            for (int i = 0; i < typeArray.Length; ++i)
            {
                typeIndexMap[typeArray[i].index] = i;
            }

            WriteArchetypes(writer, archetypeArray, typeIndexMap);

            //TODO: ensure chunks are defragged?

            NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos;
            var bufferPatches = new NativeList<BufferPatchRecord>(128, Allocator.Temp);
            var totalChunkCount = GenerateRemapInfo(entityManager, archetypeArray, out entityRemapInfos);

            writer.Write(totalChunkCount);

            var tempChunk = (Chunk*)UnsafeUtility.Malloc(Chunk.kChunkSize, 16, Allocator.Temp);

            var sharedIndexToSerialize = new Dictionary<int, int>();
            for(int archetypeIndex = 0; archetypeIndex < archetypeArray.Length; ++archetypeIndex)
            {
                var archetype = archetypeArray[archetypeIndex].Archetype;
                for (var c = (Chunk*)archetype->ChunkList.Begin; c != archetype->ChunkList.End; c = (Chunk*)c->ChunkListNode.Next)
                {
                    bufferPatches.Clear();

                    UnsafeUtility.MemCpy(tempChunk, c, Chunk.kChunkSize);
                    tempChunk->SharedComponentValueArray = (int*)((byte*)(tempChunk) + Chunk.GetSharedComponentOffset(archetype->NumSharedComponents));

                    byte* tempChunkBuffer = tempChunk->Buffer;

                    // Find all buffer pointer locations and work out how much memory the deserializer must allocate on load.
                    for (int ti = 0; ti < archetype->TypesCount; ++ti)
                    {
                        int index = archetype->TypeMemoryOrder[ti];

                        if (!archetype->Types[index].IsBuffer)
                            continue;

                        int subArrayOffset = archetype->Offsets[index];
                        BufferHeader* header = (BufferHeader*) OffsetFromPointer(tempChunkBuffer, subArrayOffset);
                        int stride = archetype->SizeOfs[index];
                        int count = c->Count;
                        var ct = TypeManager.GetTypeInfo(archetype->Types[index].TypeIndex);

                        for (int bi = 0; bi < count; ++bi)
                        {
                            if (header->Pointer != null)
                            {
                                header->Pointer = null;
                                bufferPatches.Add(new BufferPatchRecord
                                {
                                    ChunkOffset = (int)(((byte*)header) - (byte*)tempChunkBuffer),
                                    AllocSizeBytes = ct.ElementSize * header->Capacity,
                                });
                            }
                            header = (BufferHeader*)OffsetFromPointer(header, stride);
                        }
                    }

                    EntityRemapUtility.PatchEntities(archetype->ScalarEntityPatches, archetype->ScalarEntityPatchCount, archetype->BufferEntityPatches, archetype->BufferEntityPatchCount, tempChunkBuffer, tempChunk->Count, ref entityRemapInfos);

                    ClearUnusedChunkData(tempChunk);
                    tempChunk->ChunkListNode.Next = null;
                    tempChunk->ChunkListNode.Prev = null;
                    tempChunk->ChunkListWithEmptySlotsNode.Next = null;
                    tempChunk->ChunkListWithEmptySlotsNode.Prev = null;
                    tempChunk->Archetype = (Archetype*) archetypeIndex;

                    if (archetype->NumManagedArrays != 0)
                    {
                        throw new ArgumentException("Serialization of GameObject components is not supported for pure entity scenes");
                    }

                    for (int i = 0; i != archetype->NumSharedComponents; i++)
                    {
                        int sharedComponentIndex = tempChunk->SharedComponentValueArray[i];
                        int newIndex;

                        if (tempChunk->SharedComponentValueArray[i] != 0)
                        {
                            if (sharedIndexToSerialize.TryGetValue(sharedComponentIndex, out newIndex))
                            {
                                tempChunk->SharedComponentValueArray[i] = newIndex;
                            }
                            else
                            {
                                // 0 is reserved for null types in shared components
                                newIndex = sharedIndexToSerialize.Count + 1;
                                sharedIndexToSerialize[sharedComponentIndex] = newIndex;

                                tempChunk->SharedComponentValueArray[i] = newIndex;
                            }
                        }
                    }

                    writer.WriteBytes(tempChunk, Chunk.kChunkSize);

                    writer.Write(bufferPatches.Length);

                    if (bufferPatches.Length > 0)
                    {
                        writer.WriteList(bufferPatches);

                        // Write heap backed data for each required patch.
                        // TODO: PERF: Investigate static-only deserialization could manage one block and mark in pointers somehow that they are not indiviual
                        for (int i = 0; i < bufferPatches.Length; ++i)
                        {
                            var patch = bufferPatches[i];
                            // NOTE that this reads the pointer from the original, unpatched chunk.
                            // We have nulled out the pointer in the serialized data above.
                            var header = (BufferHeader*)OffsetFromPointer(c->Buffer, patch.ChunkOffset);
                            writer.WriteBytes(header->Pointer, patch.AllocSizeBytes);
                        }
                    }
                }
            }

            bufferPatches.Dispose();
            entityRemapInfos.Dispose();
            UnsafeUtility.Free(tempChunk, Allocator.Temp);

            sharedComponentsToSerialize = new int[sharedIndexToSerialize.Count];

            foreach (var i in sharedIndexToSerialize)
                sharedComponentsToSerialize[i.Value - 1] = i.Key;
        }

        static unsafe byte* OffsetFromPointer(void* ptr, int offset)
        {
            return ((byte*)ptr) + offset;
        }

        static unsafe void WriteArchetypes(BinaryWriter writer, EntityArchetype[] archetypeArray, Dictionary<int, int> typeIndexMap)
        {
            writer.Write(archetypeArray.Length);

            foreach (var archetype in archetypeArray)
            {
                writer.Write(archetype.Archetype->EntityCount);
                writer.Write(archetype.Archetype->TypesCount - 1);
                for (int i = 1; i < archetype.Archetype->TypesCount; ++i)
                {
                    var componentType = archetype.Archetype->Types[i];
                    writer.Write(typeIndexMap[componentType.TypeIndex]);
                }
            }
        }

        static unsafe int GenerateRemapInfo(EntityManager entityManager, EntityArchetype[] archetypeArray, out NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos)
        {
            int nextEntityId = 1; //0 is reserved for Entity.Null;

            entityRemapInfos = new NativeArray<EntityRemapUtility.EntityRemapInfo>(entityManager.EntityCapacity, Allocator.Temp);

            int totalChunkCount = 0;
            for (int archetypeIndex = 0; archetypeIndex < archetypeArray.Length; ++archetypeIndex)
            {
                var archetype = archetypeArray[archetypeIndex].Archetype;
                for (var c = (Chunk*)archetype->ChunkList.Begin; c != archetype->ChunkList.End; c = (Chunk*)c->ChunkListNode.Next)
                {
                    for (int iEntity = 0; iEntity < c->Count; ++iEntity)
                    {
                        var entity = *(Entity*)ChunkDataUtility.GetComponentDataRO(c, iEntity, 0);
                        EntityRemapUtility.AddEntityRemapping(ref entityRemapInfos, entity, new Entity { Version = 0, Index = nextEntityId });
                        ++nextEntityId;
                    }

                    totalChunkCount += 1;
                }
            }

            return totalChunkCount;
        }

        static unsafe string StringFromNativeBytes(NativeArray<byte> bytes, int offset = 0)
        {
            return new string((sbyte*)bytes.GetUnsafePtr() + offset);
        }
    }
}
