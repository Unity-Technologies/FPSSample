using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteAlways]
    public abstract class HDProbe : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField, FormerlySerializedAs("proxyVolumeComponent"), FormerlySerializedAs("m_ProxyVolumeReference")]
        ReflectionProxyVolumeComponent m_ProxyVolume = null;
        [SerializeField]
        bool m_InfiniteProjection = true; //usable when no proxy set

        [SerializeField]
        InfluenceVolume m_InfluenceVolume;

        [SerializeField]
        FrameSettings m_FrameSettings = null;

        [SerializeField]
        CaptureSettings m_CaptureSettings = new CaptureSettings();

        [SerializeField, FormerlySerializedAsAttribute("dimmer"), FormerlySerializedAsAttribute("m_Dimmer"), FormerlySerializedAsAttribute("multiplier")]
        float m_Multiplier = 1.0f;
        [SerializeField, FormerlySerializedAsAttribute("weight")]
        [Range(0.0f, 1.0f)]
        float m_Weight = 1.0f;

        [SerializeField]
        ReflectionProbeMode m_Mode = ReflectionProbeMode.Baked;
        [SerializeField]
        ReflectionProbeRefreshMode m_RefreshMode = ReflectionProbeRefreshMode.OnAwake;
        
        RenderTexture m_RealtimeTexture = null;
        [SerializeField]
        Texture m_CustomTexture;
        [SerializeField]
        Texture m_BakedTexture;
        [SerializeField]
        bool m_RenderDynamicObjects;

        /// <summary>Light layer to use by this probe.</summary>
        public LightLayerEnum lightLayers = LightLayerEnum.LightLayerDefault;

        // This function return a mask of light layers as uint and handle the case of Everything as being 0xFF and not -1
        public uint GetLightLayers()
        {
            int value = (int)(lightLayers);
            return value < 0 ? (uint)LightLayerEnum.Everything : (uint)value;
        }

        /// <summary>ProxyVolume currently used by this probe.</summary>
        public ReflectionProxyVolumeComponent proxyVolume { get { return m_ProxyVolume; } }

        /// <summary>InfluenceVolume of the probe.</summary>
        public InfluenceVolume influenceVolume { get { return m_InfluenceVolume; } private set { m_InfluenceVolume = value; } }

        /// <summary>Frame settings in use with this probe.</summary>
        public FrameSettings frameSettings { get { return m_FrameSettings; } }

        public CaptureSettings captureSettings { get { return m_CaptureSettings; } }

        /// <summary>Multiplier factor of reflection (non PBR parameter).</summary>
        public float multiplier { get { return m_Multiplier; } set { m_Multiplier = value; } }
        /// <summary>Weight for blending amongst probes (non PBR parameter).</summary>
        public float weight { get { return m_Weight; } set { m_Weight = value; } }
        
        /// <summary>Get the Custom Texture used</summary>
        public virtual Texture customTexture { get { return m_CustomTexture; } set { m_CustomTexture = value; } }
        /// <summary>Get the backed acquired Texture</summary>
        public virtual Texture bakedTexture { get { return m_BakedTexture; } set { m_BakedTexture = value; } }
        /// <summary>Get the realtime acquired Render Texture</summary>
        public RenderTexture realtimeTexture { get { return m_RealtimeTexture; } internal set { m_RealtimeTexture = value; } }
        /// <summary>Get the currently used Texture</summary>
        public Texture currentTexture
        {
            get
            {
                switch (mode)
                {
                    default:
                    case ReflectionProbeMode.Baked:
                        return bakedTexture;
                    case ReflectionProbeMode.Custom:
                        return customTexture;
                    case ReflectionProbeMode.Realtime:
                        return realtimeTexture;
                }
            }
        }
        /// <summary>Render dynamic objects with custom texture as background</summary>
        public bool renderDynamicObjects { get { return m_RenderDynamicObjects; } set { m_RenderDynamicObjects = value; } }

        /// <summary>The capture mode.</summary>
        public virtual ReflectionProbeMode mode
        {
            get { return m_Mode; }
            set { m_Mode = value; }
        }
        /// <summary>Refreshing rate of the capture for Realtime capture mode.</summary>
        public virtual ReflectionProbeRefreshMode refreshMode
        {
            get { return m_RefreshMode; }
            set { m_RefreshMode = value; }
        }

        /// <summary>Is the projection at infinite? Value could be changed by Proxy mode.</summary>
        public bool infiniteProjection
        {
            get
            {
                return (proxyVolume != null && proxyVolume.proxyVolume.shape == ProxyShape.Infinite)
                    || (proxyVolume == null && m_InfiniteProjection);
            }
            set
            {
                m_InfiniteProjection = value;
            }
        }

        internal Matrix4x4 influenceToWorld
        {
            get
            {
                var tr = transform;
                var influencePosition = influenceVolume.GetWorldPosition(tr);
                return Matrix4x4.TRS(
                    influencePosition,
                    tr.rotation,
                    Vector3.one
                    );
            }
        }
        internal Vector3 influenceExtents
        {
            get
            {
                switch (influenceVolume.shape)
                {
                    default:
                    case InfluenceShape.Box:
                        return influenceVolume.boxSize * 0.5f;
                    case InfluenceShape.Sphere:
                        return influenceVolume.sphereRadius * Vector3.one;
                }
            }
        }
        internal Matrix4x4 proxyToWorld
        {
            get
            {
                return proxyVolume != null
                    ? Matrix4x4.TRS(proxyVolume.transform.position, proxyVolume.transform.rotation, Vector3.one)
                    : influenceToWorld;
            }
        }
        public virtual Vector3 proxyExtents
        {
            get
            {
                return proxyVolume != null
                    ? proxyVolume.proxyVolume.extents
                    : influenceExtents;
            }
        }
        internal virtual Vector3 capturePosition
        {
            get
            {
                return transform.position; //at the moment capture position is at probe position
            }
        }

        internal virtual void Awake()
        {
            if (influenceVolume == null)
                influenceVolume = new InfluenceVolume();
            influenceVolume.Init(this);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            influenceVolume.Init(this);
        }

        internal virtual void UpdatedInfluenceVolumeShape(Vector3 size, Vector3 offset) { }
    }
}
