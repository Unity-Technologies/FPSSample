using System;

namespace UnityEngine.Experimental.Rendering
{
   [Flags]
    public enum ClearFlag
    {
        None  = 0,
        Color = 1,
        Depth = 2,

        All = Depth | Color
    }
}