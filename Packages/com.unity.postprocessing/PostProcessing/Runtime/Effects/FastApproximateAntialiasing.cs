using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the Fast Approximate Anti-aliasing (FXAA) effect.
    /// </summary>
    [Serializable]
    public sealed class FastApproximateAntialiasing
    {
        /// <summary>
        /// If <c>true</c>, it will use a slightly lower quality but faster variant of FXAA. Highly
        /// recommended on mobile platforms.
        /// </summary>
        [FormerlySerializedAs("mobileOptimized")]
        [Tooltip("Boost performances by lowering the effect quality. This setting is meant to be used on mobile and other low-end platforms but can also provide a nice performance boost on desktops and consoles.")]
        public bool fastMode = false;

        /// <summary>
        /// Set this to <c>true</c> if you need to keep the alpha channel untouched. Else it will
        /// use this channel to store internal data used to speed up and improve visual quality.
        /// </summary>
        [Tooltip("Keep alpha channel. This will slightly lower the effect quality but allows rendering against a transparent background.")]
        public bool keepAlpha = false;
    }
}
