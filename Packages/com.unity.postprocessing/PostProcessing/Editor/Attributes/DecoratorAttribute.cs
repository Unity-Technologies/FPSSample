using System;

namespace UnityEditor.Rendering.PostProcessing
{
    /// <summary>
    /// Tells a <see cref="AttributeDecorator"/> class which inspector attribute it's a decorator
    /// for.
    /// </summary>
    /// <seealso cref="AttributeDecorator"/>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DecoratorAttribute : Attribute
    {
        /// <summary>
        /// The attribute type that this decorator can inspect.
        /// </summary>
        public readonly Type attributeType;

        /// <summary>
        /// Creates a new attribute.
        /// </summary>
        /// <param name="attributeType">The type that this decorator can inspect</param>
        public DecoratorAttribute(Type attributeType)
        {
            this.attributeType = attributeType;
        }
    }
}
