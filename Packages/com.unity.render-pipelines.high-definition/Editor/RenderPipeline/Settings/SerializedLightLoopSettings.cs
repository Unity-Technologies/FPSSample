using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SerializedLightLoopSettings
    {
        public SerializedProperty root;

        public SerializedProperty enableTileAndCluster;
        public SerializedProperty enableComputeLightEvaluation;
        public SerializedProperty enableComputeLightVariants;
        public SerializedProperty enableComputeMaterialVariants;
        public SerializedProperty enableFptlForForwardOpaque;
        public SerializedProperty enableBigTilePrepass;
        public SerializedProperty isFptlEnabled;

        private SerializedProperty overrides;
        public bool overridesFptlForForwardOpaque
        {
            get { return (overrides.intValue & (int)LightLoopSettingsOverrides.FptlForForwardOpaque) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)LightLoopSettingsOverrides.FptlForForwardOpaque;
                else
                    overrides.intValue &= ~(int)LightLoopSettingsOverrides.FptlForForwardOpaque;
            }
        }
        public bool overridesBigTilePrepass
        {
            get { return (overrides.intValue & (int)LightLoopSettingsOverrides.BigTilePrepass) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)LightLoopSettingsOverrides.BigTilePrepass;
                else
                    overrides.intValue &= ~(int)LightLoopSettingsOverrides.BigTilePrepass;
            }
        }
        public bool overridesComputeLightEvaluation
        {
            get { return (overrides.intValue & (int)LightLoopSettingsOverrides.ComputeLightEvaluation) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)LightLoopSettingsOverrides.ComputeLightEvaluation;
                else
                    overrides.intValue &= ~(int)LightLoopSettingsOverrides.ComputeLightEvaluation;
            }
        }
        public bool overridesComputeLightVariants
        {
            get { return (overrides.intValue & (int)LightLoopSettingsOverrides.ComputeLightVariants) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)LightLoopSettingsOverrides.ComputeLightVariants;
                else
                    overrides.intValue &= ~(int)LightLoopSettingsOverrides.ComputeLightVariants;
            }
        }
        public bool overridesComputeMaterialVariants
        {
            get { return (overrides.intValue & (int)LightLoopSettingsOverrides.ComputeMaterialVariants) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)LightLoopSettingsOverrides.ComputeMaterialVariants;
                else
                    overrides.intValue &= ~(int)LightLoopSettingsOverrides.ComputeMaterialVariants;
            }
        }
        public bool overridesTileAndCluster
        {
            get { return (overrides.intValue & (int)LightLoopSettingsOverrides.TileAndCluster) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)LightLoopSettingsOverrides.TileAndCluster;
                else
                    overrides.intValue &= ~(int)LightLoopSettingsOverrides.TileAndCluster;
            }
        }

        public SerializedLightLoopSettings(SerializedProperty root)
        {
            this.root = root;

            enableTileAndCluster = root.Find((LightLoopSettings l) => l.enableTileAndCluster);
            enableComputeLightEvaluation = root.Find((LightLoopSettings l) => l.enableComputeLightEvaluation);
            enableComputeLightVariants = root.Find((LightLoopSettings l) => l.enableComputeLightVariants);
            enableComputeMaterialVariants = root.Find((LightLoopSettings l) => l.enableComputeMaterialVariants);
            enableFptlForForwardOpaque = root.Find((LightLoopSettings l) => l.enableFptlForForwardOpaque);
            enableBigTilePrepass = root.Find((LightLoopSettings l) => l.enableBigTilePrepass);
            isFptlEnabled = root.Find((LightLoopSettings l) => l.isFptlEnabled);

            overrides = root.Find((LightLoopSettings l) => l.overrides);
        }
    }
}
