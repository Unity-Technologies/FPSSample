using System.Collections.Generic;

namespace UnityEditor.Graphing
{
    public static class StackPool<T>
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<Stack<T>> k_StackPool = new ObjectPool<Stack<T>>(null, l => l.Clear());

        public static Stack<T> Get()
        {
            return k_StackPool.Get();
        }

        public static PooledObject<Stack<T>> GetDisposable()
        {
            return k_StackPool.GetDisposable();
        }

        public static void Release(Stack<T> toRelease)
        {
            k_StackPool.Release(toRelease);
        }
    }
}
