using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering
{
    public sealed class VolumeProfile : ScriptableObject
    {
        public List<VolumeComponent> components = new List<VolumeComponent>();

        // Editor only, doesn't have any use outside of it
        [NonSerialized]
        public bool isDirty = true;

        void OnEnable()
        {
            // Make sure every setting is valid. If a profile holds a script that doesn't exist
            // anymore, nuke it to keep the volume clean. Note that if you delete a script that is
            // currently in use in a volume you'll still get a one-time error in the console, it's
            // harmless and happens because Unity does a redraw of the editor (and thus the current
            // frame) before the recompilation step.
            components.RemoveAll(x => x == null);
        }

        public void Reset()
        {
            isDirty = true;
        }

        public T Add<T>(bool overrides = false)
            where T : VolumeComponent
        {
            return (T)Add(typeof(T), overrides);
        }

        public VolumeComponent Add(Type type, bool overrides = false)
        {
            if (Has(type))
                throw new InvalidOperationException("Component already exists in the volume");

            var component = (VolumeComponent)CreateInstance(type);
            component.SetAllOverridesTo(overrides);
            components.Add(component);
            isDirty = true;
            return component;
        }

        public void Remove<T>()
            where T : VolumeComponent
        {
            Remove(typeof(T));
        }

        public void Remove(Type type)
        {
            int toRemove = -1;

            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].GetType() == type)
                {
                    toRemove = i;
                    break;
                }
            }

            if (toRemove >= 0)
            {
                components.RemoveAt(toRemove);
                isDirty = true;
            }
        }

        public bool Has<T>()
            where T : VolumeComponent
        {
            return Has(typeof(T));
        }

        public bool Has(Type type)
        {
            foreach (var component in components)
            {
                if (component.GetType() == type)
                    return true;
            }

            return false;
        }

        public bool HasSubclassOf(Type type)
        {
            foreach (var component in components)
            {
                if (component.GetType().IsSubclassOf(type))
                    return true;
            }

            return false;
        }

        public bool TryGet<T>(out T component)
            where T : VolumeComponent
        {
            var type = typeof(T);
            return TryGet(type, out component);
        }

        public bool TryGet<T>(Type type, out T component)
            where T : VolumeComponent
        {
            component = null;

            foreach (var comp in components)
            {
                if (comp.GetType() == type)
                {
                    component = (T)comp;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetSubclassOf<T>(Type type, out T component)
            where T : VolumeComponent
        {
            component = null;

            foreach (var comp in components)
            {
                if (comp.GetType().IsSubclassOf(type))
                {
                    component = (T)comp;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetAllSubclassOf<T>(Type type, List<T> result)
            where T : VolumeComponent
        {
            Assert.IsNotNull(components);
            int count = result.Count;

            foreach (var comp in components)
            {
                if (comp.GetType().IsSubclassOf(type))
                    result.Add((T)comp);
            }

            return count != result.Count;
        }
    }
}
