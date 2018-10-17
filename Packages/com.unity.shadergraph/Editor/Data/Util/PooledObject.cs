using System;

namespace UnityEditor.Graphing
{
    public class PooledObject<T> : IDisposable where T : new()
    {
        private ObjectPool<T> m_ObjectPool;

        public T value { get; private set; }

        internal PooledObject(ObjectPool<T> objectPool, T value)
        {
            m_ObjectPool = objectPool;
            this.value = value;
        }

        private void ReleaseUnmanagedResources()
        {
            m_ObjectPool.Release(value);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PooledObject()
        {
            ReleaseUnmanagedResources();
        }
    }
}
