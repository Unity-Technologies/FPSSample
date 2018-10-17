using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphing.Util
{
    public class TypeMapper : IEnumerable<TypeMapping>
    {
        readonly Type m_FromBaseType;
        readonly Type m_ToBaseType;
        readonly Type m_FallbackType;
        readonly Dictionary<Type, Type> m_Mappings = new Dictionary<Type, Type>();

        public TypeMapper(Type fromBaseType = null, Type toBaseType = null, Type fallbackType = null)
        {
            if (fallbackType != null && toBaseType != null && !toBaseType.IsAssignableFrom(fallbackType))
                throw new ArgumentException(string.Format("{0} does not implement or derive from {1}.", fallbackType.Name, toBaseType.Name), "fallbackType");
            m_FromBaseType = fromBaseType ?? typeof(object);
            m_ToBaseType = toBaseType;
            m_FallbackType = fallbackType;
        }

        public void Add(TypeMapping mapping)
        {
            Add(mapping.fromType, mapping.toType);
        }

        public void Add(Type fromType, Type toType)
        {
            if (m_FromBaseType != typeof(object) && !m_FromBaseType.IsAssignableFrom(fromType))
            {
                throw new ArgumentException(string.Format("{0} does not implement or derive from {1}.", fromType.Name, m_FromBaseType.Name), "fromType");
            }

            if (m_ToBaseType != null && !m_ToBaseType.IsAssignableFrom(toType))
            {
                throw new ArgumentException(string.Format("{0} does not derive from {1}.", toType.Name, m_ToBaseType.Name), "toType");
            }

            m_Mappings[fromType] = toType;
        }

        public Type MapType(Type fromType)
        {
            Type toType = null;

            while (toType == null && fromType != null && fromType != m_FromBaseType)
            {
                if (!m_Mappings.TryGetValue(fromType, out toType))
                    fromType = fromType.BaseType;
            }

            return toType ?? m_FallbackType;
        }

        public IEnumerator<TypeMapping> GetEnumerator()
        {
            return m_Mappings.Select(kvp => new TypeMapping(kvp.Key, kvp.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
