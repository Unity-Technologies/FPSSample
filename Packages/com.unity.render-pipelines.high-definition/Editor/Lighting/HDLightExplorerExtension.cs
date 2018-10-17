using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [LightingExplorerExtensionAttribute(typeof(HDRenderPipelineAsset))]
    public class HDLightExplorerExtension : DefaultLightingExplorerExtension
    {
        struct LightData
        {
            public HDAdditionalLightData hdAdditionalLightData;
            public AdditionalShadowData additionalShadowData;
            public bool isPrefab;
            public Object prefabRoot;
            
            public LightData(HDAdditionalLightData hdAdditionalLightData, AdditionalShadowData additionalShadowData, bool isPrefab, Object prefabRoot)
            {
                this.hdAdditionalLightData = hdAdditionalLightData;
                this.additionalShadowData = additionalShadowData;
                this.isPrefab = isPrefab;
                this.prefabRoot = prefabRoot;
            }
        }

        struct VolumeData
        {
            public bool isGlobal;
            public bool hasBakingSky;
            public bool hasVisualEnvironment;
            public VolumeProfile profile;
            public FogType fogType;
            public SkyType skyType;
            
            public VolumeData(bool isGlobal, VolumeProfile profile, bool hasBakingSky)
            {
                this.isGlobal = isGlobal;
                this.profile = profile;
                VisualEnvironment visualEnvironment = null;
                this.hasVisualEnvironment = profile.TryGet<VisualEnvironment>(typeof(VisualEnvironment), out visualEnvironment);
                if (this.hasVisualEnvironment)
                {
                    this.skyType = (SkyType)visualEnvironment.skyType.value;
                    this.fogType = visualEnvironment.fogType.value;
                }
                else
                {
                    this.skyType = (SkyType)1;
                    this.fogType = (FogType)0;
                }
                this.hasBakingSky = hasBakingSky;
            }
        }

        static Dictionary<Light, LightData> lightDataPairing = new Dictionary<Light, LightData>();

        static Dictionary<Volume, VolumeData> volumeDataPairing = new Dictionary<Volume, VolumeData>();

        static Dictionary<ReflectionProbe, HDAdditionalReflectionData> reflectionProbeDataPairing = new Dictionary<ReflectionProbe, HDAdditionalReflectionData>();

        static Dictionary<PlanarReflectionProbe, HDAdditionalReflectionData> planarReflectionDataPairing = new Dictionary<PlanarReflectionProbe, HDAdditionalReflectionData>();

        protected static class HDStyles
        {
            public static readonly GUIContent Name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent On = EditorGUIUtility.TrTextContent("On");
            public static readonly GUIContent Type = EditorGUIUtility.TrTextContent("Type");
            public static readonly GUIContent Mode = EditorGUIUtility.TrTextContent("Mode");
            public static readonly GUIContent Range = EditorGUIUtility.TrTextContent("Range");
            public static readonly GUIContent Color = EditorGUIUtility.TrTextContent("Color");
            public static readonly GUIContent ColorFilter = EditorGUIUtility.TrTextContent("Color Filter");
            public static readonly GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity");
            public static readonly GUIContent IndirectMultiplier = EditorGUIUtility.TrTextContent("Indirect Multiplier");
            public static readonly GUIContent Unit = EditorGUIUtility.TrTextContent("Unit");
            public static readonly GUIContent ColorTemperature = EditorGUIUtility.TrTextContent("Color Temperature");
            public static readonly GUIContent Shadows = EditorGUIUtility.TrTextContent("Shadows");
            public static readonly GUIContent ContactShadows = EditorGUIUtility.TrTextContent("Contact Shadows");
            public static readonly GUIContent ShadowResolution = EditorGUIUtility.TrTextContent("Shadows Resolution");
            public static readonly GUIContent ShapeWidth = EditorGUIUtility.TrTextContent("Shape Width");
            public static readonly GUIContent VolumeProfile = EditorGUIUtility.TrTextContent("Volume Profile");
            public static readonly GUIContent ColorTemperatureMode = EditorGUIUtility.TrTextContent("Use Color Temperature");
            public static readonly GUIContent AffectDiffuse = EditorGUIUtility.TrTextContent("Affect Diffuse");
            public static readonly GUIContent AffectSpecular = EditorGUIUtility.TrTextContent("Affect Specular");
            public static readonly GUIContent IsPrefab = EditorGUIUtility.TrTextContent("Prefab");

            public static readonly GUIContent GlobalVolume = EditorGUIUtility.TrTextContent("Is Global");
            public static readonly GUIContent Priority = EditorGUIUtility.TrTextContent("Priority");
            public static readonly GUIContent HasVisualEnvironment = EditorGUIUtility.TrTextContent("Has Visual Environment");
            public static readonly GUIContent FogType = EditorGUIUtility.TrTextContent("Fog Type");
            public static readonly GUIContent SkyType = EditorGUIUtility.TrTextContent("Sky Type");
            public static readonly GUIContent HasBakingSky = EditorGUIUtility.TrTextContent("Has Baking Sky");

            public static readonly GUIContent ReflectionProbeMode = EditorGUIUtility.TrTextContent("Mode");
            public static readonly GUIContent ReflectionProbeShape = EditorGUIUtility.TrTextContent("Shape");
            public static readonly GUIContent ReflectionProbeShadowDistance = EditorGUIUtility.TrTextContent("Shadow Distance");
            public static readonly GUIContent ReflectionProbeNearClip = EditorGUIUtility.TrTextContent("Near Clip");
            public static readonly GUIContent ReflectionProbeFarClip = EditorGUIUtility.TrTextContent("Far Clip");
            public static readonly GUIContent ParallaxCorrection = EditorGUIUtility.TrTextContent("Parallax Correction");
            public static readonly GUIContent ReflectionProbeWeight = EditorGUIUtility.TrTextContent("Weight");

            public static readonly GUIContent[] LightmapBakeTypeTitles = { EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Mixed"), EditorGUIUtility.TrTextContent("Baked") };
            public static readonly int[] LightmapBakeTypeValues = { (int)LightmapBakeType.Realtime, (int)LightmapBakeType.Mixed, (int)LightmapBakeType.Baked };
        }
        
        public override LightingExplorerTab[] GetContentTabs()
        {
            return new[]
            {
                new LightingExplorerTab("Lights", GetHDLights, GetHDLightColumns),
                new LightingExplorerTab("Volumes", GetVolumes, GetVolumeColumns),
                new LightingExplorerTab("Reflection Probes", GetHDReflectionProbes, GetHDReflectionProbeColumns),
                new LightingExplorerTab("Planar Reflection Probes", GetPlanarReflections, GetPlanarReflectionColumns),
                new LightingExplorerTab("LightProbes", GetLightProbes, GetLightProbeColumns),
                new LightingExplorerTab("Emissive Materials", GetEmissives, GetEmissivesColumns)
            };
        }

        protected virtual UnityEngine.Object[] GetHDLights()
        {
            var lights = UnityEngine.Object.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(light) != null) // We have a prefab
                {
                    lightDataPairing[light] = new LightData(light.GetComponent<HDAdditionalLightData>(), light.GetComponent<AdditionalShadowData>(),
                                                            true, PrefabUtility.GetCorrespondingObjectFromSource(PrefabUtility.GetOutermostPrefabInstanceRoot(light.gameObject)));
                }
                else
                {
                    lightDataPairing[light] = new LightData(light.GetComponent<HDAdditionalLightData>(), light.GetComponent<AdditionalShadowData>(), false, null);
                }                
            }
            return lights;
        }

        protected virtual UnityEngine.Object[] GetHDReflectionProbes()
        {
            var reflectionProbes = Object.FindObjectsOfType<ReflectionProbe>();
            {
                foreach (ReflectionProbe probe in reflectionProbes )
                {
                    reflectionProbeDataPairing[probe] = probe.GetComponent<HDAdditionalReflectionData>();
                }
            }
            return reflectionProbes;
        }

        protected virtual UnityEngine.Object[] GetPlanarReflections()
        {
            return UnityEngine.Object.FindObjectsOfType<PlanarReflectionProbe>();
        }

        protected virtual UnityEngine.Object[] GetVolumes()
        {
            var volumes = UnityEngine.Object.FindObjectsOfType<Volume>();
            foreach (var volume in volumes)
            {
                bool hasBakingSky = volume.GetComponent<BakingSky>();
                volumeDataPairing[volume] = new VolumeData(volume.isGlobal, volume.profile,hasBakingSky);
            }
            return volumes;
        }
        
        protected virtual LightingExplorerTableColumn[] GetHDLightColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, HDStyles.Name, null, 200),                                               // 0: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.On, "m_Enabled", 25),                                       // 1: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Type, "m_Type", 60),                                            // 2: Type
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Mode, "m_Lightmapping", 60),                                    // 3: Mixed mode
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.Range, "m_Range", 60),                                           // 4: Range
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.ColorTemperatureMode, "m_UseColorTemperature", 100),         // 5: Color Temperature Mode
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Color, HDStyles.Color, "m_Color", 60),                                         // 6: Color
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ColorTemperature, "m_ColorTemperature", 100,(r, prop, dep) =>  // 7: Color Temperature
                {
                    if (prop.serializedObject.FindProperty("m_UseColorTemperature").boolValue)
                    {
                        prop = prop.serializedObject.FindProperty("m_ColorTemperature");
                        prop.floatValue = EditorGUI.FloatField(r,prop.floatValue);
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Intensity, "m_Intensity", 60, (r, prop, dep) =>                // 8: Intensity
                {
                    Light light = prop.serializedObject.targetObject as Light;
                    float intensity = lightDataPairing[light].hdAdditionalLightData.intensity;
                    EditorGUI.BeginChangeCheck();
                    intensity = EditorGUI.FloatField(r, intensity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(lightDataPairing[light].hdAdditionalLightData, "Changed light intensity");
                        lightDataPairing[light].hdAdditionalLightData.intensity = intensity;
                    }
                }), 
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Unit, "m_Intensity", 60, (r, prop, dep) =>                // 9: Unit
                {
                    Light light = prop.serializedObject.targetObject as Light;
                    LightUnit unit = lightDataPairing[light].hdAdditionalLightData.lightUnit;
                    EditorGUI.BeginChangeCheck();
                    unit = (LightUnit)EditorGUI.EnumPopup(r, unit);
                    if (EditorGUI.EndChangeCheck())
                    {
                        lightDataPairing[light].hdAdditionalLightData.lightUnit = unit;
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.IndirectMultiplier, "m_BounceIntensity", 90),                  // 10: Indirect multiplier
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.Shadows, "m_Shadows.m_Type", 60, (r, prop, dep) =>          // 11: Shadows
                {
                    EditorGUI.BeginChangeCheck();
                    bool shadows = EditorGUI.Toggle(r, prop.intValue != (int)LightShadows.None);
                    if (EditorGUI.EndChangeCheck())
                    {
                        prop.intValue = shadows ? (int)LightShadows.Soft : (int)LightShadows.None;
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.ContactShadows, "m_Shadows.m_Type", 100, (r, prop, dep) =>  // 12: Contact Shadows
                {
                    Light light = prop.serializedObject.targetObject as Light;
                    bool contactShadows = lightDataPairing[light].additionalShadowData.contactShadows;
                    EditorGUI.BeginChangeCheck();
                    contactShadows = EditorGUI.Toggle(r, contactShadows);
                    if (EditorGUI.EndChangeCheck())
                    {
                        lightDataPairing[light].additionalShadowData.contactShadows = contactShadows;
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, HDStyles.ShadowResolution, "m_Intensity", 60, (r, prop, dep) =>           // 13: Shadow resolution
                {
                    Light light = prop.serializedObject.targetObject as Light;
                    int shadowResolution = lightDataPairing[light].additionalShadowData.shadowResolution;
                    EditorGUI.BeginChangeCheck();
                    shadowResolution = EditorGUI.IntField(r, shadowResolution);
                    if (EditorGUI.EndChangeCheck())
                    {
                        lightDataPairing[light].additionalShadowData.shadowResolution = shadowResolution;
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.AffectDiffuse, "m_Intensity", 90, (r, prop, dep) =>         // 14: Affect Diffuse
                {
                    Light light = prop.serializedObject.targetObject as Light;
                    bool affectDiffuse = lightDataPairing[light].hdAdditionalLightData.affectDiffuse;
                    EditorGUI.BeginChangeCheck();
                    affectDiffuse = EditorGUI.Toggle(r, affectDiffuse);
                    if (EditorGUI.EndChangeCheck())
                    {
                        lightDataPairing[light].hdAdditionalLightData.affectDiffuse = affectDiffuse;
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.AffectSpecular, "m_Intensity", 90, (r, prop, dep) =>        // 15: Affect Specular
                {
                    Light light = prop.serializedObject.targetObject as Light;
                    bool affectSpecular = lightDataPairing[light].hdAdditionalLightData.affectSpecular;
                    EditorGUI.BeginChangeCheck();
                    affectSpecular = EditorGUI.Toggle(r, affectSpecular);
                    if (EditorGUI.EndChangeCheck())
                    {
                        lightDataPairing[light].hdAdditionalLightData.affectSpecular = affectSpecular;
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, HDStyles.IsPrefab, "m_Intensity", 60, (r, prop, dep) =>                // 16: Prefab
                {
                    Light light = prop.serializedObject.targetObject as Light;
                    bool isPrefab = lightDataPairing[light].isPrefab;
                    if (isPrefab)
                    {
                        EditorGUI.ObjectField(r, lightDataPairing[light].prefabRoot, typeof(GameObject),false);
                    }
                }),

            };
        }

        protected virtual LightingExplorerTableColumn[] GetVolumeColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, HDStyles.Name, null, 200),                                               // 0: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.On, "m_Enabled", 25),                                       // 1: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.GlobalVolume, "isGlobal", 60),                              // 2: Is Global
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.Priority, "priority", 60),                                     // 3: Priority
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, HDStyles.VolumeProfile, "sharedProfile", 100, (r, prop, dep) =>        // 4: Profile
                {
                    if (prop.objectReferenceValue != null )
                        EditorGUI.PropertyField(r, prop, GUIContent.none);
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.HasVisualEnvironment, "sharedProfile", 100, (r, prop, dep) =>// 5: Has Visual environment
                {
                    Volume volume = prop.serializedObject.targetObject as Volume;
                    bool hasVisualEnvironment = volumeDataPairing[volume].hasVisualEnvironment;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.Toggle(r, hasVisualEnvironment);
                    EditorGUI.EndDisabledGroup();
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, HDStyles.SkyType, "sharedProfile", 100, (r, prop, dep) =>              // 6: Sky type
                {
                    Volume volume = prop.serializedObject.targetObject as Volume;
                    if (volumeDataPairing[volume].hasVisualEnvironment)
                    {
                        SkyType skyType = volumeDataPairing[volume].skyType;
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUI.EnumPopup(r, skyType);
                        EditorGUI.EndDisabledGroup();
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, HDStyles.FogType, "sharedProfile", 100, (r, prop, dep) =>              // 7: Fog type
                {
                    Volume volume = prop.serializedObject.targetObject as Volume;
                    if (volumeDataPairing[volume].hasVisualEnvironment)
                    {
                        FogType fogType = volumeDataPairing[volume].fogType;
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUI.EnumPopup(r, fogType);
                        EditorGUI.EndDisabledGroup();
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.HasBakingSky, "sharedProfile", 100, (r, prop, dep) =>       // 8: Has Baking Sky
                {
                    Volume volume = prop.serializedObject.targetObject as Volume;
                    bool hasBakingSky = volumeDataPairing[volume].hasBakingSky;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.Toggle(r, hasBakingSky);
                    EditorGUI.EndDisabledGroup();
                }), 
            };
        }

        protected virtual LightingExplorerTableColumn[] GetHDReflectionProbeColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, HDStyles.Name, null, 200),                                               // 0: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.On, "m_Enabled", 25),                                       // 1: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.ReflectionProbeMode, "m_Mode", 60, (r, prop, dep) =>            // 2: Mode
                {
                    prop.intValue = (int)(UnityEngine.Rendering.ReflectionProbeMode)EditorGUI.EnumPopup(r,(UnityEngine.Rendering.ReflectionProbeMode)prop.intValue);
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, HDStyles.ReflectionProbeShape, "m_Mode", 60, (r, prop, dep) =>           // 3: Shape
                {
                    ReflectionProbe probe = prop.serializedObject.targetObject as ReflectionProbe;
                    InfluenceShape influenceShape = reflectionProbeDataPairing[probe].influenceVolume.shape;
                    EditorGUI.BeginChangeCheck();
                    influenceShape = (InfluenceShape)EditorGUI.EnumPopup(r, influenceShape);
                    if (EditorGUI.EndChangeCheck())
                    {
                        reflectionProbeDataPairing[probe].influenceVolume.shape = influenceShape;
                    }
                }),
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ReflectionProbeShadowDistance, "m_ShadowDistance", 90),        // 4: Shadow distance
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ReflectionProbeNearClip, "m_NearClip", 60),                    // 5: Near clip
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ReflectionProbeFarClip, "m_FarClip", 60),                      // 6: Far clip
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.ParallaxCorrection, "m_BoxProjection", 90),                 // 7: Parallax correction
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ReflectionProbeWeight, "m_Mode", 60, (r, prop, dep) =>         // 8: Weight
                {
                    ReflectionProbe probe = prop.serializedObject.targetObject as ReflectionProbe;
                    float weight = reflectionProbeDataPairing[probe].weight;
                    EditorGUI.BeginChangeCheck();
                    weight = EditorGUI.FloatField(r, weight);
                    if (EditorGUI.EndChangeCheck())
                    {
                        reflectionProbeDataPairing[probe].weight = weight;
                    }
                }),
            };
        }

        protected virtual LightingExplorerTableColumn[] GetPlanarReflectionColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, HDStyles.Name, null, 200),                                               // 0: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, HDStyles.On, "m_Enabled", 25),                                       // 1: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, HDStyles.ReflectionProbeWeight, "m_Weight", 50),                        // 2: Weight
            };
        }

        public override void OnDisable()
        {
            lightDataPairing.Clear();
            volumeDataPairing.Clear();
            reflectionProbeDataPairing.Clear();
            planarReflectionDataPairing.Clear();
        }
    }
}
