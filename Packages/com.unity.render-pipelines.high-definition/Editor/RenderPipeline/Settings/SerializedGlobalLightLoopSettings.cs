using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedGlobalLightLoopSettings
    {
        public SerializedProperty root;

        public SerializedProperty cookieSize;
        public SerializedProperty cookieTexArraySize;
        public SerializedProperty pointCookieSize;
        public SerializedProperty cubeCookieTexArraySize;
        public SerializedProperty reflectionProbeCacheSize;
        public SerializedProperty reflectionCubemapSize;
        public SerializedProperty reflectionCacheCompressed;
        public SerializedProperty planarReflectionProbeCacheSize;
        public SerializedProperty planarReflectionCubemapSize;
        public SerializedProperty planarReflectionCacheCompressed;
        public SerializedProperty skyReflectionSize;
        public SerializedProperty skyLightingOverrideLayerMask;

        public SerializedGlobalLightLoopSettings(SerializedProperty root)
        {
            this.root = root;

            cookieSize = root.Find((GlobalLightLoopSettings s) => s.cookieSize);
            cookieTexArraySize = root.Find((GlobalLightLoopSettings s) => s.cookieTexArraySize);
            pointCookieSize = root.Find((GlobalLightLoopSettings s) => s.pointCookieSize);
            cubeCookieTexArraySize = root.Find((GlobalLightLoopSettings s) => s.cubeCookieTexArraySize);

            reflectionProbeCacheSize = root.Find((GlobalLightLoopSettings s) => s.reflectionProbeCacheSize);
            reflectionCubemapSize = root.Find((GlobalLightLoopSettings s) => s.reflectionCubemapSize);
            reflectionCacheCompressed = root.Find((GlobalLightLoopSettings s) => s.reflectionCacheCompressed);

            planarReflectionProbeCacheSize = root.Find((GlobalLightLoopSettings s) => s.planarReflectionProbeCacheSize);
            planarReflectionCubemapSize = root.Find((GlobalLightLoopSettings s) => s.planarReflectionTextureSize);
            planarReflectionCacheCompressed = root.Find((GlobalLightLoopSettings s) => s.planarReflectionCacheCompressed);

            skyReflectionSize = root.Find((GlobalLightLoopSettings s) => s.skyReflectionSize);
            skyLightingOverrideLayerMask = root.Find((GlobalLightLoopSettings s) => s.skyLightingOverrideLayerMask);
        }
    }
}
