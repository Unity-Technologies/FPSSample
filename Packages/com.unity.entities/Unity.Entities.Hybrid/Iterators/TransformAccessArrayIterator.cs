using System;
using System.Linq;
using System.Reflection;

using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    struct TransformAccessArrayState : IDisposable
    {
        public TransformAccessArray Data;
        public int OrderVersion;

        public void Dispose()
        {
            if (Data.isCreated)
                Data.Dispose();
        }
    }

    public static class ComponentGroupExtensionsForTransformAccessArray
    {
        public static unsafe TransformAccessArray GetTransformAccessArray(this ComponentGroup group)
        {
            var state = (TransformAccessArrayState?)group.m_CachedState ?? new TransformAccessArrayState();
            var orderVersion = group.EntityDataManager->GetComponentTypeOrderVersion(TypeManager.GetTypeIndex<Transform>());

            if (state.Data.isCreated && orderVersion == state.OrderVersion)
                return state.Data;

            state.OrderVersion = orderVersion;

            UnityEngine.Profiling.Profiler.BeginSample("DirtyTransformAccessArrayUpdate");
            var trans = group.GetComponentArray<Transform>();
            if (!state.Data.isCreated)
                state.Data = new TransformAccessArray(trans.ToArray());
            else
                state.Data.SetTransforms(trans.ToArray());
            UnityEngine.Profiling.Profiler.EndSample();

            group.m_CachedState = state;

            return state.Data;
        }
    }
}

namespace Unity.Entities
{
    [Preserve]
    [CustomInjectionHook]
    sealed class TransformAccessArrayInjectionHook : InjectionHook
    {
        public override Type FieldTypeOfInterest => typeof(TransformAccessArray);

        public override bool IsInterestedInField(FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType == typeof(TransformAccessArray);
        }

        public override string ValidateField(FieldInfo field, bool isReadOnly, InjectionContext injectionInfo)
        {
            if (isReadOnly)
                return "[ReadOnly] may not be used on a TransformAccessArray only on ComponentDataArray<>";

            // Error on multiple TransformAccessArray
            if (injectionInfo.Entries.Any(i => i.FieldInfo.FieldType == typeof(TransformAccessArray)))
                return "A [Inject] struct, may only contain a single TransformAccessArray";

            return null;
        }

        public override InjectionContext.Entry CreateInjectionInfoFor(FieldInfo field, bool isReadOnly)
        {
            return new InjectionContext.Entry
            {
                Hook = this,
                FieldInfo = field,
                IsReadOnly = isReadOnly,
                AccessMode = isReadOnly ? ComponentType.AccessMode.ReadOnly : ComponentType.AccessMode.ReadWrite,
                IndexInComponentGroup = -1,
                FieldOffset = UnsafeUtility.GetFieldOffset(field),
                ComponentRequirements = new[] { typeof(Transform) }
            };
        }

        internal override unsafe void InjectEntry(InjectionContext.Entry entry, ComponentGroup entityGroup, ref ComponentChunkIterator iterator, int length, byte* groupStructPtr)
        {
            var transformsArray = entityGroup.GetTransformAccessArray();
            UnsafeUtility.CopyStructureToPtr(ref transformsArray, groupStructPtr + entry.FieldOffset);
        }
    }
}
