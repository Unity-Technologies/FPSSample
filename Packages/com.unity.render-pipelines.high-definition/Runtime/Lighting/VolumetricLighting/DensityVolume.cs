using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public struct DensityVolumeArtistParameters
    {
        public Color     albedo;       // Single scattering albedo [0, 1]. Alpha is ignored
        public float     meanFreePath; // In meters [1, inf]. Should be chromatic - this is an optimization!
        public float     asymmetry;    // Only used if (isLocal == false)

        public Texture3D volumeMask;
        public Vector3   textureScrollingSpeed;
        public Vector3   textureTiling;

        [SerializeField, FormerlySerializedAs("positiveFade")]
        private Vector3  m_PositiveFade;
        [SerializeField, FormerlySerializedAs("negativeFade")]
        private Vector3  m_NegativeFade;
        [SerializeField]
        private float    m_UniformFade;
        public Vector3   size;
        public bool      advancedFade;
        public bool      invertFade;

        public  int      textureIndex; // This shouldn't be public... Internal, maybe?
        private Vector3  volumeScrollingAmount;

        public Vector3 positiveFade
        {
            get
            {
                return advancedFade ? m_PositiveFade : m_UniformFade * Vector3.one;
            }
            set
            {
                if (advancedFade)
                {
                    m_PositiveFade = value;
                }
                else
                {
                    m_UniformFade = value.x;
                }
            }
        }

        public Vector3 negativeFade
        {
            get
            {
                return advancedFade ? m_NegativeFade : m_UniformFade * Vector3.one;
            }
            set
            {
                if (advancedFade)
                {
                    m_NegativeFade = value;
                }
                else
                {
                    m_UniformFade = value.x;
                }
            }
        }

        public DensityVolumeArtistParameters(Color color, float _meanFreePath, float _asymmetry)
        {
            albedo                = color;
            meanFreePath          = _meanFreePath;
            asymmetry             = _asymmetry;

            volumeMask            = null;
            textureIndex          = -1;
            textureScrollingSpeed = Vector3.zero;
            textureTiling         = Vector3.one;
            volumeScrollingAmount = textureScrollingSpeed;

            size                  = Vector3.one;

            m_PositiveFade        = Vector3.zero;
            m_NegativeFade        = Vector3.zero;
            m_UniformFade         = 0f;
            advancedFade          = false;
            invertFade            = false;
        }

        public void Update(bool animate, float time)
        {
            //Update scrolling based on deltaTime
            if (volumeMask != null)
            {
                float animationTime = animate ? time : 0.0f;
                volumeScrollingAmount = (textureScrollingSpeed * animationTime);
                // Switch from right-handed to left-handed coordinate system.
                volumeScrollingAmount.x = -volumeScrollingAmount.x;
                volumeScrollingAmount.y = -volumeScrollingAmount.y;
            }
        }

        public void Constrain()
        {
            albedo.r = Mathf.Clamp01(albedo.r);
            albedo.g = Mathf.Clamp01(albedo.g);
            albedo.b = Mathf.Clamp01(albedo.b);
            albedo.a = 1.0f;

            meanFreePath = Mathf.Clamp(meanFreePath, 1.0f, float.MaxValue);

            asymmetry = Mathf.Clamp(asymmetry, -1.0f, 1.0f);

            volumeScrollingAmount = Vector3.zero;
        }

        public DensityVolumeEngineData ConvertToEngineData()
        {
            DensityVolumeEngineData data = new DensityVolumeEngineData();

            data.extinction     = VolumeRenderingUtils.ExtinctionFromMeanFreePath(meanFreePath);
            data.scattering     = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(data.extinction, (Vector3)(Vector4)albedo);

            data.textureIndex   = textureIndex;
            data.textureScroll  = volumeScrollingAmount;
            data.textureTiling  = textureTiling;

            // Clamp to avoid NaNs.
            Vector3 positiveFade = this.positiveFade;
            Vector3 negativeFade = this.negativeFade;

            data.rcpPosFade.x = Mathf.Min(1.0f / positiveFade.x, float.MaxValue);
            data.rcpPosFade.y = Mathf.Min(1.0f / positiveFade.y, float.MaxValue);
            data.rcpPosFade.z = Mathf.Min(1.0f / positiveFade.z, float.MaxValue);

            data.rcpNegFade.y = Mathf.Min(1.0f / negativeFade.y, float.MaxValue);
            data.rcpNegFade.x = Mathf.Min(1.0f / negativeFade.x, float.MaxValue);
            data.rcpNegFade.z = Mathf.Min(1.0f / negativeFade.z, float.MaxValue);

            data.invertFade = invertFade ? 1 : 0;

            return data;
        }
    } // class DensityVolumeParameters

    [ExecuteAlways]
    [AddComponentMenu("Rendering/Density Volume", 1100)]
    public class DensityVolume : MonoBehaviour
    {
        enum Version
        {
            First,
            ScaleIndependent,
            // Add new version here and they will automatically be the Current one
            Max,
            Current = Max - 1
        }

        [SerializeField]
        int m_Version;

        bool needMigrateToScaleIndependent = false;

        public DensityVolumeArtistParameters parameters = new DensityVolumeArtistParameters(Color.white, 10.0f, 0.0f);

        private Texture3D previousVolumeMask = null;

        public Action OnTextureUpdated;

        //Gather and Update any parameters that may have changed
        public void PrepareParameters(bool animate, float time)
        {
            //Texture has been updated notify the manager
            if (previousVolumeMask != parameters.volumeMask)
            {
                NotifyUpdatedTexure();
                previousVolumeMask = parameters.volumeMask;
            }

            parameters.Update(animate, time);
        }

        private void NotifyUpdatedTexure()
        {
            if (OnTextureUpdated != null)
            {
                OnTextureUpdated();
            }
        }

        private void Awake()
        {
            Migrate();
        }

        bool CheckMigrationRequirement()
        {
            //exit as quicker as possible
            if (m_Version == (int)Version.Current)
                return false;

            //it is mandatory to call them in order
            //they can be grouped (without 'else' or not)
            if (m_Version < (int)Version.ScaleIndependent)
            {
                needMigrateToScaleIndependent = true;
            }
            return true;
        }

        void ApplyMigration()
        {
            //it is mandatory to call them in order
            if (needMigrateToScaleIndependent)
                MigrateToScaleIndependent();
        }

        void Migrate()
        {
            //Must not be called at deserialisation time if require other component
            while (CheckMigrationRequirement())
            {
                ApplyMigration();
            }
        }

        void MigrateToScaleIndependent()
        {
            parameters.size = transform.lossyScale;
            m_Version = (int)Version.ScaleIndependent;
            needMigrateToScaleIndependent = false;
        }

        private void OnEnable()
        {
            DensityVolumeManager.manager.RegisterVolume(this);
        }

        private void OnDisable()
        {
            DensityVolumeManager.manager.DeRegisterVolume(this);
        }

        private void Update()
        {
        }

        private void OnValidate()
        {
            parameters.Constrain();
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
