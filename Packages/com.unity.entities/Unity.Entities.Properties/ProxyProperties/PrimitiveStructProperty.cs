using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;

namespace Unity.Entities.Properties
{
    unsafe class PrimitiveStructProperty<TValue> : ValueStructProperty<StructProxy, TValue>
        where TValue : struct
    {
        private int FieldOffset { get; }

        public override bool IsReadOnly => false;

        public PrimitiveStructProperty(ITypedMemberDescriptor member) : base(member.Name, null, null)
        {
            FieldOffset = member.GetOffset();
        }

        public override TValue GetValue(ref StructProxy container)
        {
            TValue v = default(TValue);
            UnsafeUtility.CopyPtrToStructure(container.data + FieldOffset, out v);
            return v;
        }

        public override void SetValue(ref StructProxy container, TValue value)
        {
            // @TODO ComponentJobSafetyManager.CompleteReadAndWriteDependency ?
            UnsafeUtility.CopyStructureToPtr(ref value, container.data + FieldOffset);
        }
    }
}
