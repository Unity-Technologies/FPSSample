using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    public class DefaultPostProcessEffectEditor : PostProcessEffectBaseEditor
    {
        List<SerializedParameterOverride> m_Parameters; 

        public override void OnEnable()
        {
            m_Parameters = new List<SerializedParameterOverride>();

            var fields = target.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(t => t.FieldType.IsSubclassOf(typeof(ParameterOverride)) && t.Name != "enabled")
                .Where(t =>
                    (t.IsPublic && t.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length == 0)
                    || (t.GetCustomAttributes(typeof(UnityEngine.SerializeField), false).Length > 0)
                )
                .ToList();

            foreach (var field in fields)
            {
                var property = serializedObject.FindProperty(field.Name);
                var attributes = field.GetCustomAttributes(false).Cast<Attribute>().ToArray();
                var parameter = new SerializedParameterOverride(property, attributes);
                m_Parameters.Add(parameter);
            }
        }

        public override void OnInspectorGUI()
        {
            foreach (var parameter in m_Parameters)
                PropertyField(parameter);
        }
    }
}
