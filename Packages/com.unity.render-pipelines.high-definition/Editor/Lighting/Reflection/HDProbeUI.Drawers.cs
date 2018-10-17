using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<HDProbeUI, SerializedHDProbe>;

    partial class HDProbeUI
    {
        public static readonly CED.IDrawer[] Inspector;
        
        static readonly CED.IDrawer SectionPrimarySettings = CED.Group(
            CED.Action(Drawer_ReflectionProbeMode),
            CED.FadeGroup((s, p, o, i) => s.IsSectionExpandedReflectionProbeMode((ReflectionProbeMode)i),
                FadeOption.Indent,
                CED.space,                                              // Baked
                CED.Action(Drawer_SectionProbeModeRealtimeSettings),    // Realtime
                CED.Action(Drawer_ModeSettingsCustom)                   // Custom
                )
            );

        public static readonly CED.IDrawer SectionBakeButton = CED.Action(Drawer_SectionBakeButton);
        
        public static readonly CED.IDrawer SectionToolbar = CED.Group(
            CED.Action(Drawer_Toolbars),
            CED.space
            );

        public static readonly CED.IDrawer SectionProxyVolumeSettings = CED.FoldoutGroup(
            proxySettingsHeader,
            (s, d, o) => s.isSectionExpendedProxyVolume,
            FoldoutOption.Indent,
            CED.Action(Drawer_SectionProxySettings)
            );
        
        public static readonly CED.IDrawer SectionInfluenceVolume = CED.Select(
            (s, d, o) => s.influenceVolume,
            (s, d, o) => d.influenceVolume,
            InfluenceVolumeUI.SectionFoldoutShape
            );

        public static readonly CED.IDrawer SectionShapeCheck = CED.Action(Drawer_DifferentShapeError);

        public static readonly CED.IDrawer SectionCaptureSettings = CED.Select(
            (s, d, o) => s.captureSettings,
            (s, d, o) => d.captureSettings,
            CaptureSettingsUI.SectionCaptureSettings
            );

        public static readonly CED.IDrawer SectionFrameSettings = CED.FadeGroup(
            (s, d, o, i) => s.isFrameSettingsOverriden,
            FadeOption.None,
            CED.Select(
                (s, d, o) => s.frameSettings,
                (s, d, o) => d.frameSettings,
                FrameSettingsUI.Inspector(withOverride: true, withXR: false))
            );

        public static readonly CED.IDrawer SectionFoldoutAdditionalSettings = CED.FoldoutGroup(
            additionnalSettingsHeader,
            (s, d, o) => s.isSectionExpendedAdditionalSettings,
            FoldoutOption.Indent,
            CED.Action(Drawer_SectionCustomSettings),
            CED.space
            );

        static HDProbeUI()
        {
            Inspector = new[]
            {
                SectionToolbar,
                SectionPrimarySettings,
                SectionProxyVolumeSettings,
                SectionInfluenceVolume,
                SectionShapeCheck,
                SectionCaptureSettings,
                SectionFoldoutAdditionalSettings,
                SectionFrameSettings,
                SectionBakeButton
            };
        }

        protected static void Drawer_DifferentShapeError(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            var proxy = d.proxyVolumeReference.objectReferenceValue as ReflectionProxyVolumeComponent;
            if (proxy != null
                && (int)proxy.proxyVolume.shape != d.influenceVolume.shape.enumValueIndex
                && proxy.proxyVolume.shape != ProxyShape.Infinite)
            {
                EditorGUILayout.HelpBox(
                    proxyInfluenceShapeMismatchHelpBoxText,
                    MessageType.Error,
                    true
                    );
            }
        }
        
        static GUIStyle disabled;
        static void PropertyField(SerializedProperty prop, GUIContent content)
        {
            if(prop != null)
            {
                EditorGUILayout.PropertyField(prop, content);
            }
            else
            {
                if(disabled == null)
                {
                    disabled = new GUIStyle(GUI.skin.label);
                    disabled.onNormal.textColor *= 0.5f;
                }
                EditorGUILayout.LabelField(content, disabled);
            }
        }

        protected static void Drawer_SectionBakeButton(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            if (d.target is HDAdditionalReflectionData)
                EditorReflectionSystemGUI.DrawBakeButton((ReflectionProbeMode)d.mode.intValue, ((HDAdditionalReflectionData)d.target).reflectionProbe);
            else //PlanarReflectionProbe
                EditorReflectionSystemGUI.DrawBakeButton((ReflectionProbeMode)d.mode.intValue, d.target as PlanarReflectionProbe);
        }

        protected static void Drawer_SectionProbeModeRealtimeSettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(d.refreshMode, _.GetContent("Refresh Mode|Only EveryFrame supported at the moment"));
            GUI.enabled = true;
            EditorGUILayout.Space();
        }

        protected static void Drawer_SectionProxySettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.proxyVolumeReference, proxyVolumeContent);
            
            if (d.target.proxyVolume == null)
            {
                EditorGUI.BeginChangeCheck();
                d.infiniteProjection.boolValue = !EditorGUILayout.Toggle(useInfiniteProjectionContent, !d.infiniteProjection.boolValue);
                if(EditorGUI.EndChangeCheck())
                {
                    d.Apply();
                }
            }

            if (d.proxyVolumeReference.objectReferenceValue != null)
            {
                var proxy = (ReflectionProxyVolumeComponent)d.proxyVolumeReference.objectReferenceValue;
                if ((int)proxy.proxyVolume.shape != d.influenceVolume.shape.enumValueIndex
                    && proxy.proxyVolume.shape != ProxyShape.Infinite)
                    EditorGUILayout.HelpBox(
                        proxyInfluenceShapeMismatchHelpBoxText,
                        MessageType.Error,
                        true
                        );
            }
            else
            {
                EditorGUILayout.HelpBox(
                        d.infiniteProjection.boolValue ? noProxyInfiniteHelpBoxText : noProxyHelpBoxText,
                        MessageType.Info,
                        true
                        );
            }
        }

        protected static void Drawer_SectionCustomSettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            using (new EditorGUI.DisabledScope(!hdPipeline.asset.renderPipelineSettings.supportLightLayers))
            {
                d.lightLayers.intValue = Convert.ToInt32(EditorGUILayout.EnumFlagsField(lightLayersContent, (LightLayerEnum)d.lightLayers.intValue));
            }

            EditorGUILayout.PropertyField(d.weight, weightContent);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(d.multiplier, multiplierContent);
            if (EditorGUI.EndChangeCheck())
                d.multiplier.floatValue = Mathf.Max(0.0f, d.multiplier.floatValue);
        }

        static readonly GUIContent[] k_ModeContents = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
        static readonly int[] k_ModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Custom, (int)ReflectionProbeMode.Realtime };
        protected static void Drawer_ReflectionProbeMode(HDProbeUI s, SerializedHDProbe p, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.mode.hasMultipleDifferentValues;
            EditorGUILayout.IntPopup(p.mode, k_ModeContents, k_ModeValues, CoreEditorUtils.GetContent("Type|'Baked Cubemap' uses the 'Auto Baking' mode from the Lighting window. If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n'Custom' can be used if a custom cubemap is wanted. \n'Realtime' can be used to dynamically re-render the cubemap during runtime (via scripting)."));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                s.SetModeTarget(p.mode.intValue);
                p.Apply();
            }
        }
        
        protected static void Drawer_ModeSettingsCustom(HDProbeUI s, SerializedHDProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.renderDynamicObjects, CoreEditorUtils.GetContent("Dynamic Objects|If enabled dynamic objects are also rendered into the cubemap"));

            EditorGUI.showMixedValue = p.customBakedTexture.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var customTexture = EditorGUILayout.ObjectField(CoreEditorUtils.GetContent("Cubemap"), p.customBakedTexture.objectReferenceValue, typeof(Cubemap), false);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                p.customBakedTexture.objectReferenceValue = customTexture;
        }
    }
}
