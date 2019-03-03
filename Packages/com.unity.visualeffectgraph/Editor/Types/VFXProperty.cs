using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    struct VFXPropertyWithValue
    {
        public VFXProperty property;
        public object value;

        public VFXPropertyWithValue(VFXProperty property, object value = null)
        {
            this.property = property;
            this.value = value;
        }
    }

    [Serializable]
    struct VFXProperty
    {
        public Type type
        {
            get
            {
                return m_serializedType;
            }
            private set
            {
                m_serializedType = value;
            }
        }

        public string name;
        [SerializeField]
        private SerializableType m_serializedType;

        [SerializeField]
        public VFXPropertyAttribute[] attributes;

        public VFXProperty(Type type, string name, params VFXPropertyAttribute[] attributes)
        {
            m_serializedType = type;
            this.name = name;
            this.attributes = attributes;
        }

        public VFXProperty(FieldInfo info)
        {
            name = info.Name;
            m_serializedType = info.FieldType;
            attributes = VFXPropertyAttribute.Create(info.GetCustomAttributes(true));
        }

        public override int GetHashCode()
        {
            return 13 * name.GetHashCode() + m_serializedType.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VFXProperty))
                return false;

            VFXProperty other = (VFXProperty)obj;
            return type == other.type && name == other.name;
        }

        public IEnumerable<VFXProperty> SubProperties()
        {
            if (IsExpandable())
            {
                FieldInfo[] infos = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                return infos.Select(info => new VFXProperty(info));
            }
            else
            {
                return Enumerable.Empty<VFXProperty>();
            }
        }

        public bool IsExpandable()
        {
            if (type == null) return false;
            return !type.IsPrimitive && !typeof(UnityEngine.Object).IsAssignableFrom(type) && type != typeof(AnimationCurve) && type != typeof(Matrix4x4);
        }
    }
}
