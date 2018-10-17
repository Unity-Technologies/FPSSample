using System;
using System.Collections.Generic;

using Unity.Properties;

namespace Unity.Entities.Properties
{
    internal class ClassObjectProxyProperty : ClassValueClassProperty<ObjectContainerProxy, ObjectContainerProxy>
    {
        public Type ComponentType { get; }

        public IPropertyBag PropertyBag => _bag;

        private readonly ClassPropertyBag<ObjectContainerProxy> _bag;
        private readonly object _wrappedObject;

        public ClassObjectProxyProperty(Type t, object o, ClassPropertyBag<ObjectContainerProxy> bag)
            : base(t.Name, null, null)
        {
            _wrappedObject = o;
            _bag = bag;

            ComponentType = t;
        }

        public ClassObjectProxyProperty(Type t, object o, HashSet<Type> primitiveTypes)
            : base(t.Name, null, null)
        {
            _wrappedObject = o;

            _bag = ClassPropertyBagFactory.GetPropertyBagForObject(o, primitiveTypes);

            ComponentType = t;
        }

        public override ObjectContainerProxy GetValue(ObjectContainerProxy container)
        {
            // TODO host the Field/PropertyInfo & actually get the value here
            return new ObjectContainerProxy
            {
                bag = PropertyBag,
                o = _wrappedObject,
            };
        }
    }
}
