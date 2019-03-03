using System;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    /// <summary>
    /// A wrapper used for <see cref="ParameterOverride{T}"/> serialization and easy access to the
    /// underlying property and override state.
    /// </summary>
    public sealed class SerializedParameterOverride
    {
        /// <summary>
        /// The override state property of the serialized parameter.
        /// </summary>
        public SerializedProperty overrideState { get; private set; }

        /// <summary>
        /// The value property of the serialized parameter.
        /// </summary>
        public SerializedProperty value { get; private set; }

        /// <summary>
        /// An array of all attributes set on the original parameter.
        /// </summary>
        public Attribute[] attributes { get; private set; }

        internal SerializedProperty baseProperty;

        /// <summary>
        /// Returns the display name of the property.
        /// </summary>
        public string displayName
        {
            get { return baseProperty.displayName; }
        }

        internal SerializedParameterOverride(SerializedProperty property, Attribute[] attributes)
        {
            baseProperty = property.Copy();

            var localCopy = baseProperty.Copy();
            localCopy.Next(true);
            overrideState = localCopy.Copy();
            localCopy.Next(false);
            value = localCopy.Copy();

            this.attributes = attributes;
        }

        /// <summary>
        /// Gets the attribute of type <c>T</c> from the original parameter.
        /// </summary>
        /// <typeparam name="T">The type of attribute to look for</typeparam>
        /// <returns>And attribute or type <c>T</c>, or <c>null</c> if none has been found</returns>
        public T GetAttribute<T>()
            where T : Attribute
        {
            return (T)attributes.FirstOrDefault(x => x is T);
        }
    }
}
