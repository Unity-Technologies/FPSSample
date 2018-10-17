using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // In HD we don't expose HDRenderQueue instead we create as much value as needed in the enum for our different pass
    // and use inspector to manipulate the value.
    // In the case of transparent we want to use RenderQueue to help with sorting. We define a neutral value for the RenderQueue and priority going from -X to +X
    // going from -X to +X instead of 0 to +X as builtin Unity is better for artists as they can decide late to sort behind or in front of the scene.

    public class HDRenderQueue
    {
        public const int k_TransparentPriorityQueueRange = 100;

        public enum Priority
        {
            Background = UnityEngine.Rendering.RenderQueue.Background,
            Opaque = UnityEngine.Rendering.RenderQueue.Geometry,
            OpaqueAlphaTest = UnityEngine.Rendering.RenderQueue.AlphaTest,
            // Warning: we must not change Geometry last value to stay compatible with occlusion
            OpaqueLast = UnityEngine.Rendering.RenderQueue.GeometryLast,
            // For transparent pass we define a range of 200 value to define the priority
            // Warning: Be sure no range are overlapping
            PreRefractionFirst = 2750 - k_TransparentPriorityQueueRange,
            PreRefraction = 2750,
            PreRefractionLast = 2750 + k_TransparentPriorityQueueRange,
            TransparentFirst = UnityEngine.Rendering.RenderQueue.Transparent - k_TransparentPriorityQueueRange,
            Transparent = UnityEngine.Rendering.RenderQueue.Transparent,
            TransparentLast = UnityEngine.Rendering.RenderQueue.Transparent + k_TransparentPriorityQueueRange,
            Overlay = UnityEngine.Rendering.RenderQueue.Overlay
        }

        public static readonly RenderQueueRange k_RenderQueue_OpaqueNoAlphaTest = new RenderQueueRange { min = (int)Priority.Opaque, max = (int)Priority.OpaqueAlphaTest - 1 };
        public static readonly RenderQueueRange k_RenderQueue_OpaqueAlphaTest = new RenderQueueRange { min = (int)Priority.OpaqueAlphaTest, max = (int)Priority.OpaqueLast };
        public static readonly RenderQueueRange k_RenderQueue_AllOpaque = new RenderQueueRange { min = (int)Priority.Opaque, max = (int)Priority.OpaqueLast };

        public static readonly RenderQueueRange k_RenderQueue_PreRefraction = new RenderQueueRange { min = (int)Priority.PreRefractionFirst, max = (int)Priority.PreRefractionLast };
        public static readonly RenderQueueRange k_RenderQueue_Transparent = new RenderQueueRange { min = (int)Priority.TransparentFirst, max = (int)Priority.TransparentLast };
        public static readonly RenderQueueRange k_RenderQueue_AllTransparent = new RenderQueueRange { min = (int)Priority.PreRefractionFirst, max = (int)Priority.TransparentLast };

        public static readonly RenderQueueRange k_RenderQueue_All = new RenderQueueRange { min = 0, max = 5000 };
    }
}
