using System;
using System.Reflection;
using UnityEditor;

namespace Unity.Entities.Editor
{
    [CustomEditor(typeof(ComponentDataWrapperBase), true), CanEditMultipleObjects]
    public class ComponentDataWrapperBaseEditor : UnityEditor.Editor
    {
        private string m_SerializableError;

        protected virtual void OnEnable()
        {
            var serializedDataProperty = serializedObject.FindProperty("m_SerializedData");
            if (serializedDataProperty != null)
                return;
            FieldInfo field = null;
            var type = target.GetType();
            while (type.BaseType != typeof(ComponentDataWrapperBase))
            {
                type = type.BaseType;
                field = type.GetField("m_SerializedData", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                    break;
            }
            if (field != null && !Attribute.IsDefined(field, typeof(SerializableAttribute)))
            {
                m_SerializableError = string.Format(
                    L10n.Tr("Component type {0} is not marked with {1}"), field.FieldType, typeof(SerializableAttribute)
                );
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!string.IsNullOrEmpty(m_SerializableError))
                EditorGUILayout.HelpBox(m_SerializableError, MessageType.Error);
        }
    }
}
