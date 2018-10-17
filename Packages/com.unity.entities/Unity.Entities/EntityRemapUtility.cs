using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using EntityOffsetInfo = Unity.Entities.TypeManager.EntityOffsetInfo;

namespace Unity.Entities
{
    internal static unsafe class EntityRemapUtility
    {
        public struct EntityRemapInfo
        {
            public int SourceVersion;
            public Entity Target;
        }

        public static void AddEntityRemapping(ref NativeArray<EntityRemapInfo> remapping, Entity source, Entity target)
        {
            remapping[source.Index] = new EntityRemapInfo { SourceVersion = source.Version, Target = target };
        }

        public static Entity RemapEntity(ref NativeArray<EntityRemapInfo> remapping, Entity source)
        {
            if (source.Version == remapping[source.Index].SourceVersion)
                return remapping[source.Index].Target;
            else
                return Entity.Null;
        }

        public struct EntityPatchInfo
        {
            public int Offset;
            public int Stride;
        }

        public struct BufferEntityPatchInfo
        {
            // Offset within chunk where first buffer header can be found
            public int BufferOffset;
            // Stride between adjacent buffers that need patching
            public int BufferStride;
            // Offset (from base pointer of array) where entities live
            public int ElementOffset;
            // Stride between adjacent buffer elements
            public int ElementStride;
        }

        public static EntityOffsetInfo[] CalculateEntityOffsets(Type type)
        {
            var offsets = new List<EntityOffsetInfo>();
            CalculateEntityOffsetsRecurse(ref offsets, type, 0);
            if (offsets.Count > 0)
                return offsets.ToArray();
            else
                return null;
        }

        static void CalculateEntityOffsetsRecurse(ref List<EntityOffsetInfo> offsets, Type type, int baseOffset)
        {
            if (type == typeof(Entity))
            {
                offsets.Add(new EntityOffsetInfo { Offset = baseOffset });
            }
            else
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var field in fields)
                {
                    if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive)
                        CalculateEntityOffsetsRecurse(ref offsets, field.FieldType, baseOffset + UnsafeUtility.GetFieldOffset(field));
                }
            }
        }

        public static EntityPatchInfo* AppendEntityPatches(EntityPatchInfo* patches, EntityOffsetInfo[] offsets, int baseOffset, int stride)
        {
            if (offsets == null)
                return patches;

              for (int i = 0; i < offsets.Length; i++)
                 patches[i] = new EntityPatchInfo { Offset = baseOffset + offsets[i].Offset, Stride = stride };
             return patches + offsets.Length;
        }

        public static BufferEntityPatchInfo* AppendBufferEntityPatches(BufferEntityPatchInfo* patches, EntityOffsetInfo[] offsets, int bufferBaseOffset, int bufferStride, int elementStride)
        {
            if (offsets == null)
                return patches;

            for (int i = 0; i < offsets.Length; i++)
            {
                patches[i] = new BufferEntityPatchInfo
                {
                    BufferOffset = bufferBaseOffset,
                    BufferStride = bufferStride,
                    ElementOffset = offsets[i].Offset,
                    ElementStride = elementStride,
                };
            }

            return patches + offsets.Length;
        }

        public static void PatchEntities(EntityPatchInfo* scalarPatches, int scalarPatchCount, BufferEntityPatchInfo* bufferPatches, int bufferPatchCount, byte* data, int count, ref NativeArray<EntityRemapInfo> remapping)
        {
            // Patch scalars (single components) with entity references.
            for (int i = 0; i < scalarPatchCount; i++)
            {
                byte* entityData = data + scalarPatches[i].Offset;
                for (int j = 0; j != count; j++)
                {
                    Entity* entity = (Entity*)entityData;
                    *entity = RemapEntity(ref remapping, *entity);
                    entityData += scalarPatches[i].Stride;
                }
            }

            // Patch buffers that contain entity references
            for (int i = 0; i < bufferPatchCount; ++i)
            {
                byte* bufferData = data + bufferPatches[i].BufferOffset;

                for (int j = 0; j != count; ++j)
                {
                    BufferHeader* header = (BufferHeader*) bufferData;

                    byte* elemsBase = BufferHeader.GetElementPointer(header) + bufferPatches[i].ElementOffset;
                    int elemCount = header->Length;

                    for (int k = 0; k != elemCount; ++k)
                    {
                        Entity* entityPtr = (Entity*) elemsBase;
                        *entityPtr = RemapEntity(ref remapping, *entityPtr);
                        elemsBase += bufferPatches[i].ElementStride;
                    }

                    bufferData += bufferPatches[i].BufferStride;
                }
            }

        }
    }
}
