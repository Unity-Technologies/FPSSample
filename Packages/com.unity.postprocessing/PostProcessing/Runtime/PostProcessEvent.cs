using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    public enum PostProcessEvent
    {
        BeforeTransparent = 0,
        BeforeStack = 1,
        AfterStack = 2,
    }

    // Box free comparer for our `PostProcessEvent` enum, else the runtime will box the type when
    // used  as a key in a dictionary, thus leading to garbage generation... *sigh*
    public struct PostProcessEventComparer : IEqualityComparer<PostProcessEvent>
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
