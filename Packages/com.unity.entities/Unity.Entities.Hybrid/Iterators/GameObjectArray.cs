using System;
using System.Linq;
using System.Reflection;

using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    public static class ComponentGroupExtensionsForGameObjectArray
    {
        public static GameObjectArray GetGameObjectArray(this ComponentGroup group)
        {
            int length;
            ComponentChunkIterator iterator;
            group.GetComponentChunkIterator(out length, out iterator);
            iterator.IndexInComponentGroup = group.GetIndexInComponentGroup(TypeManager.GetTypeIndex<Transform>());
            return new GameObjectArray(iterator, length, group.ArchetypeManager);
        }
    }
}

namespace Unity.Entities
{
	public struct GameObjectArray
	{
	    ComponentChunkIterator  m_Iterator;
	    ComponentChunkCache 	m_Cache;
	    readonly int                     m_Length;
	    readonly ArchetypeManager		m_ArchetypeManager;

        internal GameObjectArray(ComponentChunkIterator iterator, int length, ArchetypeManager typeMan)
		{
            m_Length = length;
            m_Cache = default(ComponentChunkCache);
			m_Iterator = iterator;
			m_ArchetypeManager = typeMan;
		}

		public GameObject this[int index]
		{
			get
			{
                //@TODO: Unnecessary.. integrate into cache instead...
				if ((uint)index >= (uint)m_Length)
					FailOutOfRangeError(index);

                if (index < m_Cache.CachedBeginIndex || index >= m_Cache.CachedEndIndex)
	                m_Iterator.MoveToEntityIndexAndUpdateCache(index, out m_Cache, true);

			    var transform = (Transform) m_Iterator.GetManagedObject(m_ArchetypeManager, m_Cache.CachedBeginIndex, index);

				return transform.gameObject;
			}
		}

		public GameObject[] ToArray()
		{
		    var arr = new GameObject[m_Length];
			var i = 0;
			while (i < m_Length)
			{
				m_Iterator.MoveToEntityIndexAndUpdateCache(i, out m_Cache, true);
				int start, length;
				var objs = m_Iterator.GetManagedObjectRange(m_ArchetypeManager, m_Cache.CachedBeginIndex, i, out start, out length);
				for (var obj = 0; obj < length; ++obj)
					arr[i+obj] = ((Transform) objs[start + obj]).gameObject;
				i += length;
			}
			return arr;
		}

	    void FailOutOfRangeError(int index)
		{
			throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
		}

		public int Length => m_Length;
	}

    [Preserve]
    [CustomInjectionHook]
    sealed class GameObjectArrayInjectionHook : InjectionHook
    {
        public override Type FieldTypeOfInterest => typeof(GameObjectArray);

        public override bool IsInterestedInField(FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType == typeof(GameObjectArray);
        }

        public override string ValidateField(FieldInfo field, bool isReadOnly, InjectionContext injectionInfo)
        {
            if (field.FieldType != typeof(GameObjectArray))
                return null;

            if (isReadOnly)
                return "[ReadOnly] may not be used on GameObjectArray, it can only be used on ComponentDataArray<>";

            // Error on multiple GameObjectArray
            if (injectionInfo.Entries.Any(i => i.FieldInfo.FieldType == typeof(GameObjectArray)))
                return "A [Inject] struct, may only contain a single GameObjectArray";

            return null;
        }

        public override InjectionContext.Entry CreateInjectionInfoFor(FieldInfo field, bool isReadOnly)
        {
            return new InjectionContext.Entry
            {
                Hook = this,
                FieldInfo = field,
                IsReadOnly = isReadOnly,
                AccessMode = isReadOnly ? ComponentType.AccessMode.ReadOnly : ComponentType.AccessMode.ReadWrite,
                IndexInComponentGroup = -1,
                FieldOffset = UnsafeUtility.GetFieldOffset(field),
                ComponentRequirements = new[] { typeof(Transform) }
            };
        }

        internal override unsafe void InjectEntry(InjectionContext.Entry entry, ComponentGroup entityGroup, ref ComponentChunkIterator iterator, int length, byte* groupStructPtr)
        {
            var gameObjectArray = entityGroup.GetGameObjectArray();
            UnsafeUtility.CopyStructureToPtr(ref gameObjectArray, groupStructPtr + entry.FieldOffset);
        }
    }
}
