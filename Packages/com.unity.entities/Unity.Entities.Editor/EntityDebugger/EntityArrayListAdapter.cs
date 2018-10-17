using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor.IMGUI.Controls;

namespace Unity.Entities.Editor
{
    internal class EntityArrayListAdapter : IList<TreeViewItem>
    {
        
        private readonly TreeViewItem currentItem = new TreeViewItem();

        private NativeArray<ArchetypeChunk> chunkArray;

        private EntityManager entityManager;

        public int Count { get; private set; }

        public void SetSource(NativeArray<ArchetypeChunk> newChunkArray, EntityManager newEntityManager)
        {
            chunkArray = newChunkArray;
            Count = 0;
            if (chunkArray.IsCreated)
            {
                foreach (var t in chunkArray)
                    Count += t.Count;
            }

            entityManager = newEntityManager;
        }

        public TreeViewItem this[int index]
        {
            get
            {
                var requestedIndex = index;
                var currentChunk = 0;
                while (chunkArray[currentChunk].Count <= index)
                    index -= chunkArray[currentChunk++].Count;
                
                var entityArray = chunkArray[currentChunk].GetNativeArray(entityManager.GetArchetypeChunkEntityType());
                var entity = entityArray[index];
            
                currentItem.id = entity.Index;
                currentItem.displayName = $"Entity {entity.Index}";
                return currentItem;
            }
            set => throw new System.NotImplementedException();
        }
        
        public bool IsReadOnly => false;

        public bool GetById(int id, out Entity foundEntity)
        {
            foreach (var chunk in chunkArray)
            {
                var array = chunk.GetNativeArray(entityManager.GetArchetypeChunkEntityType());
                foreach (var entity in array)
                {
                    if (entity.Index == id)
                    {
                        foundEntity = entity;
                        return true;
                    }
                }
            }
            
            foundEntity = Entity.Null;
            
            return false;
        }

        public bool Contains(TreeViewItem item)
        {
            throw new NotImplementedException();
        }
        
        public IEnumerator<TreeViewItem> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TreeViewItem item)
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            throw new System.NotImplementedException();
        }

        public void CopyTo(TreeViewItem[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(TreeViewItem item)
        {
            throw new System.NotImplementedException();
        }

        public int IndexOf(TreeViewItem item)
        {
            throw new System.NotImplementedException();
        }

        public void Insert(int index, TreeViewItem item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new System.NotImplementedException();
        }
    }
}