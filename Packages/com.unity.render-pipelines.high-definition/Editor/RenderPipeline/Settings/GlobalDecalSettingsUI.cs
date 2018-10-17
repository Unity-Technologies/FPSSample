namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<GlobalDecalSettingsUI, SerializedGlobalDecalSettings>;

    class GlobalDecalSettingsUI : BaseUI<SerializedGlobalDecalSettings>
    {
        static GlobalDecalSettingsUI()
        {
            Inspector = CED.Group(SectionDecalSettings);
        }

        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SectionDecalSettings = CED.FoldoutGroup(
            "Decals",
            (s, d, o) => s.isSectionExpendedDecalSettings,
            FoldoutOption.None,
            CED.Action(Drawer_SectionDecalSettings)
            );

        AnimatedValues.AnimBool isSectionExpendedDecalSettings { get { return m_AnimBools[0]; } }

        public GlobalDecalSettingsUI()
            : base(1)
        {
            isSectionExpendedDecalSettings.value = true;
        }

        static void Drawer_SectionDecalSettings(GlobalDecalSettingsUI s, SerializedGlobalDecalSettings d, Editor o)
        {
            EditorGUILayout.PropertyField(d.drawDistance, _.GetContent("Draw Distance"));
            EditorGUILayout.PropertyField(d.atlasWidth, _.GetContent("Atlas Width"));
            EditorGUILayout.PropertyField(d.atlasHeight, _.GetContent("Atlas Height"));
            EditorGUILayout.PropertyField(d.perChannelMask, _.GetContent("Enable Metal and AO properties"));
        }
    }
}
