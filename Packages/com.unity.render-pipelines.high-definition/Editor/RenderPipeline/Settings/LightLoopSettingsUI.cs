using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<LightLoopSettingsUI, SerializedLightLoopSettings>;

    class LightLoopSettingsUI : BaseUI<SerializedLightLoopSettings>
    {
        const string lightLoopSettingsHeaderContent = "Light Loop Settings";
        // Uncomment if you re-enable LIGHTLOOP_SINGLE_PASS multi_compile in lit*.shader
        //static readonly GUIContent tileAndClusterContent = CoreEditorUtils.GetContent("Enable Tile And Cluster");
        static readonly GUIContent fptlForForwardOpaqueContent = CoreEditorUtils.GetContent("FPTL For Forward Opaque");
        static readonly GUIContent bigTilePrepassContent = CoreEditorUtils.GetContent("Big Tile Prepass");
        static readonly GUIContent computeLightEvaluationContent = CoreEditorUtils.GetContent("Compute Light Evaluation");
        static readonly GUIContent computeLightVariantsContent = CoreEditorUtils.GetContent("Compute Light Variants");
        static readonly GUIContent computeMaterialVariantsContent = CoreEditorUtils.GetContent("Compute Material Variants");

#pragma warning disable 618 //CED
        public static CED.IDrawer SectionLightLoopSettings(bool withOverride)
#pragma warning restore 618
        {
            return CED.FoldoutGroup(
                    lightLoopSettingsHeaderContent,
                    (s, p, o) => s.isSectionExpandedLightLoopSettings,
                    FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.LabelWidth(250, CED.Action((s, p, o) => Drawer_SectionLightLoopSettings(s, p, o, withOverride))),
                    CED.space
                );
        }

        public AnimBool isSectionExpandedLightLoopSettings { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedEnableTileAndCluster { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedComputeLightEvaluation { get { return m_AnimBools[2]; } }

        public LightLoopSettingsUI()
            : base(3)
        {
        }

        public override void Update()
        {
            isSectionExpandedEnableTileAndCluster.target = data.enableTileAndCluster.boolValue;
            isSectionExpandedComputeLightEvaluation.target = data.enableComputeLightEvaluation.boolValue;
            base.Update();
        }

        static void Drawer_SectionLightLoopSettings(LightLoopSettingsUI s, SerializedLightLoopSettings p, Editor owner, bool withOverride)
        {
            //disable temporarily as FrameSettings are nort supported for Baked probe at the moment
            using (new EditorGUI.DisabledScope((owner is HDProbeEditor) && (owner as HDProbeEditor).GetTarget(owner.target).mode != UnityEngine.Rendering.ReflectionProbeMode.Realtime || (owner is HDRenderPipelineEditor) && HDRenderPipelineUI.selectedFrameSettings == HDRenderPipelineUI.SelectedFrameSettings.BakedOrCustomReflection))
            {
                //RenderPipelineSettings hdrpSettings = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineSettings;
                OverridableSettingsArea area = new OverridableSettingsArea(6);
                FrameSettings defaultFrameSettings = FrameSettingsUI.GetDefaultFrameSettingsFor(owner);

                // Uncomment if you re-enable LIGHTLOOP_SINGLE_PASS multi_compile in lit*.shader
                //area.Add(p.enableTileAndCluster, tileAndClusterContent, a => p.overridesTileAndCluster = a, () => p.overridesTileAndCluster);
                //and add indent:1 or indent:2 regarding indentation you want

                if (s.isSectionExpandedEnableTileAndCluster.target)
                {
                    area.Add(p.enableFptlForForwardOpaque, fptlForForwardOpaqueContent, () => p.overridesFptlForForwardOpaque, a => p.overridesFptlForForwardOpaque = a, defaultValue: defaultFrameSettings.lightLoopSettings.enableFptlForForwardOpaque);
                    area.Add(p.enableBigTilePrepass, bigTilePrepassContent, () => p.overridesBigTilePrepass, a => p.overridesBigTilePrepass = a, defaultValue: defaultFrameSettings.lightLoopSettings.enableBigTilePrepass);
                    area.Add(p.enableComputeLightEvaluation, computeLightEvaluationContent, () => p.overridesComputeLightEvaluation, a => p.overridesComputeLightEvaluation = a, defaultValue: defaultFrameSettings.lightLoopSettings.enableComputeLightEvaluation);
                    if (s.isSectionExpandedComputeLightEvaluation.target)
                    {
                        area.Add(p.enableComputeLightVariants, computeLightVariantsContent, () => p.overridesComputeLightVariants, a => p.overridesComputeLightVariants = a, defaultValue: defaultFrameSettings.lightLoopSettings.enableComputeLightVariants, indent: 1);
                        area.Add(p.enableComputeMaterialVariants, computeMaterialVariantsContent, () => p.overridesComputeMaterialVariants, a => p.overridesComputeMaterialVariants = a, defaultValue: defaultFrameSettings.lightLoopSettings.enableComputeMaterialVariants, indent: 1);
                    }
                }

                area.Draw(withOverride);
            }
        }
    }
}
