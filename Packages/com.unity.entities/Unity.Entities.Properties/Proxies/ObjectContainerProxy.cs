using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;

namespace Unity.Entities.Properties
{
    public class ObjectContainerProxy : IPropertyContainer
    {
        public IVersionStorage VersionStorage => null;
        public IPropertyBag PropertyBag => bag;

        public IPropertyBag bag;
        public object o;
    }
}
