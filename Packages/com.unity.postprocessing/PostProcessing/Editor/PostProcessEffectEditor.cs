using System;
using System.Linq.Expressions;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    public class PostProcessEffectEditor<T> : PostProcessEffectBaseEditor
        where T : PostProcessEffectSettings
    {
        protected SerializedProperty FindProperty<TValue>(Expression<Func<T, TValue>> expr)
        {
            return serializedObject.FindProperty(RuntimeUtilities.GetFieldPath(expr));
        }

        protected SerializedParameterOverride FindParameterOverride<TValue>(Expression<Func<T, TValue>> expr)
        {
            var property = serializedObject.FindProperty(RuntimeUtilities.GetFieldPath(expr));
            var attributes = RuntimeUtilities.GetMemberAttributes(expr);
            return new SerializedParameterOverride(property, attributes);
        }
    }
}
