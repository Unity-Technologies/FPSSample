using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum FullScreenDebugMode
    {
        None,

        // Lighting
        MinLightingFullScreenDebug,
        SSAO,
        ScreenSpaceReflections,
        ContactShadows,
        PreRefractionColorPyramid,
        DepthPyramid,
        FinalColorPyramid,
        MaxLightingFullScreenDebug,

        // Rendering
        MinRenderingFullScreenDebug,
        MotionVectors,
        NanTracker,
        MaxRenderingFullScreenDebug
    }

    public class DebugDisplaySettings
    {
        public static string k_PanelDisplayStats = "Display Stats";
        public static string k_PanelMaterials = "Material";
        public static string k_PanelLighting = "Lighting";
        public static string k_PanelRendering = "Rendering";
        public static string k_PanelDecals = "Decals";

        DebugUI.Widget[] m_DebugDisplayStatsItems;
        DebugUI.Widget[] m_DebugMaterialItems;
        DebugUI.Widget[] m_DebugLightingItems;
        DebugUI.Widget[] m_DebugRenderingItems;
        DebugUI.Widget[] m_DebugDecalsItems;


        public float debugOverlayRatio = 0.33f;
        public FullScreenDebugMode  fullScreenDebugMode = FullScreenDebugMode.None;
        public float fullscreenDebugMip = 0.0f;
        public bool showSSSampledColor = false;

        public MaterialDebugSettings materialDebugSettings = new MaterialDebugSettings();
        public LightingDebugSettings lightingDebugSettings = new LightingDebugSettings();
        public MipMapDebugSettings mipMapDebugSettings = new MipMapDebugSettings();
        public ColorPickerDebugSettings colorPickerDebugSettings = new ColorPickerDebugSettings();
        public FalseColorDebugSettings falseColorDebugSettings = new FalseColorDebugSettings();
        public DecalsDebugSettings decalsDebugSettings = new DecalsDebugSettings();
        public MSAASamples msaaSamples = MSAASamples.None;

        public static GUIContent[] lightingFullScreenDebugStrings = null;
        public static int[] lightingFullScreenDebugValues = null;
        public static GUIContent[] renderingFullScreenDebugStrings = null;
        public static int[] renderingFullScreenDebugValues = null;
        public static GUIContent[] msaaSamplesDebugStrings = null;
        public static int[] msaaSamplesDebugValues = null;

        public static List<GUIContent> cameraNames = new List<GUIContent>();
        public static GUIContent[] cameraNamesStrings = null;
        public static int[] cameraNamesValues = null;
        public int debugCameraToFreeze = 0;

        static bool needsRefreshingCameraFreezeList = true;

        //saved enum fields for when repainting
        int m_LightingDebugModeEnumIndex;
        int m_LightingFulscreenDebugModeEnumIndex;
        int m_TileClusterDebugEnumIndex;
        int m_MipMapsEnumIndex;
        int m_MaterialEnumIndex;
        int m_EngineEnumIndex;
        int m_AttributesEnumIndex;
        int m_PropertiesEnumIndex;
        int m_GBufferEnumIndex;
        int m_ShadowDebugModeEnumIndex;
        int m_TileClusterDebugByCategoryEnumIndex;
        int m_LightVolumeDebugTypeEnumIndex;
        int m_RenderingFulscreenDebugModeEnumIndex;
        int m_TerrainTextureEnumIndex;
        int m_ColorPickerDebugModeEnumIndex;
        int m_MsaaSampleDebugModeEnumIndex;
        int m_DebugCameraToFreezeIndex;


        public DebugDisplaySettings()
        {
            FillFullScreenDebugEnum(ref lightingFullScreenDebugStrings, ref lightingFullScreenDebugValues, FullScreenDebugMode.MinLightingFullScreenDebug, FullScreenDebugMode.MaxLightingFullScreenDebug);
            FillFullScreenDebugEnum(ref renderingFullScreenDebugStrings, ref renderingFullScreenDebugValues, FullScreenDebugMode.MinRenderingFullScreenDebug, FullScreenDebugMode.MaxRenderingFullScreenDebug);

            msaaSamplesDebugStrings = Enum.GetNames(typeof(MSAASamples))
                .Select(t => new GUIContent(t))
                .ToArray();
            msaaSamplesDebugValues = (int[])Enum.GetValues(typeof(MSAASamples));
        }

        public int GetDebugMaterialIndex()
        {
            return materialDebugSettings.GetDebugMaterialIndex();
        }

        public DebugLightingMode GetDebugLightingMode()
        {
            return lightingDebugSettings.debugLightingMode;
        }

        public ShadowMapDebugMode GetDebugShadowMapMode()
        {
            return lightingDebugSettings.shadowDebugMode;
        }

        public DebugMipMapMode GetDebugMipMapMode()
        {
            return mipMapDebugSettings.debugMipMapMode;
        }

        public DebugMipMapModeTerrainTexture GetDebugMipMapModeTerrainTexture()
        {
            return mipMapDebugSettings.terrainTexture;
        }

        public ColorPickerDebugMode GetDebugColorPickerMode()
        {
            return colorPickerDebugSettings.colorPickerMode;
        }

        public bool IsCameraFreezeEnabled()
        {
            return debugCameraToFreeze != 0;
        }
        public string GetFrozenCameraName()
        {
            return cameraNamesStrings[debugCameraToFreeze].text;
        }

        public bool IsDebugDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled() || lightingDebugSettings.IsDebugDisplayEnabled() || mipMapDebugSettings.IsDebugDisplayEnabled() || IsDebugFullScreenEnabled();
        }

        public bool IsDebugDisplayRemovePostprocess()
        {
            // We want to keep post process when only the override more are enabled and none of the other
            return materialDebugSettings.IsDebugDisplayEnabled() || lightingDebugSettings.IsDebugDisplayRemovePostprocess() || mipMapDebugSettings.IsDebugDisplayEnabled() || IsDebugFullScreenEnabled();
        }

        public bool IsDebugMaterialDisplayEnabled()
        {
            return materialDebugSettings.IsDebugDisplayEnabled();
        }

        public bool IsDebugFullScreenEnabled()
        {
            return fullScreenDebugMode != FullScreenDebugMode.None;
        }

        public bool IsDebugMipMapDisplayEnabled()
        {
            return mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        private void DisableNonMaterialDebugSettings()
        {
            lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
        }

        public void SetDebugViewMaterial(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewMaterial(value);
        }

        public void SetDebugViewEngine(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewEngine(value);
        }

        public void SetDebugViewVarying(DebugViewVarying value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewVarying(value);
        }

        public void SetDebugViewProperties(DebugViewProperties value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewProperties(value);
        }

        public void SetDebugViewGBuffer(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            materialDebugSettings.SetDebugViewGBuffer(value);
        }

        public void SetFullScreenDebugMode(FullScreenDebugMode value)
        {
            if (lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
                value = 0;
            
            fullScreenDebugMode = value;
        }

        public void SetShadowDebugMode(ShadowMapDebugMode value)
        {
            // When SingleShadow is enabled, we don't render full screen debug modes
            if (value == ShadowMapDebugMode.SingleShadow)
                fullScreenDebugMode = 0;
            lightingDebugSettings.shadowDebugMode = value;
        }

        public void SetDebugLightingMode(DebugLightingMode value)
        {
            if (value != 0)
            {
                materialDebugSettings.DisableMaterialDebug();
                mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
            }
            lightingDebugSettings.debugLightingMode = value;
        }

        public void SetMipMapMode(DebugMipMapMode value)
        {
            if (value != 0)
            {
                materialDebugSettings.DisableMaterialDebug();
                lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            }
            mipMapDebugSettings.debugMipMapMode = value;
        }

        public void UpdateMaterials()
        {
            if (mipMapDebugSettings.debugMipMapMode != 0)
                Texture.SetStreamingTextureMaterialDebugProperties();
        }

        public void UpdateCameraFreezeOptions()
        {
            if(needsRefreshingCameraFreezeList)
            {
                cameraNames.Insert(0, new GUIContent("None"));

                cameraNamesStrings = cameraNames.ToArray();
                cameraNamesValues = Enumerable.Range(0, cameraNames.Count()).ToArray();

                UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
                RegisterRenderingDebug();
                needsRefreshingCameraFreezeList = false;
            }
        }

        public bool DebugNeedsExposure()
        {
            DebugLightingMode debugLighting = lightingDebugSettings.debugLightingMode;
            DebugViewGbuffer debugGBuffer = (DebugViewGbuffer)materialDebugSettings.debugViewGBuffer;
            return (debugLighting == DebugLightingMode.DiffuseLighting || debugLighting == DebugLightingMode.SpecularLighting) ||
                (debugGBuffer == DebugViewGbuffer.BakeDiffuseLightingWithAlbedoPlusEmissive) ||
                (fullScreenDebugMode == FullScreenDebugMode.PreRefractionColorPyramid || fullScreenDebugMode == FullScreenDebugMode.FinalColorPyramid || fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceReflections);
        }

        void RegisterDisplayStatsDebug()
        {
            m_DebugDisplayStatsItems = new DebugUI.Widget[]
            {
                new DebugUI.Value { displayName = "Frame Rate (fps)", getter = () => 1f / Time.smoothDeltaTime, refreshRate = 1f / 30f },
                new DebugUI.Value { displayName = "Frame Time (ms)", getter = () => Time.smoothDeltaTime * 1000f, refreshRate = 1f / 30f }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelDisplayStats, true);
            panel.flags = DebugUI.Flags.RuntimeOnly;
            panel.children.Add(m_DebugDisplayStatsItems);
        }

        public void RegisterMaterialDebug()
        {
            m_DebugMaterialItems = new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "Material", getter = () => materialDebugSettings.debugViewMaterial, setter = value => SetDebugViewMaterial(value), enumNames = MaterialDebugSettings.debugViewMaterialStrings, enumValues = MaterialDebugSettings.debugViewMaterialValues, getIndex = () => m_MaterialEnumIndex, setIndex = value => m_MaterialEnumIndex = value },
                new DebugUI.EnumField { displayName = "Engine", getter = () => materialDebugSettings.debugViewEngine, setter = value => SetDebugViewEngine(value), enumNames = MaterialDebugSettings.debugViewEngineStrings, enumValues = MaterialDebugSettings.debugViewEngineValues, getIndex = () => m_EngineEnumIndex, setIndex = value => m_EngineEnumIndex = value },
                new DebugUI.EnumField { displayName = "Attributes", getter = () => (int)materialDebugSettings.debugViewVarying, setter = value => SetDebugViewVarying((DebugViewVarying)value), autoEnum = typeof(DebugViewVarying), getIndex = () => m_AttributesEnumIndex, setIndex = value => m_AttributesEnumIndex = value },
                new DebugUI.EnumField { displayName = "Properties", getter = () => (int)materialDebugSettings.debugViewProperties, setter = value => SetDebugViewProperties((DebugViewProperties)value), autoEnum = typeof(DebugViewProperties), getIndex = () => m_PropertiesEnumIndex, setIndex = value => m_PropertiesEnumIndex = value },
                new DebugUI.EnumField { displayName = "GBuffer", getter = () => materialDebugSettings.debugViewGBuffer, setter = value => SetDebugViewGBuffer(value), enumNames = MaterialDebugSettings.debugViewMaterialGBufferStrings, enumValues = MaterialDebugSettings.debugViewMaterialGBufferValues, getIndex = () => m_GBufferEnumIndex, setIndex = value => m_GBufferEnumIndex = value }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelMaterials, true);
            panel.children.Add(m_DebugMaterialItems);
        }

        // For now we just rebuild the lighting panel if needed, but ultimately it could be done in a better way
        void RefreshLightingDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            RegisterLightingDebug();
        }

        void RefreshDecalsDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsItems);
            RegisterDecalsDebug();
        }

        void RefreshRenderingDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
            RegisterRenderingDebug();
        }

        public void RegisterLightingDebug()
        {
            var list = new List<DebugUI.Widget>();

            list.Add(new DebugUI.Foldout
            {
                displayName = "Show Light By Type",
                children = {
                    new DebugUI.BoolField { displayName = "Show Directional Lights", getter = () => lightingDebugSettings.showDirectionalLight, setter = value => lightingDebugSettings.showDirectionalLight = value },
                    new DebugUI.BoolField { displayName = "Show Punctual Lights", getter = () => lightingDebugSettings.showPunctualLight, setter = value => lightingDebugSettings.showPunctualLight = value },
                    new DebugUI.BoolField { displayName = "Show Area Lights", getter = () => lightingDebugSettings.showAreaLight, setter = value => lightingDebugSettings.showAreaLight = value },
                    new DebugUI.BoolField { displayName = "Show Reflection Probe", getter = () => lightingDebugSettings.showReflectionProbe, setter = value => lightingDebugSettings.showReflectionProbe = value },
                }
            });

            list.Add(new DebugUI.EnumField { displayName = "Shadow Debug Mode", getter = () => (int)lightingDebugSettings.shadowDebugMode, setter = value => SetShadowDebugMode((ShadowMapDebugMode)value), autoEnum = typeof(ShadowMapDebugMode), onValueChanged = RefreshLightingDebug, getIndex = () => m_ShadowDebugModeEnumIndex, setIndex = value => m_ShadowDebugModeEnumIndex = value });

            if (lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.VisualizeShadowMap || lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
            {
                var container = new DebugUI.Container();
                container.children.Add(new DebugUI.BoolField { displayName = "Use Selection", getter = () => lightingDebugSettings.shadowDebugUseSelection, setter = value => lightingDebugSettings.shadowDebugUseSelection = value, flags = DebugUI.Flags.EditorOnly, onValueChanged = RefreshLightingDebug });

                if (!lightingDebugSettings.shadowDebugUseSelection)
                    container.children.Add(new DebugUI.UIntField { displayName = "Shadow Map Index", getter = () => lightingDebugSettings.shadowMapIndex, setter = value => lightingDebugSettings.shadowMapIndex = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetCurrentShadowCount() - 1u });

                list.Add(container);
            }

            list.Add(new DebugUI.FloatField
            {
                displayName = "Global Shadow Scale Factor",
                getter = () => lightingDebugSettings.shadowResolutionScaleFactor,
                setter = (v) => lightingDebugSettings.shadowResolutionScaleFactor = v,
                min = () => 0.01f,
                max = () => 4.0f,
            });

            list.Add(new DebugUI.BoolField{
                displayName = "Clear Shadow atlas",
                getter = () => lightingDebugSettings.clearShadowAtlas,
                setter = (v) => lightingDebugSettings.clearShadowAtlas = v
            });

            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Min Value", getter = () => lightingDebugSettings.shadowMinValue, setter = value => lightingDebugSettings.shadowMinValue = value });
            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Max Value", getter = () => lightingDebugSettings.shadowMaxValue, setter = value => lightingDebugSettings.shadowMaxValue = value });

            list.Add(new DebugUI.EnumField { displayName = "Lighting Debug Mode", getter = () => (int)lightingDebugSettings.debugLightingMode, setter = value => SetDebugLightingMode((DebugLightingMode)value), autoEnum = typeof(DebugLightingMode), onValueChanged = RefreshLightingDebug, getIndex = () => m_LightingDebugModeEnumIndex, setIndex = value => m_LightingDebugModeEnumIndex = value });
            list.Add(new DebugUI.EnumField { displayName = "Fullscreen Debug Mode", getter = () => (int)fullScreenDebugMode, setter = value => SetFullScreenDebugMode((FullScreenDebugMode)value), enumNames = lightingFullScreenDebugStrings, enumValues = lightingFullScreenDebugValues, onValueChanged = RefreshLightingDebug, getIndex = () => m_LightingFulscreenDebugModeEnumIndex, setIndex = value => m_LightingFulscreenDebugModeEnumIndex = value });
            switch (fullScreenDebugMode)
            {
                case FullScreenDebugMode.PreRefractionColorPyramid:
                case FullScreenDebugMode.FinalColorPyramid:
                case FullScreenDebugMode.DepthPyramid:
                {
                    list.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.UIntField
                            {
                                displayName = "Fullscreen Debug Mip",
                                getter = () =>
                                    {
                                        int id;
                                        switch (fullScreenDebugMode)
                                        {
                                            case FullScreenDebugMode.FinalColorPyramid:
                                            case FullScreenDebugMode.PreRefractionColorPyramid:
                                                id = HDShaderIDs._ColorPyramidScale;
                                                break;
                                            default:
                                                id = HDShaderIDs._DepthPyramidScale;
                                                break;
                                        }
                                        var size = Shader.GetGlobalVector(id);
                                        float lodCount = size.z;
                                        return (uint)(fullscreenDebugMip * lodCount);
                                    },
                                setter = value =>
                                    {
                                        int id;
                                        switch (fullScreenDebugMode)
                                        {
                                            case FullScreenDebugMode.FinalColorPyramid:
                                            case FullScreenDebugMode.PreRefractionColorPyramid:
                                                id = HDShaderIDs._ColorPyramidScale;
                                                break;
                                            default:
                                                id = HDShaderIDs._DepthPyramidScale;
                                                break;
                                        }
                                        var size = Shader.GetGlobalVector(id);
                                        float lodCount = size.z;
                                        fullscreenDebugMip = (float)Convert.ChangeType(value, typeof(float)) / lodCount;
                                    },
                                min = () => 0u,
                                max = () =>
                                    {
                                        int id;
                                        switch (fullScreenDebugMode)
                                        {
                                            case FullScreenDebugMode.FinalColorPyramid:
                                            case FullScreenDebugMode.PreRefractionColorPyramid:
                                                id = HDShaderIDs._ColorPyramidScale;
                                                break;
                                            default:
                                                id = HDShaderIDs._DepthPyramidScale;
                                                break;
                                        }
                                        var size = Shader.GetGlobalVector(id);
                                        float lodCount = size.z;
                                        return (uint)lodCount;
                                    }
                            }
                        }
                    });
                    break;
                }
                default:
                    fullscreenDebugMip = 0;
                    break;
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Smoothness", getter = () => lightingDebugSettings.overrideSmoothness, setter = value => lightingDebugSettings.overrideSmoothness = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.overrideSmoothness)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Smoothness", getter = () => lightingDebugSettings.overrideSmoothnessValue, setter = value => lightingDebugSettings.overrideSmoothnessValue = value, min = () => 0f, max = () => 1f, incStep = 0.025f }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Albedo", getter = () => lightingDebugSettings.overrideAlbedo, setter = value => lightingDebugSettings.overrideAlbedo = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.overrideAlbedo)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Albedo", getter = () => lightingDebugSettings.overrideAlbedoValue, setter = value => lightingDebugSettings.overrideAlbedoValue = value, showAlpha = false, hdr = false }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Normal", getter = () => lightingDebugSettings.overrideNormal, setter = value => lightingDebugSettings.overrideNormal = value });

            list.Add(new DebugUI.BoolField { displayName = "Override Specular Color", getter = () => lightingDebugSettings.overrideSpecularColor, setter = value => lightingDebugSettings.overrideSpecularColor = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.overrideSpecularColor)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Specular Color", getter = () => lightingDebugSettings.overrideSpecularColorValue, setter = value => lightingDebugSettings.overrideSpecularColorValue = value, showAlpha = false, hdr = false }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Emissive Color", getter = () => lightingDebugSettings.overrideEmissiveColor, setter = value => lightingDebugSettings.overrideEmissiveColor = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.overrideEmissiveColor)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Emissive Color", getter = () => lightingDebugSettings.overrideEmissiveColorValue, setter = value => lightingDebugSettings.overrideEmissiveColorValue = value, showAlpha = false, hdr = true }
                    }
                });
            }

            list.Add(new DebugUI.EnumField { displayName = "Tile/Cluster Debug", getter = () => (int)lightingDebugSettings.tileClusterDebug, setter = value => lightingDebugSettings.tileClusterDebug = (LightLoop.TileClusterDebug)value, autoEnum = typeof(LightLoop.TileClusterDebug), onValueChanged = RefreshLightingDebug, getIndex = () => m_TileClusterDebugEnumIndex, setIndex = value => m_TileClusterDebugEnumIndex = value });
            if (lightingDebugSettings.tileClusterDebug != LightLoop.TileClusterDebug.None && lightingDebugSettings.tileClusterDebug != LightLoop.TileClusterDebug.MaterialFeatureVariants)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Tile/Cluster Debug By Category", getter = () => (int)lightingDebugSettings.tileClusterDebugByCategory, setter = value => lightingDebugSettings.tileClusterDebugByCategory = (LightLoop.TileClusterCategoryDebug)value, autoEnum = typeof(LightLoop.TileClusterCategoryDebug), getIndex = () => m_TileClusterDebugByCategoryEnumIndex, setIndex = value => m_TileClusterDebugByCategoryEnumIndex = value }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Display Sky Reflection", getter = () => lightingDebugSettings.displaySkyReflection, setter = value => lightingDebugSettings.displaySkyReflection = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.displaySkyReflection)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Sky Reflection Mipmap", getter = () => lightingDebugSettings.skyReflectionMipmap, setter = value => lightingDebugSettings.skyReflectionMipmap = value, min = () => 0f, max = () => 1f, incStep = 0.05f }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Display Light Volumes", getter = () => lightingDebugSettings.displayLightVolumes, setter = value => lightingDebugSettings.displayLightVolumes = value, onValueChanged = RefreshLightingDebug });
            if (lightingDebugSettings.displayLightVolumes)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Light Volume Debug Type", getter = () => (int)lightingDebugSettings.lightVolumeDebugByCategory, setter = value => lightingDebugSettings.lightVolumeDebugByCategory = (LightLoop.LightVolumeDebug)value, autoEnum = typeof(LightLoop.LightVolumeDebug), getIndex = () => m_LightVolumeDebugTypeEnumIndex, setIndex = value => m_LightVolumeDebugTypeEnumIndex = value },
                        new DebugUI.UIntField { displayName = "Max Debug Light Count", getter = () => (uint)lightingDebugSettings.maxDebugLightCount, setter = value => lightingDebugSettings.maxDebugLightCount = value, min = () => 0, max = () => 24, incStep = 1 }
                    }
                });
            }

            if (DebugNeedsExposure())
                list.Add(new DebugUI.FloatField { displayName = "Debug Exposure", getter = () => lightingDebugSettings.debugExposure, setter = value => lightingDebugSettings.debugExposure = value });


            m_DebugLightingItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelLighting, true);
            panel.children.Add(m_DebugLightingItems);
        }

        public void RegisterRenderingDebug()
        {
            var widgetList = new List<DebugUI.Widget>();

            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "Fullscreen Debug Mode", getter = () => (int)fullScreenDebugMode, setter = value => fullScreenDebugMode = (FullScreenDebugMode)value, enumNames = renderingFullScreenDebugStrings, enumValues = renderingFullScreenDebugValues, getIndex = () => m_RenderingFulscreenDebugModeEnumIndex, setIndex = value => m_RenderingFulscreenDebugModeEnumIndex = value },
                new DebugUI.EnumField { displayName = "MipMaps", getter = () => (int)mipMapDebugSettings.debugMipMapMode, setter = value => SetMipMapMode((DebugMipMapMode)value), autoEnum = typeof(DebugMipMapMode), onValueChanged = RefreshRenderingDebug, getIndex = () => m_MipMapsEnumIndex, setIndex = value => m_MipMapsEnumIndex = value },
            });

            if (mipMapDebugSettings.debugMipMapMode != DebugMipMapMode.None)
            {
                widgetList.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Terrain Texture", getter = ()=>(int)mipMapDebugSettings.terrainTexture, setter = value => mipMapDebugSettings.terrainTexture = (DebugMipMapModeTerrainTexture)value, autoEnum = typeof(DebugMipMapModeTerrainTexture), getIndex = () => m_TerrainTextureEnumIndex, setIndex = value => m_TerrainTextureEnumIndex = value }
                    }
                });
            }

            widgetList.AddRange(new []
            {
                new DebugUI.Container
                {
                    displayName = "Color Picker",
                    flags = DebugUI.Flags.EditorOnly,
                    children =
                    {
                        new DebugUI.EnumField  { displayName = "Debug Mode", getter = () => (int)colorPickerDebugSettings.colorPickerMode, setter = value => colorPickerDebugSettings.colorPickerMode = (ColorPickerDebugMode)value, autoEnum = typeof(ColorPickerDebugMode), getIndex = () => m_ColorPickerDebugModeEnumIndex, setIndex = value => m_ColorPickerDebugModeEnumIndex = value },
                        new DebugUI.ColorField { displayName = "Font Color", flags = DebugUI.Flags.EditorOnly, getter = () => colorPickerDebugSettings.fontColor, setter = value => colorPickerDebugSettings.fontColor = value }
                    }
                }
            });
            
            widgetList.Add(new DebugUI.BoolField  { displayName = "False Color Mode", getter = () => falseColorDebugSettings.falseColor, setter = value => falseColorDebugSettings.falseColor = value, onValueChanged = RefreshRenderingDebug });
            if (falseColorDebugSettings.falseColor)
            {
                widgetList.Add(new DebugUI.Container{
                    flags = DebugUI.Flags.EditorOnly,
                    children = 
                    {
                        new DebugUI.FloatField { displayName = "Range Threshold 0", getter = () => falseColorDebugSettings.colorThreshold0, setter = value => falseColorDebugSettings.colorThreshold0 = Mathf.Min(value, falseColorDebugSettings.colorThreshold1) },
                        new DebugUI.FloatField { displayName = "Range Threshold 1", getter = () => falseColorDebugSettings.colorThreshold1, setter = value => falseColorDebugSettings.colorThreshold1 = Mathf.Clamp(value, falseColorDebugSettings.colorThreshold0, falseColorDebugSettings.colorThreshold2) },
                        new DebugUI.FloatField { displayName = "Range Threshold 2", getter = () => falseColorDebugSettings.colorThreshold2, setter = value => falseColorDebugSettings.colorThreshold2 = Mathf.Clamp(value, falseColorDebugSettings.colorThreshold1, falseColorDebugSettings.colorThreshold3) },
                        new DebugUI.FloatField { displayName = "Range Threshold 3", getter = () => falseColorDebugSettings.colorThreshold3, setter = value => falseColorDebugSettings.colorThreshold3 = Mathf.Max(value, falseColorDebugSettings.colorThreshold2) },
                    }
                });
            }

            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "MSAA Samples", getter = () => (int)msaaSamples, setter = value => msaaSamples = (MSAASamples)value, enumNames = msaaSamplesDebugStrings, enumValues = msaaSamplesDebugValues, getIndex = () => m_MsaaSampleDebugModeEnumIndex, setIndex = value => m_MsaaSampleDebugModeEnumIndex = value },
            });

            widgetList.AddRange(new DebugUI.Widget[]
            {
                    new DebugUI.EnumField { displayName = "Freeze Camera for culling", getter = () => debugCameraToFreeze, setter = value => debugCameraToFreeze = value, enumNames = cameraNamesStrings, enumValues = cameraNamesValues, getIndex = () => m_DebugCameraToFreezeIndex, setIndex = value => m_DebugCameraToFreezeIndex = value },
            });

            m_DebugRenderingItems = widgetList.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelRendering, true);
            panel.children.Add(m_DebugRenderingItems);
        }

        public void RegisterDecalsDebug()
        {
            m_DebugDecalsItems = new DebugUI.Widget[]
            {
                new DebugUI.BoolField { displayName = "Display atlas", getter = () => decalsDebugSettings.m_DisplayAtlas, setter = value => decalsDebugSettings.m_DisplayAtlas = value},
                new DebugUI.UIntField { displayName = "Mip Level", getter = () => decalsDebugSettings.m_MipLevel, setter = value => decalsDebugSettings.m_MipLevel = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetDecalAtlasMipCount() }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelDecals, true);
            panel.children.Add(m_DebugDecalsItems);
        }

        public void RegisterDebug()
        {
            RegisterDecalsDebug();
            RegisterDisplayStatsDebug();
            RegisterMaterialDebug();
            RegisterLightingDebug();
            RegisterRenderingDebug();
        }

        public void UnregisterDebug()
        {
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsItems);
            UnregisterDebugItems(k_PanelDisplayStats, m_DebugDisplayStatsItems);
            UnregisterDebugItems(k_PanelMaterials, m_DebugMaterialItems);
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
        }

        void UnregisterDebugItems(string panelName, DebugUI.Widget[] items)
        {
            var panel = DebugManager.instance.GetPanel(panelName);
            if (panel != null)
                panel.children.Remove(items);
        }

        void FillFullScreenDebugEnum(ref GUIContent[] strings, ref int[] values, FullScreenDebugMode min, FullScreenDebugMode max)
        {
            int count = max - min - 1;
            strings = new GUIContent[count + 1];
            values = new int[count + 1];
            strings[0] = new GUIContent(FullScreenDebugMode.None.ToString());
            values[0] = (int)FullScreenDebugMode.None;
            int index = 1;
            for (int i = (int)min + 1; i < (int)max; ++i)
            {
                strings[index] = new GUIContent(((FullScreenDebugMode)i).ToString());
                values[index] = i;
                index++;
            }
        }

        static string FormatVector(Vector3 v)
        {
            return string.Format("({0:F6}, {1:F6}, {2:F6})", v.x, v.y, v.z);
        }

        public static void RegisterCamera(string cameraName)
        {
            if (cameraNames.FindIndex(x => x.text.Equals(cameraName)) < 0)
            {
                cameraNames.Add(new GUIContent(cameraName));
                needsRefreshingCameraFreezeList = true;
            }
        }

        public static void UnRegisterCamera(string cameraName)
        {
            int indexOfCamera = cameraNames.FindIndex(x => x.text.Equals(cameraName));
            if (indexOfCamera > 0)
            {
                cameraNames.RemoveAt(indexOfCamera);
                needsRefreshingCameraFreezeList = true;
            }
        }
    }
}
