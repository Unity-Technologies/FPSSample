using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Entities
{
    internal unsafe struct BufferHeader
    {
        public const int kMinimumCapacity = 8;

        public byte* Pointer;
        public int Length;
        public int Capacity;

        public static unsafe byte* GetElementPointer(BufferHeader* header)
        {
            if (header->Pointer != null)
                return header->Pointer;

            return (byte*) (header + 1);
        }

        public enum TrashMode
        {
            TrashOldData,
            RetainOldData
        }

        public static unsafe void EnsureCapacity(BufferHeader* header, int count, int typeSize, int alignment, TrashMode trashMode)
        {
            if (header->Capacity >= count)
                return;

            int newCapacity = Math.Max(Math.Max(2 * header->Capacity, count), kMinimumCapacity);
            long newBlockSize = newCapacity * typeSize;

            byte* oldData = GetElementPointer(header);
            byte* newData = (byte*) UnsafeUtility.Malloc(newBlockSize, alignment, Allocator.Persistent);

            if (trashMode == TrashMode.RetainOldData)
            {
                long oldBlockSize = header->Capacity * typeSize;
                UnsafeUtility.MemCpy(newData, oldData, oldBlockSize);
            }

            // Note we're freeing the old buffer only if it was not using the internal capacity. Don't change this to 'oldData', because that would be a bug.
            if (header->Pointer != null)
            {
                UnsafeUtility.Free(header->Pointer, Allocator.Persistent);
            }

            header->Pointer = newData;
            header->Capacity = newCapacity;
        }

        public static unsafe void Assign(BufferHeader* header, byte* source, int count, int typeSize, int alignment)
        {
            EnsureCapacity(header, count, typeSize, alignment, TrashMode.TrashOldData);

            // Select between internal capacity buffer and heap buffer.
            byte* elementPtr = GetElementPointer(header);

            UnsafeUtility.MemCpy(elementPtr, source, typeSize * count);

            header->Length = count;
        }

        public static void Initialize(BufferHeader* header, int bufferCapacity)
        {
            header->Pointer = null;
            header->Length = 0;
            header->Capacity = bufferCapacity;
        }

        public static void Destroy(BufferHeader* header)
        {
            if (header->Pointer != null)
            {
                UnsafeUtility.Free(header->Pointer, Allocator.Persistent);
            }

            Initialize(header, 0);
        }
    }
}
