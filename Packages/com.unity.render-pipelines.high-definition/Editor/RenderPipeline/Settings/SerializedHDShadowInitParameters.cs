using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedHDShadowInitParameters
    {
        public SerializedProperty root;

        public SerializedProperty shadowAtlasWidth;
        public SerializedProperty shadowAtlasHeight;
        public SerializedProperty shadowMapDepthBits;
        public SerializedProperty useDynamicViewportRescale;

        public SerializedProperty maxShadowRequests;

        public SerializedProperty punctualShadowQuality;
        public SerializedProperty directionalShadowQuality;

        public SerializedHDShadowInitParameters(SerializedProperty root)
        {
            this.root = root;

            shadowAtlasWidth = root.Find((HDShadowInitParameters s) => s.shadowAtlasWidth);
            shadowAtlasHeight = root.Find((HDShadowInitParameters s) => s.shadowAtlasHeight);
            shadowMapDepthBits = root.Find((HDShadowInitParameters s) => s.shadowMapsDepthBits);
            useDynamicViewportRescale = root.Find((HDShadowInitParameters s) => s.useDynamicViewportRescale);
            maxShadowRequests = root.Find((HDShadowInitParameters s) => s.maxShadowRequests);
            punctualShadowQuality = root.Find((HDShadowInitParameters s) => s.punctualShadowQuality);
            directionalShadowQuality = root.Find((HDShadowInitParameters s) => s.directionalShadowQuality);
        }
    }
}
