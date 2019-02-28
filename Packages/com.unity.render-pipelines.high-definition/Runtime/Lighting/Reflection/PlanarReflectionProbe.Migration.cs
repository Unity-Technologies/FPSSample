using UnityEngine.Serialization;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public sealed partial class PlanarReflectionProbe : IVersionable<PlanarReflectionProbe.Version>
    {
        enum Version
        {
            Initial,
            MigrateSphereOffset = 2, //first iteration where we actually migrate data
            CaptureSettings
        }

        static readonly MigrationDescription<Version, PlanarReflectionProbe> k_Migration = MigrationDescription.New(
            MigrationStep.New(Version.CaptureSettings, (PlanarReflectionProbe p) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                if (p.m_ObsoleteOverrideFieldOfView)
                {
                    p.captureSettings.overrides |= CaptureSettingsOverrides.FieldOfview;
                }
                p.captureSettings.fieldOfView = p.m_ObsoleteFieldOfViewOverride;
                p.captureSettings.nearClipPlane = p.m_ObsoleteCaptureNearPlane;
                p.captureSettings.farClipPlane = p.m_ObsoleteCaptureFarPlane;
#pragma warning restore 618 // Type or member is obsolete
                //not used for planar, keep it clean
                p.influenceVolume.boxBlendNormalDistanceNegative = Vector3.zero;
                p.influenceVolume.boxBlendNormalDistancePositive = Vector3.zero;
            })
        );

        //make the version name explicite to deal with inheritance
        [SerializeField, FormerlySerializedAs("version"), FormerlySerializedAs("m_Version")]
        int m_PlanarProbeVersion;
        Version IVersionable<Version>.version
        {
            get
            {
                return (Version)m_PlanarProbeVersion;
            }
            set
            {
                m_PlanarProbeVersion = (int)value;
            }
        }

        #region Deprecated Fields
#pragma warning disable 649 //never assigned
        [SerializeField, Obsolete("keeped only for data migration")]
        bool m_ObsoleteOverrideFieldOfView;
        [SerializeField, Obsolete("keeped only for data migration")]
        float m_ObsoleteFieldOfViewOverride = CaptureSettings.@default.fieldOfView;
        [SerializeField, Obsolete("keeped only for data migration")]
        float m_ObsoleteCaptureNearPlane = CaptureSettings.@default.nearClipPlane;
        [SerializeField, Obsolete("keeped only for data migration")]
        float m_ObsoleteCaptureFarPlane = CaptureSettings.@default.farClipPlane;
#pragma warning restore 649 //never assigned
        #endregion
    }
}
