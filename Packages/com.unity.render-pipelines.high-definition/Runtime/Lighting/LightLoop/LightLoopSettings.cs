using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Flags]
    public enum LightLoopSettingsOverrides
    {
        FptlForForwardOpaque = 1 << 0,
        BigTilePrepass = 1 << 1,
        ComputeLightEvaluation = 1 << 2,
        ComputeLightVariants = 1 << 3,
        ComputeMaterialVariants = 1 << 4,
        TileAndCluster = 1 << 5,
        //Fptl = 1 << 6, //isFptlEnabled set up by system
    }

    [Serializable]
    public class LightLoopSettings
    {
        static Dictionary<LightLoopSettingsOverrides, Action<LightLoopSettings, LightLoopSettings>> s_Overrides = new Dictionary<LightLoopSettingsOverrides, Action<LightLoopSettings, LightLoopSettings>>
        {
            {LightLoopSettingsOverrides.FptlForForwardOpaque, (a, b) => { a.enableFptlForForwardOpaque = b.enableFptlForForwardOpaque; } },
            {LightLoopSettingsOverrides.BigTilePrepass, (a, b) => { a.enableBigTilePrepass = b.enableBigTilePrepass; } },
            {LightLoopSettingsOverrides.ComputeLightEvaluation, (a, b) => { a.enableComputeLightEvaluation = b.enableComputeLightEvaluation; } },
            {LightLoopSettingsOverrides.ComputeLightVariants, (a, b) => { a.enableComputeLightVariants = b.enableComputeLightVariants; } },
            {LightLoopSettingsOverrides.ComputeMaterialVariants, (a, b) => { a.enableComputeMaterialVariants = b.enableComputeMaterialVariants; } },
            {LightLoopSettingsOverrides.TileAndCluster, (a, b) => { a.enableTileAndCluster = b.enableTileAndCluster; } },
        };

        public LightLoopSettingsOverrides overrides;

        // Setup by the users
        public bool enableTileAndCluster = true;
        public bool enableComputeLightEvaluation = true;
        public bool enableComputeLightVariants = true;
        public bool enableComputeMaterialVariants = true;
        // Deferred opaque always use FPTL, forward opaque can use FPTL or cluster, transparent always use cluster
        // When MSAA is enabled, we only support cluster (Fptl is too slow with MSAA), and we don't support MSAA for deferred path (mean it is ok to keep fptl)
        public bool enableFptlForForwardOpaque = true;
        public bool enableBigTilePrepass = true;

        // Setup by system
        public bool isFptlEnabled = true;

        public LightLoopSettings() { }
        public LightLoopSettings(LightLoopSettings toCopy)
        {
            toCopy.CopyTo(this);
        }

        public void CopyTo(LightLoopSettings lightLoopSettings)
        {
            lightLoopSettings.enableTileAndCluster = this.enableTileAndCluster;
            lightLoopSettings.enableComputeLightEvaluation = this.enableComputeLightEvaluation;
            lightLoopSettings.enableComputeLightVariants = this.enableComputeLightVariants;
            lightLoopSettings.enableComputeMaterialVariants = this.enableComputeMaterialVariants;

            lightLoopSettings.enableFptlForForwardOpaque = this.enableFptlForForwardOpaque;
            lightLoopSettings.enableBigTilePrepass = this.enableBigTilePrepass;

            lightLoopSettings.isFptlEnabled = this.isFptlEnabled;

            lightLoopSettings.overrides = this.overrides;
        }

        public LightLoopSettings Override(LightLoopSettings overridedFrameSettings)
        {
            if (overrides == 0)
            {
                //nothing to override
                return overridedFrameSettings;
            }

            LightLoopSettings result = new LightLoopSettings(overridedFrameSettings);
            Array values = Enum.GetValues(typeof(LightLoopSettingsOverrides));
            foreach (LightLoopSettingsOverrides val in values)
            {
                if ((val & overrides) > 0)
                {
                    s_Overrides[val](result, this);
                }
            }

            //propagate override to be chained
            result.overrides = overrides | overridedFrameSettings.overrides;
            return result;
        }

        // aggregateFrameSettings already contain the aggregation of RenderPipelineSettings and FrameSettings (regular and/or debug)
        public static void InitializeLightLoopSettings(Camera camera, FrameSettings aggregateFrameSettings,
            RenderPipelineSettings renderPipelineSettings, FrameSettings frameSettings,
            ref LightLoopSettings aggregate)
        {
            if (aggregate == null)
                aggregate = new LightLoopSettings();

            aggregate.enableTileAndCluster = frameSettings.lightLoopSettings.enableTileAndCluster;
            aggregate.enableComputeLightEvaluation = frameSettings.lightLoopSettings.enableComputeLightEvaluation;
            aggregate.enableComputeLightVariants = frameSettings.lightLoopSettings.enableComputeLightVariants;
            aggregate.enableComputeMaterialVariants = frameSettings.lightLoopSettings.enableComputeMaterialVariants;
            aggregate.enableFptlForForwardOpaque = frameSettings.lightLoopSettings.enableFptlForForwardOpaque;
            aggregate.enableBigTilePrepass = frameSettings.lightLoopSettings.enableBigTilePrepass;

            // Deferred opaque are always using Fptl. Forward opaque can use Fptl or Cluster, transparent use cluster.
            // When MSAA is enabled we disable Fptl as it become expensive compare to cluster
            // In HD, MSAA is only supported for forward only rendering, no MSAA in deferred mode (for code complexity reasons)
            aggregate.enableFptlForForwardOpaque = aggregate.enableFptlForForwardOpaque && !aggregateFrameSettings.enableMSAA;

            // disable FPTL for stereo for now
            aggregate.enableFptlForForwardOpaque = aggregate.enableFptlForForwardOpaque && !XRGraphics.enabled;

            // If Deferred, enable Fptl. If we are forward renderer only and not using Fptl for forward opaque, disable Fptl
            aggregate.isFptlEnabled = aggregateFrameSettings.shaderLitMode == LitShaderMode.Deferred || aggregate.enableFptlForForwardOpaque;
        }

        public static void RegisterDebug(LightLoopSettings lightLoopSettings, List<DebugUI.Widget> widgets)
        {
            widgets.AddRange(new[]
            {
                new DebugUI.Foldout
                {
                    displayName = "Light Loop Settings",
                    children =
                    {
                        // Uncomment if you re-enable LIGHTLOOP_SINGLE_PASS multi_compile in lit*.shader
                        //new DebugUI.BoolField { displayName = "Enable Tile/Cluster", getter = () => lightLoopSettings.enableTileAndCluster, setter = value => lightLoopSettings.enableTileAndCluster = value },
                        new DebugUI.BoolField { displayName = "Enable Fptl for Forward Opaque", getter = () => lightLoopSettings.enableFptlForForwardOpaque, setter = value => lightLoopSettings.enableFptlForForwardOpaque = value },
                        new DebugUI.BoolField { displayName = "Enable Big Tile", getter = () => lightLoopSettings.enableBigTilePrepass, setter = value => lightLoopSettings.enableBigTilePrepass = value },
                        new DebugUI.BoolField { displayName = "Enable Compute Lighting", getter = () => lightLoopSettings.enableComputeLightEvaluation, setter = value => lightLoopSettings.enableComputeLightEvaluation = value },
                        new DebugUI.BoolField { displayName = "Enable Light Classification", getter = () => lightLoopSettings.enableComputeLightVariants, setter = value => lightLoopSettings.enableComputeLightVariants = value },
                        new DebugUI.BoolField { displayName = "Enable Material Classification", getter = () => lightLoopSettings.enableComputeMaterialVariants, setter = value => lightLoopSettings.enableComputeMaterialVariants = value }
                    }
                }
            });
        }
    }
}
