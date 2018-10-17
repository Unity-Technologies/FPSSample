using System;
using System.Linq;

namespace UnityEditor.Rendering.PostProcessing
{
    public sealed class SerializedParameterOverride
    {
        public SerializedProperty overrideState { get; private set; }
        public SerializedProperty value { get; private set; }
        public Attribute[] attributes { get; private set; }

        internal SerializedProperty baseProperty;

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

        public T GetAttribute<T>()
            where T : Attribute
        {
            return (T)attributes.FirstOrDefault(x => x is T);
        }
    }
}
