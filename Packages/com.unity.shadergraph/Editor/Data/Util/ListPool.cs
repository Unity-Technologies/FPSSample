using System.Collections.Generic;

namespace UnityEditor.Graphing
{
    public static class ListPool<T>
    {
        // Object pool to avoid allocations.
        static readonly ObjectPool<List<T>> s_ListPool = new ObjectPool<List<T>>(null, l => l.Clear());

        public static List<T> Get()
        {
            return s_ListPool.Get();
        }

        public static PooledObject<List<T>> GetDisposable()
        {
            return s_ListPool.GetDisposable();
        }

        public static void Release(List<T> toRelease)
        {
            s_ListPool.Release(toRelease);
        }
    }
}
