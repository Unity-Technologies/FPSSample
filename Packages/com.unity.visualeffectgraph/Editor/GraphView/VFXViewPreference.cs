using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    public static class VFXViewPreference
    {
        private static bool m_Loaded = false;
        private static bool m_DisplayExperimentalOperator = false;
        private static bool m_AllowShaderExternalization = false;
        private static bool m_DisplayExtraDebugInfo = false;
        private static bool m_ForceEditionCompilation = false;
        private static bool m_AdvancedLogs = false;

        public static bool displayExperimentalOperator
        {
            get
            {
                LoadIfNeeded();
                return m_DisplayExperimentalOperator;
            }
        }

        public static bool displayExtraDebugInfo
        {
            get
            {
                LoadIfNeeded();
                return m_DisplayExtraDebugInfo;
            }
        }

        public static bool advancedLogs
        {
            get
            {
                LoadIfNeeded();
                return m_AdvancedLogs;
            }
        }

        public static bool forceEditionCompilation
        {
            get
            {
                LoadIfNeeded();
                return m_ForceEditionCompilation;
            }
        }

        public const string experimentalOperatorKey = "VFX.displayExperimentalOperatorKey";
        public const string extraDebugInfoKey = "VFX.ExtraDebugInfo";
        public const string forceEditionCompilationKey = "VFX.ForceEditionCompilation";
        public const string allowShaderExternalizationKey = "VFX.allowShaderExternalization";
        public const string advancedLogsKey = "VFX.AdvancedLogs";

        private static void LoadIfNeeded()
        {
            if (!m_Loaded)
            {
                m_DisplayExperimentalOperator = EditorPrefs.GetBool(experimentalOperatorKey, false);
                m_DisplayExtraDebugInfo = EditorPrefs.GetBool(extraDebugInfoKey, false);
                m_ForceEditionCompilation = EditorPrefs.GetBool(forceEditionCompilationKey, false);
                m_AllowShaderExternalization = EditorPrefs.GetBool(allowShaderExternalizationKey, false);
                m_AdvancedLogs = EditorPrefs.GetBool(advancedLogsKey, false);
                m_Loaded = true;
            }
        }

        class VFXSettingsProvider : SettingsProvider
        {
            public VFXSettingsProvider() : base("Visual Effects", SettingsScope.User)
            {
                hasSearchInterestHandler = HasSearchInterestHandler;
            }

            bool HasSearchInterestHandler(string searchContext)
            {
                return true;
            }

            public override void OnGUI(string searchContext)
            {
                using (new SettingsWindow.GUIScope())
                {
                    LoadIfNeeded();
                    m_DisplayExperimentalOperator = EditorGUILayout.Toggle("Experimental Operators/Blocks", m_DisplayExperimentalOperator);
                    m_DisplayExtraDebugInfo = EditorGUILayout.Toggle("Show Additional Debug info", m_DisplayExtraDebugInfo);
                    m_AdvancedLogs = EditorGUILayout.Toggle("Verbose Mode for compilation", m_AdvancedLogs);
                    m_AllowShaderExternalization = EditorGUILayout.Toggle("Experimental shader externalization", m_AllowShaderExternalization);

                    bool oldForceEditionCompilation = m_ForceEditionCompilation;
                    m_ForceEditionCompilation = EditorGUILayout.Toggle("Force Compilation in Edition Mode", m_ForceEditionCompilation);
                    if (m_ForceEditionCompilation != oldForceEditionCompilation)
                    {
                        // TODO Factorize that somewhere
                        var vfxAssets = new HashSet<VisualEffectAsset>();
                        var vfxAssetsGuid = AssetDatabase.FindAssets("t:VisualEffectAsset");
                        foreach (var guid in vfxAssetsGuid)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
                            if (vfxAsset != null)
                                vfxAssets.Add(vfxAsset);
                        }

                        foreach (var vfxAsset in vfxAssets)
                            vfxAsset.GetResource().GetOrCreateGraph().SetCompilationMode(m_ForceEditionCompilation ? VFXCompilationMode.Edition : VFXCompilationMode.Runtime);
                    }

                    if (GUI.changed)
                    {
                        EditorPrefs.SetBool(experimentalOperatorKey, m_DisplayExperimentalOperator);
                        EditorPrefs.SetBool(extraDebugInfoKey, m_DisplayExtraDebugInfo);
                        EditorPrefs.SetBool(forceEditionCompilationKey, m_ForceEditionCompilation);
                        EditorPrefs.SetBool(advancedLogsKey, m_AdvancedLogs);
                        EditorPrefs.SetBool(allowShaderExternalizationKey, m_AllowShaderExternalization);
                    }
                }

                base.OnGUI(searchContext);
            }
        }

        [SettingsProvider]
        public static SettingsProvider PreferenceSettingsProvider()
        {
            return new VFXSettingsProvider();
        }

        public static void PreferencesGUI()
        {
        }
    }
}
