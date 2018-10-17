namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ReflectionProxyVolumeComponent : MonoBehaviour, ISerializationCallbackReceiver
    {
        enum Version
        {
            First,
            IncludeInfiniteInShape,
            // Add new version here and they will automatically be the Current one
            Max,
            Current = Max - 1
        }

        [SerializeField]
        int m_Version;

        [SerializeField]
        ProxyVolume m_ProxyVolume = new ProxyVolume();

        /// <summary>Access to proxy volume parameters</summary>
        public ProxyVolume proxyVolume { get { return m_ProxyVolume; } }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_Version != (int)Version.Current)
            {
                // Add here data migration code
                if (m_Version < (int)Version.IncludeInfiniteInShape)
                {
                    proxyVolume.MigrateInfiniteProhjectionInShape();
                }
                m_Version = (int)Version.Current;
            }
        }
    }
}
