using System;
using System.Collections.Generic;
using System.Reflection;

namespace Unity.Entities
{
    internal sealed class CustomInjectionHookAttribute : Attribute
    {
    }

    public sealed class InjectionContext
    {
        private readonly List<Entry> m_Entries = new List<Entry>();

        public bool HasComponentRequirements { get; private set; }

        public bool HasEntries => m_Entries.Count != 0;

        public IReadOnlyCollection<Entry> Entries => m_Entries;

        public IEnumerable<ComponentType> ComponentRequirements
        {
            get
            {
                foreach (var info in m_Entries)
                foreach (var requirement in info.ComponentRequirements)
                    yield return requirement;
            }
        }

        internal void AddEntry(Entry entry)
        {
            HasComponentRequirements = HasComponentRequirements || entry.ComponentRequirements.Length > 0;
            m_Entries.Add(entry);
        }

        public void PrepareEntries(ComponentGroup entityGroup)
        {
            if (!HasEntries)
                return;

            for (var index = 0; index < m_Entries.Count; index++)
            {
                var entry = m_Entries[index];
                entry.Hook.PrepareEntry(ref entry, entityGroup);
                m_Entries[index] = entry;
            }
        }

        internal unsafe void UpdateEntries(ComponentGroup entityGroup, ref ComponentChunkIterator iterator, int length,
            byte* groupStructPtr)
        {
            if (!HasEntries)
                return;

            foreach (var info in m_Entries)
                info.Hook.InjectEntry(info, entityGroup, ref iterator, length, groupStructPtr);
        }

        public struct Entry
        {
            public int FieldOffset;
            public FieldInfo FieldInfo;
            public Type[] ComponentRequirements;
            public InjectionHook Hook;
            public ComponentType.AccessMode AccessMode;
            public int IndexInComponentGroup;
            public bool IsReadOnly;
            public ComponentType ComponentType;
        }
    }

    public abstract unsafe class InjectionHook
    {
        public abstract Type FieldTypeOfInterest { get; }
        public abstract bool IsInterestedInField(FieldInfo fieldInfo);
        public abstract InjectionContext.Entry CreateInjectionInfoFor(FieldInfo field, bool isReadOnly);

        internal abstract void InjectEntry(InjectionContext.Entry entry, ComponentGroup entityGroup,
            ref ComponentChunkIterator iterator, int length, byte* groupStructPtr);

        public abstract string ValidateField(FieldInfo field, bool isReadOnly, InjectionContext injectionInfo);

        public virtual void PrepareEntry(ref InjectionContext.Entry entry, ComponentGroup entityGroup)
        {
        }
    }

    public static class InjectionHookSupport
    {
        private static bool s_HasHooks;
        private static readonly List<InjectionHook> k_Hooks = new List<InjectionHook>();

        internal static IReadOnlyCollection<InjectionHook> Hooks => k_Hooks;

        public static void RegisterHook(InjectionHook hook)
        {
            s_HasHooks = true;
            k_Hooks.Add(hook);
        }

        public static void UnregisterHook(InjectionHook hook)
        {
            k_Hooks.Remove(hook);
            s_HasHooks = k_Hooks.Count != 0;
        }

        internal static InjectionHook HookFor(FieldInfo fieldInfo)
        {
            if (!s_HasHooks)
                return null;

            // TODO: in case of multiple hooks interested in a single field type, we should drop an error (in Editor)
            foreach (var hook in k_Hooks)
                if (hook.IsInterestedInField(fieldInfo))
                    return hook;

            return null;
        }

        public static bool IsValidHook(Type type)
        {
            if (type.IsAbstract)
                return false;
            if (type.ContainsGenericParameters)
                return false;
            if (!typeof(InjectionHook).IsAssignableFrom(type))
                return false;
            return type.GetCustomAttributes(typeof(CustomInjectionHookAttribute), true).Length != 0;
        }
    }
}
