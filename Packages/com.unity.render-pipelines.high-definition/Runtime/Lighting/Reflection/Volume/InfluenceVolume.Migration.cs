using System;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class InfluenceVolume : IVersionable<InfluenceVolume.Version>, ISerializationCallbackReceiver
    {
        enum Version
        {
            Initial,
            SphereOffset
        }

        static readonly MigrationDescription<Version, InfluenceVolume> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.SphereOffset, (InfluenceVolume i) =>
            {
                if (i.shape == InfluenceShape.Sphere)
                {
#pragma warning disable 618
                    i.m_Offset = i.m_ObsoleteSphereBaseOffset;
#pragma warning restore 618
                }
            })
        );

        [SerializeField]
        Version m_Version;
        Version IVersionable<Version>.version
        {
            get
            {
                return (Version)m_Version;
            }
            set
            {
                m_Version = value;
            }
        }

        // Obsolete fields
#pragma warning disable 649 //never assigned
        [SerializeField, FormerlySerializedAs("m_SphereBaseOffset"), Obsolete("For Data Migration")]
        Vector3 m_ObsoleteSphereBaseOffset;
#pragma warning restore 649 //never assigned


        //as there is only internal change, keep it at deserialization time
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize()
        {
            k_Migration.Migrate(this);
        }
    }
}
