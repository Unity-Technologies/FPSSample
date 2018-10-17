using System;

namespace UnityEditor.Rendering.PostProcessing
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DecoratorAttribute : Attribute
    {
        public readonly Type attributeType;

        public DecoratorAttribute(Type attributeType)
        {
            this.attributeType = attributeType;
        }
    }
}
