using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Properties;

namespace Unity.Entities.Properties
{
    internal class FieldObjectProperty<TValue> : ValueClassProperty<ObjectContainerProxy, TValue>
    {
        public override bool IsReadOnly => true;

        public FieldInfo Field { get; set; }

        public FieldObjectProperty(object parentObject, FieldInfo info) : base(info.Name, null, null)
        {
            _parentObject = parentObject;

            Field = info;
        }

        public override TValue GetValue(ObjectContainerProxy container)
        {
            return (TValue)Field.GetValue(_parentObject);
        }

        private readonly object _parentObject;
    }

    internal class CSharpPropertyObjectProperty<TValue> : ValueClassProperty<ObjectContainerProxy, TValue>
    {
        public override bool IsReadOnly => true;

        public PropertyInfo Property { get; set; }

        public CSharpPropertyObjectProperty(object parentObject, PropertyInfo info) : base(info.Name, null, null)
        {
            _parentObject = parentObject;

            Property = info;
        }

        public override TValue GetValue(ObjectContainerProxy container)
        {
            return (TValue) Property.GetValue(_parentObject);
        }

        private readonly object _parentObject;
    }
}