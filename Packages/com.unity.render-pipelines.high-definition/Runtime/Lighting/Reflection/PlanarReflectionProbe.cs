using UnityEngine.Serialization;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteAlways]
    public class PlanarReflectionProbe : HDProbe, ISerializationCallbackReceiver
    {
        enum Version
        {
            First,
            Second,
            MigrateOffsetSphere,
            MigrateCaptureSettings,
            // Add new version here and they will automatically be the Current one
            Max,
            Current = Max - 1
        }

        [SerializeField, FormerlySerializedAs("version")]
        uint m_Version;

        public enum CapturePositionMode
        {
            Static,
            MirrorCamera,
        }

        [SerializeField]
        Vector3 m_CaptureLocalPosition;
        [SerializeField]
        CapturePositionMode m_CapturePositionMode = CapturePositionMode.Static;
        [SerializeField]
        Vector3 m_CaptureMirrorPlaneLocalPosition;
        [SerializeField]
        Vector3 m_CaptureMirrorPlaneLocalNormal = Vector3.up;

#pragma warning disable 649 //never assigned
        [SerializeField, Obsolete("keeped only for data migration")]
        bool m_OverrideFieldOfView;
        [SerializeField, Obsolete("keeped only for data migration")]
        float m_FieldOfViewOverride = CaptureSettings.@default.fieldOfView;
        [SerializeField, Obsolete("keeped only for data migration")]
        float m_CaptureNearPlane = CaptureSettings.@default.nearClipPlane;
        [SerializeField, Obsolete("keeped only for data migration")]
        float m_CaptureFarPlane = CaptureSettings.@default.farClipPlane;
#pragma warning restore 649 //never assigned

        public BoundingSphere boundingSphere { get { return influenceVolume.GetBoundingSphereAt(transform); } }
        public Bounds bounds { get { return influenceVolume.GetBoundsAt(transform); } }
        public Vector3 captureLocalPosition { get { return m_CaptureLocalPosition; } set { m_CaptureLocalPosition = value; } }
        public CapturePositionMode capturePositionMode { get { return m_CapturePositionMode; } }
        public Vector3 captureMirrorPlaneLocalPosition
        {
            get { return m_CaptureMirrorPlaneLocalPosition; }
            set { m_CaptureMirrorPlaneLocalPosition = value; }
        }
        public Vector3 captureMirrorPlanePosition { get { return transform.TransformPoint(m_CaptureMirrorPlaneLocalPosition); } }
        public Vector3 captureMirrorPlaneLocalNormal
        {
            get { return m_CaptureMirrorPlaneLocalNormal; }
            set { m_CaptureMirrorPlaneLocalNormal = value; }
        }
        public Vector3 captureMirrorPlaneNormal { get { return transform.TransformDirection(m_CaptureMirrorPlaneLocalNormal); } }
        internal override Vector3 capturePosition
        {
            get
            {
                return transform.TransformPoint(captureLocalPosition);
            }
        }

        public bool useMirrorPlane
        {
            get
            {
                return mode == ReflectionProbeMode.Realtime
                    && refreshMode == ReflectionProbeRefreshMode.EveryFrame
                    && capturePositionMode == CapturePositionMode.MirrorCamera;
            }
        }

        //for strange reason, current ReflectionSystem needs a proxyExtents two time bigger for planar. To be fixed when refactoring the ReflectionSystem
        public override Vector3 proxyExtents
        {
            get
            {
                return proxyVolume != null
                    ? proxyVolume.proxyVolume.extents
                    : influenceVolume.boxSize;
            }
        }

        public void RequestRealtimeRender()
        {
            if (isActiveAndEnabled)
                ReflectionSystem.RequestRealtimeRender(this);
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

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Assert.IsNotNull(influenceVolume, "influenceVolume must have an instance at this point. See HDProbe.Awake()");
            if (m_Version != (uint)Version.Current)
            {
                // Add here data migration code
                if(m_Version < (uint)Version.MigrateOffsetSphere)
                {
                    influenceVolume.MigrateOffsetSphere();
                    //not used for planar, keep it clean
                    influenceVolume.boxBlendNormalDistanceNegative = Vector3.zero;
                    influenceVolume.boxBlendNormalDistancePositive = Vector3.zero;
                    m_Version = (uint)Version.MigrateOffsetSphere;
                }
                if(m_Version < (uint)Version.MigrateCaptureSettings)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    if (m_OverrideFieldOfView)
                    {
                        captureSettings.overrides |= CaptureSettingsOverrides.FieldOfview;
                    }
                    captureSettings.fieldOfView = m_FieldOfViewOverride;
                    captureSettings.nearClipPlane = m_CaptureNearPlane;
                    captureSettings.farClipPlane = m_CaptureFarPlane;
#pragma warning restore CS0618 // Type or member is obsolete
                    m_Version = (uint)Version.MigrateCaptureSettings;
                }
            }

        }
    }
}
