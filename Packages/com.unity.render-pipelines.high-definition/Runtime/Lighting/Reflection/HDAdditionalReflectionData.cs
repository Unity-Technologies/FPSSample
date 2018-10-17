using UnityEngine.Serialization;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [RequireComponent(typeof(ReflectionProbe))]
    public class HDAdditionalReflectionData : HDProbe
    {
        enum Version
        {
            First,
            Second,
            HDProbeChild,
            UseInfluenceVolume,
            MergeEditors,
            AddCaptureSettingsAndFrameSettings,
            // Add new version here and they will automatically be the Current one
            Max,
            Current = Max - 1
        }

        [SerializeField, FormerlySerializedAs("version")]
        int m_Version;

        ReflectionProbe m_LegacyProbe;
        /// <summary>Get the sibling component ReflectionProbe</summary>
        public ReflectionProbe reflectionProbe
        {
            get
            {
                if(m_LegacyProbe == null || m_LegacyProbe.Equals(null))
                {
                    m_LegacyProbe = GetComponent<ReflectionProbe>();
                }
                return m_LegacyProbe;
            }
        }

#pragma warning disable 649 //never assigned
        //data only kept for migration, to be removed in future version
        [SerializeField, System.Obsolete("influenceShape is deprecated, use influenceVolume parameters instead")]
        InfluenceShape influenceShape;
        [SerializeField, System.Obsolete("influenceSphereRadius is deprecated, use influenceVolume parameters instead")]
        float influenceSphereRadius = 3.0f;
        [SerializeField, System.Obsolete("blendDistancePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 blendDistancePositive = Vector3.zero;
        [SerializeField, System.Obsolete("blendDistanceNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 blendDistanceNegative = Vector3.zero;
        [SerializeField, System.Obsolete("blendNormalDistancePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 blendNormalDistancePositive = Vector3.zero;
        [SerializeField, System.Obsolete("blendNormalDistanceNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 blendNormalDistanceNegative = Vector3.zero;
        [SerializeField, System.Obsolete("boxSideFadePositive is deprecated, use influenceVolume parameters instead")]
        Vector3 boxSideFadePositive = Vector3.one;
        [SerializeField, System.Obsolete("boxSideFadeNegative is deprecated, use influenceVolume parameters instead")]
        Vector3 boxSideFadeNegative = Vector3.one;
#pragma warning restore 649 //never assigned

        bool needMigrateToHDProbeChild = false;
        bool needMigrateToUseInfluenceVolume = false;
        bool needMigrateToMergeEditors = false;
        bool needMigrateAddCaptureSettingsAndFrameSettings = false;

        public void CopyTo(HDAdditionalReflectionData data)
        {
            influenceVolume.CopyTo(data.influenceVolume);
            data.influenceVolume.shape = influenceVolume.shape; //force the legacy probe to refresh its size

            data.mode = mode;
            data.refreshMode = refreshMode;
            data.multiplier = multiplier;
            data.weight = weight;
        }

        bool CheckMigrationRequirement()
        {
            //exit as quicker as possible
            if (m_Version == (int)Version.Current)
                return false;

            //it is mandatory to call them in order
            //they can be grouped (without 'else' or not
            if (m_Version < (int)Version.HDProbeChild)
            {
                needMigrateToHDProbeChild = true;
            }
            if (m_Version < (int)Version.UseInfluenceVolume)
            {
                needMigrateToUseInfluenceVolume = true;
            }
            if (m_Version < (int)Version.MergeEditors)
            {
                needMigrateToMergeEditors = true;
            }
            if (m_Version < (int)Version.AddCaptureSettingsAndFrameSettings)
            {
                needMigrateAddCaptureSettingsAndFrameSettings = true;
            }
            //mandatory 'else' to only update version if other migrations done
            else if (m_Version < (int)Version.Current)
            {
                m_Version = (int)Version.Current;
                return false;
            }
            return true;
        }

        void ApplyMigration()
        {
            //it is mandatory to call them in order
            if (needMigrateToHDProbeChild)
                MigrateToHDProbeChild();
            if (needMigrateToUseInfluenceVolume)
                MigrateToUseInfluenceVolume();
            if (needMigrateToMergeEditors)
                MigrateToMergeEditors();
            if (needMigrateAddCaptureSettingsAndFrameSettings)
                MigrateAddCaptureSettingsAndFrameSettings();
        }

        void Migrate()
        {
            //Must not be called at deserialisation time if require other component
            while (CheckMigrationRequirement())
            {
                ApplyMigration();
            }
        }

        internal override void Awake()
        {
            base.Awake();

            //launch migration at creation too as m_Version could not have an
            //existence in older version
            Migrate();
        }

        void OnEnable()
        {
            ReflectionSystem.RegisterProbe(this);
        }

        void OnDisable()
        {
            ReflectionSystem.UnregisterProbe(this);
        }

        void OnValidate()
        {
            ReflectionSystem.UnregisterProbe(this);

            if (isActiveAndEnabled)
                ReflectionSystem.RegisterProbe(this);
        }

        void MigrateToHDProbeChild()
        {
            mode = reflectionProbe.mode;
            refreshMode = reflectionProbe.refreshMode;
            m_Version = (int)Version.HDProbeChild;
            needMigrateToHDProbeChild = false;
        }

        void MigrateToUseInfluenceVolume()
        {
            influenceVolume.boxSize = reflectionProbe.size;
#pragma warning disable CS0618 // Type or member is obsolete
            influenceVolume.sphereRadius = influenceSphereRadius;
            influenceVolume.shape = influenceShape; //must be done after each size transfert
            influenceVolume.boxBlendDistancePositive = blendDistancePositive;
            influenceVolume.boxBlendDistanceNegative = blendDistanceNegative;
            influenceVolume.boxBlendNormalDistancePositive = blendNormalDistancePositive;
            influenceVolume.boxBlendNormalDistanceNegative = blendNormalDistanceNegative;
            influenceVolume.boxSideFadePositive = boxSideFadePositive;
            influenceVolume.boxSideFadeNegative = boxSideFadeNegative;
#pragma warning restore CS0618 // Type or member is obsolete
            m_Version = (int)Version.UseInfluenceVolume;
            needMigrateToUseInfluenceVolume = false;

            //Note: former editor parameters will be recreated as if non existent.
            //User will lose parameters corresponding to non used mode between simplified and advanced
        }

        void MigrateToMergeEditors()
        {
            infiniteProjection = !reflectionProbe.boxProjection;
            reflectionProbe.boxProjection = false;
            m_Version = (int)Version.MergeEditors;
            needMigrateToMergeEditors = false;
        }

        void MigrateAddCaptureSettingsAndFrameSettings()
        {
            captureSettings.shadowDistance = reflectionProbe.shadowDistance;
            captureSettings.cullingMask = reflectionProbe.cullingMask;
#if UNITY_EDITOR //m_UseOcclusionCulling is not exposed in c# !
            UnityEditor.SerializedObject serializedReflectionProbe = new UnityEditor.SerializedObject(reflectionProbe);
            captureSettings.useOcclusionCulling = serializedReflectionProbe.FindProperty("m_UseOcclusionCulling").boolValue;
#endif
            captureSettings.nearClipPlane = reflectionProbe.nearClipPlane;
            captureSettings.farClipPlane = reflectionProbe.farClipPlane;
            m_Version = (int)Version.AddCaptureSettingsAndFrameSettings;
            needMigrateAddCaptureSettingsAndFrameSettings = false;
        }

        public override Texture customTexture { get { return reflectionProbe.customBakedTexture; } set { reflectionProbe.customBakedTexture = value; } }
        public override Texture bakedTexture { get { return reflectionProbe.bakedTexture; } set { reflectionProbe.bakedTexture = value; } }

        public override ReflectionProbeMode mode
        {
            set
            {
                base.mode = value;
                reflectionProbe.mode = value; //ensure compatibility till we capture without the legacy component
                if(value == ReflectionProbeMode.Realtime)
                {
                    refreshMode = ReflectionProbeRefreshMode.EveryFrame;
                }
            }
        }
        public override ReflectionProbeRefreshMode refreshMode
        {
            set
            {
                base.refreshMode = value;
                reflectionProbe.refreshMode = value; //ensure compatibility till we capture without the legacy component
            }
        }

        internal override void UpdatedInfluenceVolumeShape(Vector3 size, Vector3 offset)
        {
            reflectionProbe.size = size;
            reflectionProbe.center = transform.rotation*offset;
        }
    }
}
