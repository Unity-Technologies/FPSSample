using System;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
	internal unsafe struct ChunkListMap : IDisposable
	{
	    public void Init(int count)
	    {
	        Assert.IsTrue(0 == (count & (count-1)));
	        buckets = (Node*)UnsafeUtility.Malloc(count * sizeof(Node), 8, Allocator.Persistent);
	        hashMask = (uint)(count - 1);
	        UnsafeUtility.MemClear(buckets, count*sizeof(Node));
	        emptyNodes = (uint)count;
	    }

	    public void AppendFrom(ChunkListMap* src)
	    {
		    if (src->buckets == null)
		    {
			    return;
		    }

		    Node* srcBuckets = src->buckets;
	        int srcBucketCount = (int)src->hashMask+1;
	        Node* lastNode = &buckets[hashMask];

	        for (int i = 0; i < srcBucketCount; i++)
	        {
	            Node* srcNode = &srcBuckets[i];
	            if (!srcNode->IsDeleted() && !srcNode->IsFree())
	            {
		            AddMultiple(&srcNode->list);
	            }
	        }
	    }

	    uint GetHashCode(int* sharedComponentDataIndices, int numSharedComponents)
	    {
	        // simplified xxhash inner loop, replace once there is a proper hash function in Mathematics
	        ulong hash = 1609587929392839161UL;
	        unchecked
	        {
	            for (int i = 0; i < numSharedComponents; ++i)
	            {
	                hash += (ulong)sharedComponentDataIndices[i] * 14029467366897019727UL;
	                hash = (hash>>31) | (hash<<33);
	                hash *= 11400714785074694791UL;
	            }

	            return (uint)(hash ^ (hash >> 32));
	        }
	    }

	    public Chunk* GetChunkWithEmptySlots(int* sharedComponentDataIndices, int numSharedComponents)
	    {
	        uint hash = GetHashCode(sharedComponentDataIndices, numSharedComponents);
	        Node* node = &buckets[hash & hashMask];
	        Node* lastNode = &buckets[hashMask];

	        while (!node->IsFree())
	        {
	            if(!node->IsDeleted() && node->CheckEqual(hash, sharedComponentDataIndices, numSharedComponents))
	            {
	                return  ArchetypeManager.GetChunkFromEmptySlotNode(node->list.Begin);
	            }

	            node = node + 1;
	            if (node > lastNode)
	                node = buckets;
	        }

	        return null;
	    }

	    public void Add(Chunk* chunk)
	    {
	        int* sharedComponentDataIndices = chunk->SharedComponentValueArray;
	        int numSharedComponents = chunk->Archetype->NumSharedComponents;
	        uint hash = GetHashCode(sharedComponentDataIndices, numSharedComponents);
	        Node* node = &buckets[hash & hashMask];
	        Node* lastNode = &buckets[hashMask];
	        Node* freeNode = null;

	        while(!node->IsFree())
	        {
	            if(!node->IsDeleted())
	            {
	                if(node->CheckEqual(hash, sharedComponentDataIndices, numSharedComponents))
	                {
	                    node->list.Add(&chunk->ChunkListWithEmptySlotsNode);
	                    return;
	                }
	            }
	            else
	            {
	                if (freeNode == null)
	                    freeNode = node;
	            }

	            node = node + 1;
	            if (node > lastNode)
	                node = buckets;
	        }

	        if (freeNode == null)
	        {
	            freeNode = node;
	            --emptyNodes;
	        }

	        freeNode->hash = hash;
	        UnsafeLinkedListNode.InitializeList(&freeNode->list);
	        freeNode->list.Add(&chunk->ChunkListWithEmptySlotsNode);

	        if (ShouldGrow(emptyNodes))
	        {
	            Grow();
	        }
	    }

		void AddMultiple(UnsafeLinkedListNode *list)
		{
			var firstChunk = ArchetypeManager.GetChunkFromEmptySlotNode(list->Begin);
			UInt32 hash = GetHashCode(firstChunk->SharedComponentValueArray, firstChunk->Archetype->NumSharedComponents);

			int* sharedComponentDataIndices = firstChunk->SharedComponentValueArray;
			int numSharedComponents = firstChunk->Archetype->NumSharedComponents;
			Node* node = &buckets[hash & hashMask];
			Node* lastNode = &buckets[hashMask];
			Node* freeNode = null;

			while(!node->IsFree())
			{
				if(!node->IsDeleted())
				{
					if(node->CheckEqual(hash, sharedComponentDataIndices, numSharedComponents))
					{
						UnsafeLinkedListNode.InsertListBefore(node->list.End, list);
						return;
					}
				}
				else
				{
					if (freeNode == null)
						freeNode = node;
				}

				node = node + 1;
				if (node > lastNode)
					node = buckets;
			}

			if (freeNode == null)
			{
				freeNode = node;
				--emptyNodes;
			}

			freeNode->hash = hash;
			UnsafeLinkedListNode.InitializeList(&freeNode->list);
			UnsafeLinkedListNode.InsertListBefore(freeNode->list.End, list);

			if (ShouldGrow(emptyNodes))
			{
				Grow();
			}
		}

	    bool ShouldGrow(uint unoccupiedNodes)
	    {
	        return (unoccupiedNodes * 3 < hashMask);
	    }

	    void Grow()
	    {
	        //Check the table should grow or just purge deleted elements
	        uint unoccupiedNodes = 0;
	        for (int i = 0; i <= hashMask; ++i)
	        {
	            if (buckets[i].IsFree() || buckets[i].IsDeleted())
	            {
	                ++unoccupiedNodes;
	            }
	        }

	        int newBucketCount = (int)(hashMask + 1) * (ShouldGrow(unoccupiedNodes) ? 2 : 1);
            Node* oldBuckets = buckets;
	        int oldBucketCount = (int)hashMask + 1;
	        Init(newBucketCount);

	        Node* lastNode = &buckets[hashMask];
	        for (int i = 0; i < oldBucketCount; i++)
	        {
	            Node* srcNode = &oldBuckets[i];
	            if (!srcNode->IsDeleted() && !srcNode->IsFree())
	            {
	                UInt32 hash = srcNode->hash;
	                Node* node = &buckets[hash & hashMask];
	                while (!node->IsFree())
	                {
	                    node = node + 1;
	                    if (node > lastNode) node = buckets;
	                }

	                *node = *srcNode;
	                node->list.Next->Prev = &node->list;
	                node->list.Prev->Next = &node->list;
	                --emptyNodes;
	            }
	        }
	        UnsafeUtility.Free(oldBuckets, Allocator.Persistent);
	    }

	    struct Node
	    {
	        public UnsafeLinkedListNode list;
	        public uint hash;

	        public bool IsFree()
	        {
	            return list.Next == null;
	        }

	        public bool IsDeleted()
	        {
	            return list.IsEmpty;
	        }

	        public bool CheckEqual(uint hash, int* sharedComponentDataIndices, int numSharedComponents)
	        {
	            return ((this.hash == hash) &&
	                (UnsafeUtility.MemCmp(sharedComponentDataIndices, ArchetypeManager.GetChunkFromEmptySlotNode(list.Begin)->SharedComponentValueArray,
	                     numSharedComponents * sizeof(int)) == 0));
	        }
	    }

	    private Node* buckets;
	    private uint hashMask;
	    private uint emptyNodes;

	    public void Dispose()
	    {
	        if(buckets != null)
                UnsafeUtility.Free(buckets, Allocator.Persistent);
	    }
	}
}
