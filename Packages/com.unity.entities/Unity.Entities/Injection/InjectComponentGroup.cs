using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    internal struct ProxyComponentData : IComponentData
    {
        #pragma warning disable 169 // Not really zero-sized component (tag)
        private byte m_Internal;
        #pragma warning restore 169
    }

    internal struct ProxyBufferElementData : IBufferElementData
    {
        #pragma warning disable 169 // Not really zero-sized component (tag)
        private byte m_Internal;
        #pragma warning restore 169
    }

    internal struct ProxySharedComponentData : ISharedComponentData
    {
        #pragma warning disable 169 // Not really zero-sized component (tag)
        private byte m_Internal;
        #pragma warning restore 169
    }

    internal class InjectComponentGroupData
    {
        private readonly InjectionData[] m_ComponentDataInjections;

        private readonly int m_EntityArrayOffset;
        private readonly InjectionData[] m_BufferArrayInjections;
        private readonly int m_GroupFieldOffset;

        private readonly InjectionContext m_InjectionContext;
        private readonly int m_LengthOffset;
        private readonly InjectionData[] m_SharedComponentInjections;
        private readonly ComponentGroup m_EntityGroup;

        private readonly int m_ComponentGroupIndex;

        private unsafe InjectComponentGroupData(ComponentSystemBase system, FieldInfo groupField,
            InjectionData[] componentDataInjections, InjectionData[] bufferArrayInjections,
            InjectionData[] sharedComponentInjections,
            FieldInfo entityArrayInjection, FieldInfo indexFromEntityInjection, InjectionContext injectionContext,
            FieldInfo lengthInjection, FieldInfo componentGroupIndexField, ComponentType[] componentRequirements)
        {
            m_EntityGroup = system.GetComponentGroupInternal(componentRequirements);

            m_ComponentGroupIndex = Array.IndexOf(system.ComponentGroups, m_EntityGroup);

            m_ComponentDataInjections = componentDataInjections;
            m_BufferArrayInjections = bufferArrayInjections;
            m_SharedComponentInjections = sharedComponentInjections;
            m_InjectionContext = injectionContext;

            PatchGetIndexInComponentGroup(m_ComponentDataInjections);
            PatchGetIndexInComponentGroup(m_BufferArrayInjections);
            PatchGetIndexInComponentGroup(m_SharedComponentInjections);

            injectionContext.PrepareEntries(m_EntityGroup);

            if (entityArrayInjection != null)
                m_EntityArrayOffset = UnsafeUtility.GetFieldOffset(entityArrayInjection);
            else
                m_EntityArrayOffset = -1;

            if (lengthInjection != null)
                m_LengthOffset = UnsafeUtility.GetFieldOffset(lengthInjection);
            else
                m_LengthOffset = -1;

            m_GroupFieldOffset = UnsafeUtility.GetFieldOffset(groupField);

            if (componentGroupIndexField != null)
            {
                var pinnedSystemPtr = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(system, out var gchandle);
                var groupIndexPtr = pinnedSystemPtr + m_GroupFieldOffset + UnsafeUtility.GetFieldOffset(componentGroupIndexField);

                int groupIndex = m_ComponentGroupIndex;
                UnsafeUtility.CopyStructureToPtr(ref groupIndex, groupIndexPtr);

                UnsafeUtility.ReleaseGCObject(gchandle);
            }
        }

        private void PatchGetIndexInComponentGroup(InjectionData[] componentInjections)
        {
            for (var i = 0; i != componentInjections.Length; i++)
                componentInjections[i].IndexInComponentGroup =
                    m_EntityGroup.GetIndexInComponentGroup(componentInjections[i].ComponentType.TypeIndex);
        }

        public unsafe void UpdateInjection(byte* systemPtr)
        {
            var groupStructPtr = systemPtr + m_GroupFieldOffset;

            int length;
            ComponentChunkIterator iterator;
            m_EntityGroup.GetComponentChunkIterator(out length, out iterator);

            for (var i = 0; i != m_ComponentDataInjections.Length; i++)
            {
                ComponentDataArray<ProxyComponentData> data;
                m_EntityGroup.GetComponentDataArray(ref iterator, m_ComponentDataInjections[i].IndexInComponentGroup,
                    length, out data);
                UnsafeUtility.CopyStructureToPtr(ref data, groupStructPtr + m_ComponentDataInjections[i].FieldOffset);
            }

            for (var i = 0; i != m_SharedComponentInjections.Length; i++)
            {
                SharedComponentDataArray<ProxySharedComponentData> data;
                m_EntityGroup.GetSharedComponentDataArray(ref iterator,
                    m_SharedComponentInjections[i].IndexInComponentGroup, length, out data);
                UnsafeUtility.CopyStructureToPtr(ref data, groupStructPtr + m_SharedComponentInjections[i].FieldOffset);
            }

            for (var i = 0; i != m_BufferArrayInjections.Length; i++)
            {
                BufferArray<ProxyBufferElementData> data;
                m_EntityGroup.GetBufferArray(ref iterator, m_BufferArrayInjections[i].IndexInComponentGroup, length,
                    out data);
                UnsafeUtility.CopyStructureToPtr(ref data, groupStructPtr + m_BufferArrayInjections[i].FieldOffset);
            }

            if (m_EntityArrayOffset != -1)
            {
                EntityArray entityArray;
                m_EntityGroup.GetEntityArray(ref iterator, length, out entityArray);
                UnsafeUtility.CopyStructureToPtr(ref entityArray, groupStructPtr + m_EntityArrayOffset);
            }

            if (m_InjectionContext.HasEntries)
                m_InjectionContext.UpdateEntries(m_EntityGroup, ref iterator, length, groupStructPtr);

            if (m_LengthOffset != -1) UnsafeUtility.CopyStructureToPtr(ref length, groupStructPtr + m_LengthOffset);
        }

        public static InjectComponentGroupData CreateInjection(Type injectedGroupType, FieldInfo groupField,
            ComponentSystemBase system)
        {
            FieldInfo entityArrayField;
            FieldInfo indexFromEntityField;
            FieldInfo lengthField;
            FieldInfo componentGroupIndexField;

            var injectionContext = new InjectionContext();
            var componentDataInjections = new List<InjectionData>();
            var bufferDataInjections = new List<InjectionData>();
            var sharedComponentInjections = new List<InjectionData>();

            var componentRequirements = new HashSet<ComponentType>();
            var error = CollectInjectedGroup(system, groupField, injectedGroupType, out entityArrayField,
                out indexFromEntityField, injectionContext, out lengthField, out componentGroupIndexField,
                componentRequirements, componentDataInjections, bufferDataInjections, sharedComponentInjections);
            if (error != null)
                throw new ArgumentException(error);

            return new InjectComponentGroupData(system, groupField, componentDataInjections.ToArray(),
                bufferDataInjections.ToArray(), sharedComponentInjections.ToArray(), entityArrayField,
                indexFromEntityField, injectionContext, lengthField, componentGroupIndexField,
                componentRequirements.ToArray());
        }

        private static string CollectInjectedGroup(ComponentSystemBase system, FieldInfo groupField,
            Type injectedGroupType, out FieldInfo entityArrayField, out FieldInfo indexFromEntityField,
            InjectionContext injectionContext, out FieldInfo lengthField, out FieldInfo componentGroupIndexField,
            ISet<ComponentType> componentRequirements, ICollection<InjectionData> componentDataInjections,
            ICollection<InjectionData> bufferDataInjections, ICollection<InjectionData> sharedComponentInjections)
        {
            //@TODO: Improve error messages...
            var fields =
                injectedGroupType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            entityArrayField = null;
            indexFromEntityField = null;
            lengthField = null;
            componentGroupIndexField = null;

            foreach (var field in fields)
            {
                var isReadOnly = field.GetCustomAttributes(typeof(ReadOnlyAttribute), true).Length != 0;
                //@TODO: Prevent using GameObjectEntity, it will never show up. Point to GameObjectArray instead...

                if (field.FieldType.IsGenericType &&
                    field.FieldType.GetGenericTypeDefinition() == typeof(ComponentDataArray<>))
                {
                    var injection = new InjectionData(field, field.FieldType.GetGenericArguments()[0], isReadOnly);
                    componentDataInjections.Add(injection);
                    componentRequirements.Add(injection.ComponentType);
                }
                else if (field.FieldType.IsGenericType &&
                         field.FieldType.GetGenericTypeDefinition() == typeof(SubtractiveComponent<>))
                {
                    componentRequirements.Add(ComponentType.Subtractive(field.FieldType.GetGenericArguments()[0]));
                }
                else if (field.FieldType.IsGenericType &&
                         field.FieldType.GetGenericTypeDefinition() == typeof(BufferArray<>))
                {
                    var injection = new InjectionData(field, field.FieldType.GetGenericArguments()[0], isReadOnly);

                    bufferDataInjections.Add(injection);
                    componentRequirements.Add(injection.ComponentType);
                }
                else if (field.FieldType.IsGenericType &&
                         field.FieldType.GetGenericTypeDefinition() == typeof(SharedComponentDataArray<>))
                {
                    if (!isReadOnly)
                        return
                            $"{system.GetType().Name}:{groupField.Name} SharedComponentDataArray<> must always be injected as [ReadOnly]";
                    var injection = new InjectionData(field, field.FieldType.GetGenericArguments()[0], true);

                    sharedComponentInjections.Add(injection);
                    componentRequirements.Add(injection.ComponentType);
                }
                else if (field.FieldType == typeof(EntityArray))
                {
                    // Error on multiple EntityArray
                    if (entityArrayField != null)
                        return
                            $"{system.GetType().Name}:{groupField.Name} An [Inject] struct, may only contain a single EntityArray";

                    entityArrayField = field;
                }
                else if (field.FieldType == typeof(int))
                {
                    if (field.Name != "Length" && field.Name != "GroupIndex")
                        return
                            $"{system.GetType().Name}:{groupField.Name} Int in an [Inject] struct should be named \"Length\" (group length) or \"GroupIndex\" (index in ComponentGroup[])";

                    if (!field.IsInitOnly)
                        return
                            $"{system.GetType().Name}:{groupField.Name} {field.Name} must use the \"readonly\" keyword";

                    if(field.Name == "Length")
                        lengthField = field;

                    if (field.Name == "GroupIndex")
                        componentGroupIndexField = field;
                }
                else
                {
                    var hook = InjectionHookSupport.HookFor(field);
                    if (hook == null)
                        return
                            $"{system.GetType().Name}:{groupField.Name} [Inject] may only be used on ComponentDataArray<>, ComponentArray<>, TransformAccessArray, EntityArray, {string.Join(",", InjectionHookSupport.Hooks.Select(h => h.FieldTypeOfInterest.Name))} and int Length.";

                    var error = hook.ValidateField(field, isReadOnly, injectionContext);
                    if (error != null) return error;

                    injectionContext.AddEntry(hook.CreateInjectionInfoFor(field, isReadOnly));
                }
            }

            if (injectionContext.HasComponentRequirements)
                foreach (var requirement in injectionContext.ComponentRequirements)
                    componentRequirements.Add(requirement);

            return null;
        }
    }
}
