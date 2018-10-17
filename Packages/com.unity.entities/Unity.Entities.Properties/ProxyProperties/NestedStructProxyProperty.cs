using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;

namespace Unity.Entities.Properties
{
    internal unsafe class NestedStructProxyProperty : StructValueStructProperty<StructProxy, StructProxy>
    {
        public int FieldOffset { get; }
        public Type ComponentType { get; }
        public IPropertyBag PropertyBag { get; set; }

        public NestedStructProxyProperty(ITypedMemberDescriptor member)
            : base(member.Name, null, null, (ByRef m, StructValueStructProperty<StructProxy, StructProxy> property, ref StructProxy c, IPropertyVisitor v) =>
            {
                var val = property.GetValue(ref c);
                m(property, ref c, ref val, v);
            })
        {
            FieldOffset = member.GetOffset();
            ComponentType = member.GetMemberType();
        }

        public override StructProxy GetValue(ref StructProxy container)
        {
            return new StructProxy()
            {
                data = container.data + FieldOffset,
                bag = PropertyBag,
                type = ComponentType
            };
        }
    }
}
