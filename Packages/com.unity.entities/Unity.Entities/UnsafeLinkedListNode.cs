using Unity.Assertions;

namespace Unity.Entities
{
    // IMPORTANT NOTE:
    // UnsafeLinkedListNode may NOT be put into any memory owned by a class.
    // The memory containing it must ALWAYS be allocated with malloc instead, also it can never be on the stack.
    internal unsafe struct UnsafeLinkedListNode
    {
        public UnsafeLinkedListNode* Prev;
        public UnsafeLinkedListNode* Next;

        public static void InitializeList(UnsafeLinkedListNode* list)
        {
            list->Prev = list;
            list->Next = list;
        }

        public bool IsInList => Prev != null;

        public UnsafeLinkedListNode* Begin => Next;

        public UnsafeLinkedListNode* Back => Prev;

        public bool IsEmpty
        {
            get
            {
                fixed (UnsafeLinkedListNode* list = &this)
                {
                    return list == Next;
                }
            }
        }

        public UnsafeLinkedListNode* End
        {
            get
            {
                fixed (UnsafeLinkedListNode* list = &this)
                {
                    return list;
                }
            }
        }

        public void Add(UnsafeLinkedListNode* node)
        {
            fixed (UnsafeLinkedListNode* list = &this)
            {
                InsertBefore(list, node);
            }
        }

        public static void InsertBefore(UnsafeLinkedListNode* pos, UnsafeLinkedListNode* node)
        {
            Assert.IsTrue(node != pos);
            Assert.IsFalse(node->IsInList);

            node->Prev = pos->Prev;
            node->Next = pos;

            node->Prev->Next = node;
            node->Next->Prev = node;
        }

        public static void InsertListBefore(UnsafeLinkedListNode* pos, UnsafeLinkedListNode* srcList)
        {
            Assert.IsTrue(pos != srcList);
            Assert.IsFalse(srcList->IsEmpty);

            // Insert source before pos
            var a = pos->Prev;
            var b = pos;
            a->Next = srcList->Next;
            b->Prev = srcList->Prev;
            a->Next->Prev = a;
            b->Prev->Next = b;

            // Clear source list
            srcList->Next = srcList;
            srcList->Prev = srcList;
        }

        public void Remove()
        {
            if (Prev == null)
                return;

            Prev->Next = Next;
            Next->Prev = Prev;
            Prev = null;
            Next = null;
        }
    }

    // it takes pointers to other nodes and thus can't handle a moving GC if the data was on a class
}
