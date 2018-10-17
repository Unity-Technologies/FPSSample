using System;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;

namespace UnityEditor.Experimental.Rendering
{
    public sealed class SerializedDataParameter
    {
        public SerializedProperty overrideState { get; private set; }
        public SerializedProperty value { get; private set; }
        public Attribute[] attributes { get; private set; }
        public Type referenceType { get; private set; }

        SerializedProperty m_BaseProperty;
        object m_ReferenceValue;

        public string displayName
        {
            get { return m_BaseProperty.displayName; }
        }

        internal SerializedDataParameter(SerializedProperty property)
        {
            // Find the actual property type, optional attributes & reference
            var path = property.propertyPath.Split('.');
            object obj = property.serializedObject.targetObject;
            FieldInfo field = null;

            foreach (var p in path)
            {
                field = obj.GetType().GetField(p, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                obj = field.GetValue(obj);
            }

            Assert.IsNotNull(field);

            m_BaseProperty = property.Copy();
            overrideState = m_BaseProperty.FindPropertyRelative("m_OverrideState");
            value = m_BaseProperty.FindPropertyRelative("m_Value");
            attributes = field.GetCustomAttributes(false).Cast<Attribute>().ToArray();
            referenceType = obj.GetType();
            m_ReferenceValue = obj;
        }

        public T GetAttribute<T>()
            where T : Attribute
        {
            return (T)attributes.FirstOrDefault(x => x is T);
        }

        public T GetObjectRef<T>()
        {
            return (T)m_ReferenceValue;
        }
    }
}
