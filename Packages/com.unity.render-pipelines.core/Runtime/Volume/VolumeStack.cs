using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    public sealed class VolumeStack : IDisposable
    {
        // Holds the state of _all_ component types you can possibly add on volumes
        public Dictionary<Type, VolumeComponent> components;

        internal VolumeStack()
        {
        }

        internal void Reload(IEnumerable<Type> baseTypes)
        {
            if (components == null)
                components = new Dictionary<Type, VolumeComponent>();
            else
                components.Clear();

            foreach (var type in baseTypes)
            {
                var inst = (VolumeComponent)ScriptableObject.CreateInstance(type);
                components.Add(type, inst);
            }
        }

        public T GetComponent<T>()
            where T : VolumeComponent
        {
            var comp = GetComponent(typeof(T));
            return (T)comp;
        }

        public VolumeComponent GetComponent(Type type)
        {
            VolumeComponent comp;
            components.TryGetValue(type, out comp);
            return comp;
        }

        public void Dispose()
        {
            foreach (var component in components)
                CoreUtils.Destroy(component.Value);

            components.Clear();
        }
    }
}
