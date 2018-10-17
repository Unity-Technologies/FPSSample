using System.Collections.Generic;

namespace UnityEditor.Graphing
{
    public static class QueuePool<T>
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<Queue<T>> k_QueuePool = new ObjectPool<Queue<T>>(null, l => l.Clear());

        public static Queue<T> Get()
        {
            return k_QueuePool.Get();
        }

        public static PooledObject<Queue<T>> GetDisposable()
        {
            return k_QueuePool.GetDisposable();
        }

        public static void Release(Queue<T> toRelease)
        {
            k_QueuePool.Release(toRelease);
        }
    }
}
