using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Use this attribute to associate a <see cref="PostProcessEffectSettings"/> to a
    /// <see cref="PostProcessEffectRenderer{T}"/> type.
    /// </summary>
    /// <seealso cref="PostProcessEffectSettings"/>
    /// <seealso cref="PostProcessEffectRenderer{T}"/>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PostProcessAttribute : Attribute
    {
        /// <summary>
        /// The renderer type to associate with a <see cref="PostProcessEffectSettings"/>.
        /// </summary>
        public readonly Type renderer;

        /// <summary>
        /// The injection point for the effect.
        /// </summary>
        public readonly PostProcessEvent eventType;

        /// <summary>
        /// The menu item name to set for the effect. You can use a `/` character to add sub-menus.
        /// </summary>
        public readonly string menuItem;

        /// <summary>
        /// Should this effect be allowed in the Scene View?
        /// </summary>
        public readonly bool allowInSceneView;

        internal readonly bool builtinEffect;

        /// <summary>
        /// Creates a new attribute.
        /// </summary>
        /// <param name="renderer">The renderer type to associate with a <see cref="PostProcessEffectSettings"/></param>
        /// <param name="eventType">The injection point for the effect</param>
        /// <param name="menuItem">The menu item name to set for the effect. You can use a `/` character to add sub-menus.</param>
        /// <param name="allowInSceneView">Should this effect be allowed in the Scene View?</param>
        public PostProcessAttribute(Type renderer, PostProcessEvent eventType, string menuItem, bool allowInSceneView = true)
        {
            this.renderer = renderer;
            this.eventType = eventType;
            this.menuItem = menuItem;
            this.allowInSceneView = allowInSceneView;
            builtinEffect = false;
        }

        internal PostProcessAttribute(Type renderer, string menuItem, bool allowInSceneView = true)
        {
            this.renderer = renderer;
            this.menuItem = menuItem;
            this.allowInSceneView = allowInSceneView;
            builtinEffect = true;
        }
    }
}
