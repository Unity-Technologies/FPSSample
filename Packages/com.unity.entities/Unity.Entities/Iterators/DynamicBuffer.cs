using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Entities
{
	[StructLayout(LayoutKind.Sequential)]
	[NativeContainer]
    public unsafe struct DynamicBuffer<T> where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        BufferHeader* m_Buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
	    internal AtomicSafetyHandle m_Safety;
	    internal AtomicSafetyHandle m_ArrayInvalidationSafety;
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal DynamicBuffer(BufferHeader* header, AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety)
        {
            m_Buffer = header;
            m_Safety = safety;
            m_ArrayInvalidationSafety = arrayInvalidationSafety;
        }
#else
        internal DynamicBuffer(BufferHeader* header)
        {
            m_Buffer = header;
        }
#endif

        public int Length
        {
            get { return m_Buffer->Length; }
        }

        public int Capacity
        {
            get { return m_Buffer->Capacity; }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckBounds(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if ((uint)index >= (uint)Length)
                throw new IndexOutOfRangeException($"Index {index} is out of range in DynamicBuffer of '{Length}' Length.");
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReadAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWriteAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }

        public T this [int index]
        {
            get
            {
                CheckReadAccess();
                CheckBounds(index);
                return UnsafeUtility.ReadArrayElement<T>(BufferHeader.GetElementPointer(m_Buffer), index);
            }
            set
            {
                CheckWriteAccess();
                CheckBounds(index);
                UnsafeUtility.WriteArrayElement<T>(BufferHeader.GetElementPointer(m_Buffer), index, value);
            }
        }

        public void ResizeUninitialized(int length)
        {
            InvalidateArrayAliases();
            BufferHeader.EnsureCapacity(m_Buffer, length, UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), BufferHeader.TrashMode.RetainOldData);
            m_Buffer->Length = length;
        }

        private void InvalidateArrayAliases()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_ArrayInvalidationSafety);
#endif
        }

        public void Clear()
        {
            m_Buffer->Length = 0;
        }

        public void TrimExcess()
        {
            byte* oldPtr = m_Buffer->Pointer;
            int length = m_Buffer->Length;

            if (length == Capacity || oldPtr == null)
                return;

            int elemSize = UnsafeUtility.SizeOf<T>();
            int elemAlign = UnsafeUtility.AlignOf<T>();

            byte* newPtr = (byte*) UnsafeUtility.Malloc(elemSize * length, elemAlign, Allocator.Persistent);
            UnsafeUtility.MemCpy(newPtr, oldPtr, elemSize * length);

            m_Buffer->Capacity = length;
            m_Buffer->Pointer = newPtr;

            UnsafeUtility.Free(oldPtr, Allocator.Persistent);
        }

        public void Add(T elem)
        {
            CheckWriteAccess();
            int length = Length;
            ResizeUninitialized(length + 1);
            this[length] = elem;
        }

        public void AddRange(NativeArray<T> newElems)
        {
            CheckWriteAccess();
            int elemSize = UnsafeUtility.SizeOf<T>();
            int oldLength = Length;
            ResizeUninitialized(oldLength + newElems.Length);

            byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);
            UnsafeUtility.MemCpy(basePtr + oldLength * elemSize, newElems.GetUnsafeReadOnlyPtr<T>(), elemSize * newElems.Length);
        }

        public void RemoveRange(int index, int count)
        {
            CheckWriteAccess();
            CheckBounds(index + count - 1);

            int elemSize = UnsafeUtility.SizeOf<T>();
            byte* basePtr = BufferHeader.GetElementPointer(m_Buffer);

            UnsafeUtility.MemMove(basePtr + index * elemSize, basePtr + (index + count) * elemSize, elemSize * (Length - count - index));

            m_Buffer->Length -= count;
        }

        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        public byte* GetBasePointer()
        {
            CheckWriteAccess();
            return BufferHeader.GetElementPointer(m_Buffer);
        }

        public DynamicBuffer<U> Reinterpret<U>() where U: struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (UnsafeUtility.SizeOf<U>() != UnsafeUtility.SizeOf<T>())
                throw new InvalidOperationException($"Types {typeof(U)} and {typeof(T)} are of different sizes; cannot reinterpret");
#endif
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBuffer<U>(m_Buffer, m_Safety, m_ArrayInvalidationSafety);
#else
            return new DynamicBuffer<U>(m_Buffer);
#endif
        }

        /// <summary>
        /// Return a native array that aliases the buffer contents. The array is only legal to access as long as the buffer is not reallocated.
        /// </summary>
        public NativeArray<T> ToNativeArray()
        {
            var shadow = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(GetBasePointer(), Length, Allocator.Invalid);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var handle = m_ArrayInvalidationSafety;
            AtomicSafetyHandle.UseSecondaryVersion(ref handle);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref shadow, handle);
#endif
            return shadow;
        }

        public void CopyFrom(NativeArray<T> v)
        {
            ResizeUninitialized(v.Length);
            ToNativeArray().CopyFrom(v);
        }

        public void CopyFrom(T[] v)
        {
            ResizeUninitialized(v.Length);
            ToNativeArray().CopyFrom(v);
        }
    }
}
