using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;

    public static class EditorReflectionSystemGUI
    {
        static readonly string[] k_BakeCustomOptionText = { "Bake as new Cubemap..." };
        static readonly string[] k_BakeButtonsText = { "Bake All Reflection Probes" };

        public static void DrawBakeButton(ReflectionProbeMode reflectionProbeMode, ReflectionProbe probe)
        {
            DrawBakeButton(reflectionProbeMode, probe, null);
        }

        public static void DrawBakeButton(ReflectionProbeMode reflectionProbeMode, PlanarReflectionProbe probe)
        {
            DrawBakeButton(reflectionProbeMode, null, probe);
        }

        static void DrawBakeButton(ReflectionProbeMode reflectionProbeMode, ReflectionProbe probe, PlanarReflectionProbe planarProbe)
        {
            if (reflectionProbeMode == ReflectionProbeMode.Realtime)
            {
                EditorGUILayout.HelpBox("Refresh of this reflection probe should be initiated from the scripting API because the type is 'Realtime'", MessageType.Info);

                if (!QualitySettings.realtimeReflectionProbes)
                    EditorGUILayout.HelpBox("Realtime reflection probes are disabled in Quality Settings", MessageType.Warning);
                return;
            }

            if (reflectionProbeMode == ReflectionProbeMode.Baked
                && UnityEditor.Lightmapping.giWorkflowMode != UnityEditor.Lightmapping.GIWorkflowMode.OnDemand)
            {
                EditorGUILayout.HelpBox("Baking of this reflection probe is automatic because this probe's type is 'Baked' and the Lighting window is using 'Auto Baking'. The cubemap created is stored in the GI cache.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            switch (reflectionProbeMode)
            {
                case ReflectionProbeMode.Custom:
                {
                    if (ButtonWithDropdownList(
                            _.GetContent("Bake|Bakes Reflection Probe's cubemap, overwriting the existing cubemap texture asset (if any)."), k_BakeCustomOptionText,
                            data =>
                        {
                            var mode = (int)data;

                            if (mode == 0)
                            {
                                if (probe != null)
                                {
                                    EditorReflectionSystem.BakeCustomReflectionProbe(probe, false);
                                }
                                if (planarProbe != null)
                                {
                                    EditorReflectionSystem.BakeCustomReflectionProbe(planarProbe, false);
                                }
                            }
                        },
                            GUILayout.ExpandWidth(true)))
                    {
                        if (probe != null)
                        {
                            EditorReflectionSystem.BakeCustomReflectionProbe(probe, true);
                        }
                        if (planarProbe != null)
                        {
                            EditorReflectionSystem.BakeCustomReflectionProbe(planarProbe, true);
                        }
                        GUIUtility.ExitGUI();
                    }
                    break;
                }

                case ReflectionProbeMode.Baked:
                {
                    GUI.enabled = probe != null && probe.enabled
                        || planarProbe != null && planarProbe.enabled;

                    // Bake button in non-continous mode
                    if (ButtonWithDropdownList(
                            _.GetContent("Bake"),
                            k_BakeButtonsText,
                            data =>
                        {
                            var mode = (int)data;
                            if (mode == 0)
                                EditorReflectionSystem.BakeAllReflectionProbesSnapshots();
                        },
                            GUILayout.ExpandWidth(true)))
                    {
                        if (probe != null)
                        {
                            EditorReflectionSystem.BakeReflectionProbeSnapshot(probe);
                        }
                        if (planarProbe != null)
                        {
                            EditorReflectionSystem.BakeReflectionProbeSnapshot(planarProbe);
                        }
                        GUIUtility.ExitGUI();
                    }

                    GUI.enabled = true;
                    break;
                }

                case ReflectionProbeMode.Realtime:

                    // Not showing bake button in realtime
                    break;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        static MethodInfo k_EditorGUI_ButtonWithDropdownList = typeof(EditorGUI).GetMethod("ButtonWithDropdownList", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, new[] { typeof(GUIContent), typeof(string[]), typeof(GenericMenu.MenuFunction2), typeof(GUILayoutOption[]) }, new ParameterModifier[0]);

        static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            return (bool)k_EditorGUI_ButtonWithDropdownList.Invoke(null, new object[] { content, buttonNames, callback, options });
        }
    }
}
