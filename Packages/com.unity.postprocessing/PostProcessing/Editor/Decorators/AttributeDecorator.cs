using System;
using UnityEngine;

namespace UnityEditor.Rendering.PostProcessing
{
    /// <summary>
    /// The base abstract class for all attribute decorators.
    /// </summary>
    public abstract class AttributeDecorator
    {
        /// <summary>
        /// Override this and return <c>false</c> if you want to customize the override checkbox
        /// position, else it'll automatically draw it and put the property content in a
        /// horizontal scope.
        /// </summary>
        /// <returns><c>true</c> if the override checkbox should be automatically put next to the
        /// property, <c>false</c> if it uses a custom position</returns>
        public virtual bool IsAutoProperty()
        {
            return true;
        }

        /// <summary>
        /// The rendering method called for the custom GUI.
        /// </summary>
        /// <param name="property">The property to draw the UI for</param>
        /// <param name="overrideState">The override checkbox property</param>
        /// <param name="title">The title and tooltip for the property</param>
        /// <param name="attribute">A reference to the property attribute set on the original field
        /// </param>
        /// <returns><c>true</c> if the property UI got rendered successfully, <c>false</c> to
        /// fallback on the default editor UI for this property</returns>
        public abstract bool OnGUI(SerializedProperty property, SerializedProperty overrideState, GUIContent title, Attribute attribute);
    }
}
