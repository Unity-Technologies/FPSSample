using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<GlobalLightLoopSettingsUI, SerializedGlobalLightLoopSettings>;

    class GlobalLightLoopSettingsUI : BaseUI<SerializedGlobalLightLoopSettings>
    {
        const string cacheErrorFormat = "This configuration will lead to more than 2 GB reserved for this cache at runtime! ({0} requested) Only {1} element will be reserved instead.";
        const string cacheInfoFormat = "Reserving {0} in memory at runtime.";

        static GlobalLightLoopSettingsUI()
        {
            Inspector = CED.Group(
                CED.FoldoutGroup(
                    "Cookies",
                    (s,d,o) => s.isSectionExpandedCoockiesSettings,
                    FoldoutOption.None,
                    SectionCookies),
                CED.FoldoutGroup(
                    "Reflections",
                    (s, d, o) => s.isSectionExpandedReflectionSettings,
                    FoldoutOption.None,
                    SectionReflection),
                CED.FoldoutGroup(
                    "Sky",
                    (s, d, o) => s.isSectionExpendedSkySettings,
                    FoldoutOption.None,
                    SectionSky),
                CED.FoldoutGroup(
                    "LightLoop",
                    (s, d, o) => s.isSectionExpendedLightLoopSettings,
                    FoldoutOption.None,
                    SectionLightLoop)
                );
        }

#pragma warning disable 618 //CED
        public static readonly CED.IDrawer Inspector;
        
        public static readonly CED.IDrawer SectionCookies = CED.Action(Drawer_SectionCookies);
        public static readonly CED.IDrawer SectionReflection = CED.Action(Drawer_SectionReflection);
        public static readonly CED.IDrawer SectionSky = CED.Action(Drawer_SectionSky);
        public static readonly CED.IDrawer SectionLightLoop = CED.Action(Drawer_LightLoop);
#pragma warning restore 618

        public AnimBool isSectionExpandedCoockiesSettings { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedReflectionSettings { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpendedSkySettings { get { return m_AnimBools[2]; } }
        public AnimBool isSectionExpendedLightLoopSettings { get { return m_AnimBools[3]; } }

        public GlobalLightLoopSettingsUI()
            : base(4)
        {
            isSectionExpandedCoockiesSettings.value = true;
            isSectionExpandedReflectionSettings.value = true;
            isSectionExpendedSkySettings.value = true;
            isSectionExpendedLightLoopSettings.value = true;
        }

        static string HumanizeWeight(long weightInByte)
        {
            if (weightInByte < 500)
            {
                return weightInByte + " B";
            }
            else if (weightInByte < 500000L)
            {
                float res = weightInByte / 1000f;
                return res.ToString("n2") + " KB";
            }
            else if (weightInByte < 500000000L)
            {
                float res = weightInByte / 1000000f;
                return res.ToString("n2") + " MB";
            }
            else
            {
                float res = weightInByte / 1000000000f;
                return res.ToString("n2") + " GB";
            }
        }

        static void Drawer_SectionCookies(GlobalLightLoopSettingsUI s, SerializedGlobalLightLoopSettings d, Editor o)
        {
            EditorGUILayout.PropertyField(d.cookieSize, _.GetContent("Cookie Size"));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(d.cookieTexArraySize, _.GetContent("Texture Array Size"));
            if (EditorGUI.EndChangeCheck())
            {
                d.cookieTexArraySize.intValue = Mathf.Clamp(d.cookieTexArraySize.intValue, 1, TextureCache.k_MaxSupported);
            }
            long currentCache = TextureCache2D.GetApproxCacheSizeInByte(d.cookieTexArraySize.intValue, d.cookieSize.intValue, 1);
            if (currentCache > LightLoop.k_MaxCacheSize)
            {
                int reserved = TextureCache2D.GetMaxCacheSizeForWeightInByte(LightLoop.k_MaxCacheSize, d.cookieSize.intValue, 1);
                string message = string.Format(cacheErrorFormat, HumanizeWeight(currentCache), reserved);
                EditorGUILayout.HelpBox(message, MessageType.Error);
            }
            else
            {
                string message = string.Format(cacheInfoFormat, HumanizeWeight(currentCache));
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
            EditorGUILayout.PropertyField(d.pointCookieSize, _.GetContent("Point Cookie Size"));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(d.cubeCookieTexArraySize, _.GetContent("Cubemap Array Size"));
            if (EditorGUI.EndChangeCheck())
            {
                d.cubeCookieTexArraySize.intValue = Mathf.Clamp(d.cubeCookieTexArraySize.intValue, 1, TextureCache.k_MaxSupported);
            }
            currentCache = TextureCacheCubemap.GetApproxCacheSizeInByte(d.cubeCookieTexArraySize.intValue, d.pointCookieSize.intValue, 1);
            if (currentCache > LightLoop.k_MaxCacheSize)
            {
                int reserved = TextureCacheCubemap.GetMaxCacheSizeForWeightInByte(LightLoop.k_MaxCacheSize, d.pointCookieSize.intValue, 1);
                string message = string.Format(cacheErrorFormat, HumanizeWeight(currentCache), reserved);
                EditorGUILayout.HelpBox(message, MessageType.Error);
            }
            else
            {
                string message = string.Format(cacheInfoFormat, HumanizeWeight(currentCache));
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
            EditorGUILayout.Space();
        }

        static void Drawer_SectionReflection(GlobalLightLoopSettingsUI s, SerializedGlobalLightLoopSettings d, Editor o)
        {
            EditorGUILayout.PropertyField(d.reflectionCacheCompressed, _.GetContent("Compress Reflection Probe Cache"));
            EditorGUILayout.PropertyField(d.reflectionCubemapSize, _.GetContent("Reflection Cubemap Size"));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(d.reflectionProbeCacheSize, _.GetContent("Probe Cache Size"));
            if (EditorGUI.EndChangeCheck())
            {
                d.reflectionProbeCacheSize.intValue = Mathf.Clamp(d.reflectionProbeCacheSize.intValue, 1, TextureCache.k_MaxSupported);
            }
            long currentCache = ReflectionProbeCache.GetApproxCacheSizeInByte(d.reflectionProbeCacheSize.intValue, d.reflectionCubemapSize.intValue, d.supportFabricConvolution.boolValue ? 2 : 1);
            if (currentCache > LightLoop.k_MaxCacheSize)
            {
                int reserved = ReflectionProbeCache.GetMaxCacheSizeForWeightInByte(LightLoop.k_MaxCacheSize, d.reflectionCubemapSize.intValue, d.supportFabricConvolution.boolValue ? 2 : 1);
                string message = string.Format(cacheErrorFormat, HumanizeWeight(currentCache), reserved);
                EditorGUILayout.HelpBox(message, MessageType.Error);
            }
            else
            {
                string message = string.Format(cacheInfoFormat, HumanizeWeight(currentCache));
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(d.planarReflectionCacheCompressed, _.GetContent("Compress Planar Reflection Probe Cache"));
            EditorGUILayout.PropertyField(d.planarReflectionCubemapSize, _.GetContent("Planar Reflection Texture Size"));
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(d.planarReflectionProbeCacheSize, _.GetContent("Planar Probe Cache Size"));
            if (EditorGUI.EndChangeCheck())
            {
                d.planarReflectionProbeCacheSize.intValue = Mathf.Clamp(d.planarReflectionProbeCacheSize.intValue, 1, TextureCache.k_MaxSupported);
            }
            currentCache = PlanarReflectionProbeCache.GetApproxCacheSizeInByte(d.planarReflectionProbeCacheSize.intValue, d.planarReflectionCubemapSize.intValue, 1);
            if (currentCache > LightLoop.k_MaxCacheSize)
            {
                int reserved = PlanarReflectionProbeCache.GetMaxCacheSizeForWeightInByte(LightLoop.k_MaxCacheSize, d.planarReflectionCubemapSize.intValue, 1);
                string message = string.Format(cacheErrorFormat, HumanizeWeight(currentCache), reserved);
                EditorGUILayout.HelpBox(message, MessageType.Error);
            }
            else
            {
                string message = string.Format(cacheInfoFormat, HumanizeWeight(currentCache));
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }

            EditorGUILayout.PropertyField(d.supportFabricConvolution, _.GetContent("Support Fabric BSDF Convolution."));
            EditorGUILayout.Space();
        }

        static void Drawer_SectionSky(GlobalLightLoopSettingsUI s, SerializedGlobalLightLoopSettings d, Editor o)
        {
            EditorGUILayout.PropertyField(d.skyReflectionSize, _.GetContent("Reflection Size"));
            EditorGUILayout.PropertyField(d.skyLightingOverrideLayerMask, _.GetContent("Lighting Override Mask|This layer mask will define in which layers the sky system will look for sky settings volumes for lighting override"));
            if (d.skyLightingOverrideLayerMask.intValue == -1)
            {
                EditorGUILayout.HelpBox("Be careful, Sky Lighting Override Mask is set to Everything. This is most likely a mistake as it serves no purpose.", MessageType.Warning);
            }
            EditorGUILayout.Space();
        }

        static void Drawer_LightLoop(GlobalLightLoopSettingsUI s, SerializedGlobalLightLoopSettings d, Editor o)
        {
            EditorGUILayout.DelayedIntField(d.maxDirectionalLightsOnScreen, _.GetContent("Max Directional Lights On Screen"));
            EditorGUILayout.DelayedIntField(d.maxPunctualLightsOnScreen, _.GetContent("Max Punctual Lights On Screen"));
            EditorGUILayout.DelayedIntField(d.maxAreaLightsOnScreen, _.GetContent("Max Area Lights On Screen"));
            EditorGUILayout.DelayedIntField(d.maxEnvLightsOnScreen, _.GetContent("Max Env Lights On Screen"));
            EditorGUILayout.DelayedIntField(d.maxDecalsOnScreen, _.GetContent("Max Decals On Screen"));
            
            d.maxDirectionalLightsOnScreen.intValue = Mathf.Clamp(d.maxDirectionalLightsOnScreen.intValue, 1, LightLoop.k_MaxDirectionalLightsOnScreen);
            d.maxPunctualLightsOnScreen.intValue = Mathf.Clamp(d.maxPunctualLightsOnScreen.intValue, 1, LightLoop.k_MaxPunctualLightsOnScreen);
            d.maxAreaLightsOnScreen.intValue = Mathf.Clamp(d.maxAreaLightsOnScreen.intValue, 1, LightLoop.k_MaxAreaLightsOnScreen);
            d.maxEnvLightsOnScreen.intValue = Mathf.Clamp(d.maxEnvLightsOnScreen.intValue, 1, LightLoop.k_MaxEnvLightsOnScreen);
            d.maxDecalsOnScreen.intValue = Mathf.Clamp(d.maxDecalsOnScreen.intValue, 1, LightLoop.k_MaxDecalsOnScreen);
        }
    }
}
