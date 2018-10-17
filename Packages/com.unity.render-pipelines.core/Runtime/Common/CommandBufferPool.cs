using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    class ObjectPool<T> where T : new()
    {
        readonly Stack<T> m_Stack = new Stack<T>();
        readonly UnityAction<T> m_ActionOnGet;
        readonly UnityAction<T> m_ActionOnRelease;

        public int countAll { get; private set; }
        public int countActive { get { return countAll - countInactive; } }
        public int countInactive { get { return m_Stack.Count; } }

        public ObjectPool(UnityAction<T> actionOnGet, UnityAction<T> actionOnRelease)
        {
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
        }

        public T Get()
        {
            T element;
            if (m_Stack.Count == 0)
            {
                element = new T();
                countAll++;
            }
            else
            {
                element = m_Stack.Pop();
            }
            if (m_ActionOnGet != null)
                m_ActionOnGet(element);
            return element;
        }

        public void Release(T element)
        {
            if (m_Stack.Count > 0 && ReferenceEquals(m_Stack.Peek(), element))
                Debug.LogError("Internal error. Trying to destroy object that is already released to pool.");
            if (m_ActionOnRelease != null)
                m_ActionOnRelease(element);
            m_Stack.Push(element);
        }
    }

    public static class CommandBufferPool
    {
        static ObjectPool<CommandBuffer> s_BufferPool = new ObjectPool<CommandBuffer>(null, x => x.Clear());

        public static CommandBuffer Get()
        {
            var cmd = s_BufferPool.Get();
            cmd.name = "Unnamed Command Buffer";
            return cmd;
        }

        public static CommandBuffer Get(string name)
        {
            var cmd = s_BufferPool.Get();
            cmd.name = name;
            return cmd;
        }

        public static void Release(CommandBuffer buffer)
        {
            s_BufferPool.Release(buffer);
        }
    }
}
