using System;
using Unity.Properties;

namespace Unity.Entities.Properties
{
    public unsafe struct StructProxy : IPropertyContainer
    {
        public IVersionStorage VersionStorage => null;
        public IPropertyBag PropertyBag => bag;

        public byte* data;
        public IPropertyBag bag;
        public Type type;
    }
}
