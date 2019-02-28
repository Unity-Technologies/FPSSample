#if UNITY_EDITOR //formerly migration were only handled in editor for this asset
using System;
using UnityEngine.Serialization;

using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class RenderPipelineResources : ScriptableObject, IVersionable<RenderPipelineResources.Version>
    {
        enum Version
        {
            None,
            First,
            RemovedEditorOnlyResources = 4
        }

        static readonly MigrationDescription<Version, RenderPipelineResources> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.RemovedEditorOnlyResources, (RenderPipelineResources i) =>
            {
                i.Init();
            })
        );

        [HideInInspector, SerializeField, FormerlySerializedAs("version")]
        Version m_Version = Version.First;  //keep former creation affectation
        Version IVersionable<Version>.version { get { return (Version)m_Version; } set { m_Version = value; } }

        public void UpgradeIfNeeded()
        {
            k_Migration.Migrate(this);
        }
    }
}
#endif
