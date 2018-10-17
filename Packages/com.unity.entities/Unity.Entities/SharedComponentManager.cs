using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Entities
{
    internal class SharedComponentDataManager
    {
        private NativeMultiHashMap<int, int> m_HashLookup = new NativeMultiHashMap<int, int>(128, Allocator.Persistent);

        private List<object>    m_SharedComponentData = new List<object>();
        private NativeList<int> m_SharedComponentRefCount = new NativeList<int>(0, Allocator.Persistent);
        private NativeList<int> m_SharedComponentType = new NativeList<int>(0, Allocator.Persistent);
        private NativeList<int> m_SharedComponentVersion = new NativeList<int>(0, Allocator.Persistent);
        private int             m_FreeListIndex;

        public SharedComponentDataManager()
        {
            m_SharedComponentData.Add(null);
            m_SharedComponentRefCount.Add(1);
            m_SharedComponentVersion.Add(1);
            m_SharedComponentType.Add(-1);
            m_FreeListIndex = -1;
        }

        public void Dispose()
        {
            if (!IsEmpty())
                Debug.LogWarning("SharedComponentData manager should be empty on shutdown");

            m_SharedComponentType.Dispose();
            m_SharedComponentRefCount.Dispose();
            m_SharedComponentVersion.Dispose();
            m_SharedComponentData.Clear();
            m_SharedComponentData = null;
            m_HashLookup.Dispose();
        }

        public void GetAllUniqueSharedComponents<T>(List<T> sharedComponentValues)
            where T : struct, ISharedComponentData
        {
            sharedComponentValues.Add(default(T));
            for (var i = 1; i != m_SharedComponentData.Count; i++)
            {
                var data = m_SharedComponentData[i];
                if (data != null && data.GetType() == typeof(T))
                    sharedComponentValues.Add((T) m_SharedComponentData[i]);
            }
        }

        public void GetAllUniqueSharedComponents<T>(List<T> sharedComponentValues, List<int> sharedComponentIndices)
            where T : struct, ISharedComponentData
        {
            sharedComponentValues.Add(default(T));
            sharedComponentIndices.Add(0);
            for (var i = 1; i != m_SharedComponentData.Count; i++)
            {
                var data = m_SharedComponentData[i];
                if (data != null && data.GetType() == typeof(T))
                {
                    sharedComponentValues.Add((T) m_SharedComponentData[i]);
                    sharedComponentIndices.Add(i);
                }
            }
        }

        public int GetSharedComponentCount()
        {
            return m_SharedComponentData.Count;
        }

        public int InsertSharedComponent<T>(T newData) where T : struct
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var index = FindSharedComponentIndex(TypeManager.GetTypeIndex<T>(), newData);

            if (index == 0) return 0;

            if (index != -1)
            {
                m_SharedComponentRefCount[index]++;
                return index;
            }

            var typeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
            var hashcode = FastEquality.GetHashCode(ref newData, typeInfo);
            return Add(typeIndex, hashcode, newData);
        }

        private unsafe int FindSharedComponentIndex<T>(int typeIndex, T newData) where T : struct
        {
            var defaultVal = default(T);
            var typeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
            if (FastEquality.Equals(ref defaultVal, ref newData, typeInfo))
                return 0;
            return FindNonDefaultSharedComponentIndex(typeIndex, FastEquality.GetHashCode(ref newData, typeInfo),
                UnsafeUtility.AddressOf(ref newData), typeInfo);
        }

        private unsafe int FindNonDefaultSharedComponentIndex(int typeIndex, int hashCode, void* newData,
            FastEquality.TypeInfo typeInfo)
        {
            int itemIndex;
            NativeMultiHashMapIterator<int> iter;

            if (!m_HashLookup.TryGetFirstValue(hashCode, out itemIndex, out iter))
                return -1;

            do
            {
                var data = m_SharedComponentData[itemIndex];
                if (data != null && m_SharedComponentType[itemIndex] == typeIndex)
                {
                    ulong handle;
                    var value = PinGCObjectAndGetAddress(data, out handle);
                    var res = FastEquality.Equals(newData, value, typeInfo);
                    UnsafeUtility.ReleaseGCObject(handle);

                    if (res)
                        return itemIndex;
                }
            } while (m_HashLookup.TryGetNextValue(out itemIndex, ref iter));

            return -1;
        }

        internal unsafe int InsertSharedComponentAssumeNonDefault(int typeIndex, int hashCode, object newData,
            FastEquality.TypeInfo typeInfo)
        {
            ulong handle;
            var newDataPtr = PinGCObjectAndGetAddress(newData, out handle);

            var index = FindNonDefaultSharedComponentIndex(typeIndex, hashCode, newDataPtr, typeInfo);

            UnsafeUtility.ReleaseGCObject(handle);

            if (-1 == index)
                index = Add(typeIndex, hashCode, newData);
            else
                m_SharedComponentRefCount[index] += 1;

            return index;
        }

        private int Add(int typeIndex, int hashCode, object newData)
        {
            int index;

            if (m_FreeListIndex != -1)
            {
                index = m_FreeListIndex;
                m_FreeListIndex = m_SharedComponentVersion[index];

                Assert.IsTrue(m_SharedComponentData[index] == null);

                m_HashLookup.Add(hashCode, index);
                m_SharedComponentData[index] = newData;
                m_SharedComponentRefCount[index] = 1;
                m_SharedComponentVersion[index] = 1;
                m_SharedComponentType[index] = typeIndex;
            }
            else
            {
                index = m_SharedComponentData.Count;
                m_HashLookup.Add(hashCode, index);
                m_SharedComponentData.Add(newData);
                m_SharedComponentRefCount.Add(1);
                m_SharedComponentVersion.Add(1);
                m_SharedComponentType.Add(typeIndex);
            }

            return index;
        }


        public void IncrementSharedComponentVersion(int index)
        {
            m_SharedComponentVersion[index]++;
        }

        public int GetSharedComponentVersion<T>(T sharedData) where T : struct
        {
            var index = FindSharedComponentIndex(TypeManager.GetTypeIndex<T>(), sharedData);
            return index == -1 ? 0 : m_SharedComponentVersion[index];
        }

        public T GetSharedComponentData<T>(int index) where T : struct
        {
            if (index == 0)
                return default(T);

            return (T) m_SharedComponentData[index];
        }

        public object GetSharedComponentDataBoxed(int index)
        {
            if (index == 0)
                return Activator.CreateInstance(TypeManager.GetType(m_SharedComponentType[index]));

            return m_SharedComponentData[index];
        }

        public void AddReference(int index)
        {
            if (index != 0)
                ++m_SharedComponentRefCount[index];
        }

        private static unsafe int GetHashCodeFast(object target, FastEquality.TypeInfo typeInfo)
        {
            ulong handle;
            var ptr = PinGCObjectAndGetAddress(target, out handle);
            var hashCode = FastEquality.GetHashCode(ptr, typeInfo);
            UnsafeUtility.ReleaseGCObject(handle);

            return hashCode;
        }

        private static unsafe void* PinGCObjectAndGetAddress(object target, out ulong handle)
        {
            var ptr = UnsafeUtility.PinGCObjectAndGetAddress(target, out handle);
            return (byte*) ptr + TypeManager.ObjectOffset;
        }


        public void RemoveReference(int index)
        {
            if (index == 0)
                return;

            var newCount = --m_SharedComponentRefCount[index];
            Assert.IsTrue(newCount >= 0);

            if (newCount != 0)
                return;

            var typeIndex = m_SharedComponentType[index];

            var typeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
            var hashCode = GetHashCodeFast(m_SharedComponentData[index], typeInfo);

            m_SharedComponentData[index] = null;
            m_SharedComponentType[index] = -1;
            m_SharedComponentVersion[index] = m_FreeListIndex;
            m_FreeListIndex = index;

            int itemIndex;
            NativeMultiHashMapIterator<int> iter;
            if (!m_HashLookup.TryGetFirstValue(hashCode, out itemIndex, out iter))
            {
                #if ENABLE_UNITY_COLLECTIONS_CHECKS
                throw new System.ArgumentException("RemoveReference didn't find element in in hashtable");
                #endif
            }

            do
            {
                if (itemIndex == index)
                {
                    m_HashLookup.Remove(iter);
                    break;
                }
            }
            while (m_HashLookup.TryGetNextValue(out itemIndex, ref iter))
                ;
        }

        //@TODO: Rename
        public void CheckRefcounts()
        {
            int refcount = 0;
            for (int i = 0; i < m_SharedComponentData.Count; ++i)
            {
                if (m_SharedComponentData[i] != null)
                    refcount++;
            }

            Assert.AreEqual(refcount, m_HashLookup.Length);
        }

        public bool IsEmpty()
        {
            for (int i = 1; i < m_SharedComponentData.Count; ++i)
            {
                if (m_SharedComponentData[i] != null)
                    return false;

                if (m_SharedComponentType[i] != -1)
                    return false;

                if (m_SharedComponentRefCount[i] != 0)
                    return false;
            }

            if (m_SharedComponentData[0] != null)
                return false;

            if (m_HashLookup.Length != 0)
                return false;

            return true;
        }

        public unsafe void MoveSharedComponents(SharedComponentDataManager srcSharedComponents,
            int* sharedComponentIndices, int sharedComponentIndicesCount)
        {
            for (var i = 0; i != sharedComponentIndicesCount; i++)
            {
                var srcIndex = sharedComponentIndices[i];
                if (srcIndex == 0)
                    continue;

                var srcData = srcSharedComponents.m_SharedComponentData[srcIndex];
                var typeIndex = srcSharedComponents.m_SharedComponentType[srcIndex];

                var typeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
                var hashCode = GetHashCodeFast(srcData, typeInfo);

                var dstIndex = InsertSharedComponentAssumeNonDefault(typeIndex, hashCode, srcData, typeInfo);
                srcSharedComponents.RemoveReference(srcIndex);

                sharedComponentIndices[i] = dstIndex;
            }
        }

        public unsafe bool AllSharedComponentReferencesAreFromChunks(ArchetypeManager archetypeManager)
        {
            var chunkCount = new NativeArray<int>(m_SharedComponentRefCount.Length, Allocator.Temp);

            var archetype = archetypeManager.m_LastArchetype;
            while (archetype != null)
            {
                for (var c = archetype->ChunkList.Begin; c != archetype->ChunkList.End; c = c->Next)
                {
                    var chunk = (Chunk*)c;

                    for (int i = 0; i < archetype->NumSharedComponents; ++i)
                    {
                        chunkCount[chunk->SharedComponentValueArray[i]] += 1;
                    }
                }

                archetype = archetype->PrevArchetype;
            }

            chunkCount[0] = 1;
            int cmp = UnsafeUtility.MemCmp(m_SharedComponentRefCount.GetUnsafePtr(), chunkCount.GetUnsafeReadOnlyPtr(), sizeof(int) * chunkCount.Length);
            chunkCount.Dispose();

            return cmp == 0;
        }

        public unsafe NativeArray<int> MoveAllSharedComponents(SharedComponentDataManager srcSharedComponents,
            Allocator allocator)
        {
            var remap = new NativeArray<int>(srcSharedComponents.GetSharedComponentCount(), allocator);
            remap[0] = 0;

            for (int srcIndex = 1; srcIndex < remap.Length; ++srcIndex)
            {
                var srcData = srcSharedComponents.m_SharedComponentData[srcIndex];
                if (srcData == null) continue;

                var typeIndex = srcSharedComponents.m_SharedComponentType[srcIndex];

                var typeInfo = TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo;
                var hashCode = GetHashCodeFast(srcData, typeInfo);

                var dstIndex = InsertSharedComponentAssumeNonDefault(typeIndex, hashCode, srcData, typeInfo);

                m_SharedComponentRefCount[dstIndex] += srcSharedComponents.m_SharedComponentRefCount[srcIndex] - 1;

                remap[srcIndex] = dstIndex;
            }

            srcSharedComponents.m_HashLookup.Clear();
            srcSharedComponents.m_SharedComponentVersion.ResizeUninitialized(1);
            srcSharedComponents.m_SharedComponentRefCount.ResizeUninitialized(1);
            srcSharedComponents.m_SharedComponentType.ResizeUninitialized(1);
            srcSharedComponents.m_SharedComponentData.Clear();
            srcSharedComponents.m_SharedComponentData.Add(null);
            srcSharedComponents.m_FreeListIndex = -1;

            return remap;
        }

        public void PrepareForDeserialize()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!IsEmpty())
               throw new System.ArgumentException("SharedComponentManager must be empty when deserializing a scene");
#endif

            m_HashLookup.Clear();
            m_SharedComponentVersion.ResizeUninitialized(1);
            m_SharedComponentRefCount.ResizeUninitialized(1);
            m_SharedComponentType.ResizeUninitialized(1);
            m_SharedComponentData.Clear();
            m_SharedComponentData.Add(null);

            m_FreeListIndex = -1;
        }
    }
}
