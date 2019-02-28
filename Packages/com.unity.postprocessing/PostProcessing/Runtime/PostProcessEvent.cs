using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Injection points for custom effects.
    /// </summary>
    public enum PostProcessEvent
    {
        /// <summary>
        /// Effects at this injection points will execute before transparent objects are rendered.
        /// </summary>
        BeforeTransparent = 0,

        /// <summary>
        /// Effects at this injection points will execute after temporal anti-aliasing and before
        /// builtin effects are rendered.
        /// </summary>
        BeforeStack = 1,

        /// <summary>
        /// Effects at this injection points will execute after builtin effects have been rendered
        /// and before the final pass that does FXAA and applies dithering.
        /// </summary>
        AfterStack = 2,
    }

    // Box free comparer for our `PostProcessEvent` enum, else the runtime will box the type when
    // used  as a key in a dictionary, thus leading to garbage generation... *sigh*
    internal struct PostProcessEventComparer : IEqualityComparer<PostProcessEvent>
    {
        public bool Equals(PostProcessEvent x, PostProcessEvent y)
        {
            return x == y;
        }

        public int GetHashCode(PostProcessEvent obj)
        {
            return (int)obj;
        }
    }
}
