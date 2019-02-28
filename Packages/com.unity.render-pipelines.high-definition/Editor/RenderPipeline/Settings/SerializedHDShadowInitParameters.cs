using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedHDShadowInitParameters
    {
        public SerializedProperty root;

        public SerializedProperty shadowAtlasResolution;
        public SerializedProperty shadowMapDepthBits;
        public SerializedProperty useDynamicViewportRescale;

        public SerializedProperty maxShadowRequests;

        public SerializedProperty shadowQuality;

        public SerializedHDShadowInitParameters(SerializedProperty root)
        {
            this.root = root;

            shadowAtlasResolution = root.Find((HDShadowInitParameters s) => s.shadowAtlasResolution);
            shadowMapDepthBits = root.Find((HDShadowInitParameters s) => s.shadowMapsDepthBits);
            useDynamicViewportRescale = root.Find((HDShadowInitParameters s) => s.useDynamicViewportRescale);
            maxShadowRequests = root.Find((HDShadowInitParameters s) => s.maxShadowRequests);
            shadowQuality = root.Find((HDShadowInitParameters s) => s.shadowQuality);
        }
    }
}
