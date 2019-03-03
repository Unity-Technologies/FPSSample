using System;
using System.Linq.Expressions;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    /// <summary>
    /// The class to inherit from when designing custom effect editors.
    /// </summary>
    /// <typeparam name="T">The effect type to create an editor for</typeparam>
    public class PostProcessEffectEditor<T> : PostProcessEffectBaseEditor
        where T : PostProcessEffectSettings
    {
        /// <summary>
        /// Find a serialized property using an expression instead of a string. This is safer as it
        /// helps avoiding typos and make code refactoring easier.
        /// </summary>
        /// <typeparam name="TValue">The serialized value type</typeparam>
        /// <param name="expr">The expression to parse to reach the property</param>
        /// <returns>A <see cref="SerializedProperty"/> or <c>null</c> if none was found</returns>
        /// <example>
        /// <code>
        /// [Serializable]
        /// public class MyEffect : PostProcessEffectSettings
        /// {
        ///     public float myParameter = 1f;
        /// }
        /// 
        /// [PostProcessEditor(typeof(MyEffect))]
        /// public class MyEffectEditor : PostProcessEffectEditor&lt;MyEffect&gt;
        /// {
        ///     SerializedProperty m_MyParameter;
        /// 
        ///     public override void OnEnable()
        ///     {
        ///         m_MyParameter = FindProperty(x => x.myParameter);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// If you're trying to retrieve a <see cref="SerializedParameterOverride"/>, you should
        /// use <seealso cref="FindParameterOverride{TValue}"/> instead.
        /// </remarks>
        /// <seealso cref="SerializedProperty"/>
        /// <seealso cref="FindParameterOverride{TValue}"/>
        protected SerializedProperty FindProperty<TValue>(Expression<Func<T, TValue>> expr)
        {
            return serializedObject.FindProperty(RuntimeUtilities.GetFieldPath(expr));
        }

        /// <summary>
        /// Find a serialized parameter override using an expression instead of a string. This is
        /// safer as it helps avoiding typos and make code refactoring easier.
        /// </summary>
        /// <typeparam name="TValue">The serialized value type</typeparam>
        /// <param name="expr">The expression to parse to reach the parameter override</param>
        /// <returns>A <see cref="SerializedParameterOverride"/> or <c>null</c> if none was
        /// found</returns>
        /// <example>
        /// <code>
        /// [Serializable]
        /// public class MyEffect : PostProcessEffectSettings
        /// {
        ///     public FloatParameter myParameter = new FloatParameter { value = 1f };
        /// }
        /// 
        /// [PostProcessEditor(typeof(MyEffect))]
        /// public class MyEffectEditor : PostProcessEffectEditor&lt;MyEffect&gt;
        /// {
        ///     SerializedParameterOverride m_MyParameter;
        /// 
        ///     public override void OnEnable()
        ///     {
        ///         m_MyParameter = FindParameterOverride(x => x.myParameter);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="SerializedParameterOverride"/>
        protected SerializedParameterOverride FindParameterOverride<TValue>(Expression<Func<T, TValue>> expr)
        {
            var property = serializedObject.FindProperty(RuntimeUtilities.GetFieldPath(expr));
            var attributes = RuntimeUtilities.GetMemberAttributes(expr);
            return new SerializedParameterOverride(property, attributes);
        }
    }
}
