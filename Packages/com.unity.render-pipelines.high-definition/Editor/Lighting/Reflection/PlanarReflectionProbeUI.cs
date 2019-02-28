namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using UnityEngine;
    using UnityEngine.Experimental.Rendering.HDPipeline;
    using UnityEngine.Rendering;
    using CED = CoreEditorDrawer<HDProbeUI, SerializedHDProbe>;

    partial class PlanarReflectionProbeUI : HDProbeUI
    {
#pragma warning disable 618
        new public static readonly CED.IDrawer[] Inspector;
#pragma warning restore 618

        //temporary to lock UI on realtime until other mode than realtime are usable
        new static void Drawer_ReflectionProbeMode(HDProbeUI s, SerializedHDProbe p, Editor owner)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                HDProbeUI.Drawer_ReflectionProbeMode(s, p, owner);
            }
        }

        //temporary to lock UI on realtime until other mode than realtime are usable
#pragma warning disable 618 //CED
        static readonly CED.IDrawer SectionPrimarySettings = CED.Group(
#pragma warning restore 618
            CED.Action(Drawer_ReflectionProbeMode),
            CED.FadeGroup((s, p, o, i) => s.IsSectionExpandedReflectionProbeMode((ReflectionProbeMode)i),
#pragma warning disable 618
                FadeOption.Indent,
#pragma warning restore 618
                CED.space,                                              // Baked
                CED.Action(Drawer_SectionProbeModeRealtimeSettings),    // Realtime
                CED.Action(Drawer_ModeSettingsCustom)                   // Custom
                )
            );

        static PlanarReflectionProbeUI()
        {
            //copy HDProbe UI
            int max = HDProbeUI.Inspector.Length;
#pragma warning disable 618 //CED
            Inspector = new CED.IDrawer[max];
#pragma warning disable 618
            for (int i = 0; i < max; ++i)
            {
                Inspector[i] = HDProbeUI.Inspector[i];
            }

            //forbid other mode than realtime at the moment
            Inspector[1] = SectionPrimarySettings;      //lock realtime/Custom/bake on realtime
            Inspector[Inspector.Length - 1] = CED.noop; //hide bake button

            //override SectionInfluenceVolume to remove normals settings
            Inspector[3] = CED.FoldoutGroup(
                InfluenceVolumeUI.influenceVolumeHeader,
                Expandable.InfluenceVolume,
                k_ExpandedState,
                CED.Select(
                    (s, d, o) => s.influenceVolume,
                    (s, d, o) => d.influenceVolume,
                    InfluenceVolumeUI.InnerInspector(UnityEngine.Experimental.Rendering.HDPipeline.ReflectionProbeType.PlanarReflection)
                    )
                );
        }

        internal PlanarReflectionProbeUI()
        {
            //remove normal edition tool and capture point for planar
            toolBars = new[] { ToolBar.InfluenceShape | ToolBar.Blend, ToolBar.CapturePosition }; 
        }
    }
}
