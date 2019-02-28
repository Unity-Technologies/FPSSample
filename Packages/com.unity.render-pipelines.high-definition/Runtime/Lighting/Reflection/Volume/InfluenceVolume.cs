using System;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public partial class InfluenceVolume
    {
        HDProbe m_Probe;

        [SerializeField, FormerlySerializedAs("m_ShapeType")]
        InfluenceShape m_Shape = InfluenceShape.Box;
        [SerializeField, FormerlySerializedAs("m_BoxBaseOffset")]
        Vector3 m_Offset;

        // Box
        [SerializeField, FormerlySerializedAs("m_BoxBaseSize")]
        Vector3 m_BoxSize = Vector3.one * 10;
        [SerializeField, FormerlySerializedAs("m_BoxInfluencePositiveFade")]
        Vector3 m_BoxBlendDistancePositive;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNegativeFade")]
        Vector3 m_BoxBlendDistanceNegative;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNormalPositiveFade")]
        Vector3 m_BoxBlendNormalDistancePositive;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNormalNegativeFade")]
        Vector3 m_BoxBlendNormalDistanceNegative;
        [SerializeField, FormerlySerializedAs("m_BoxPositiveFaceFade")]
        Vector3 m_BoxSideFadePositive = Vector3.one;
        [SerializeField, FormerlySerializedAs("m_BoxNegativeFaceFade")]
        Vector3 m_BoxSideFadeNegative = Vector3.one;

        //editor value that need to be saved for easy passing from simplified to advanced and vice et versa
        // /!\ must not be used outside editor code
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendDistancePositive")]
        Vector3 m_EditorAdvancedModeBlendDistancePositive;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendDistanceNegative")]
        Vector3 m_EditorAdvancedModeBlendDistanceNegative;
        [SerializeField, FormerlySerializedAs("editorSimplifiedModeBlendDistance")]
        float m_EditorSimplifiedModeBlendDistance;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendNormalDistancePositive")]
        Vector3 m_EditorAdvancedModeBlendNormalDistancePositive;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendNormalDistanceNegative")]
        Vector3 m_EditorAdvancedModeBlendNormalDistanceNegative;
        [SerializeField, FormerlySerializedAs("editorSimplifiedModeBlendNormalDistance")]
        float m_EditorSimplifiedModeBlendNormalDistance;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeEnabled")]
        bool m_EditorAdvancedModeEnabled;
        [SerializeField]
        Vector3 m_EditorAdvancedModeFaceFadePositive = Vector3.one;
        [SerializeField]
        Vector3 m_EditorAdvancedModeFaceFadeNegative = Vector3.one;

        // Sphere
        [SerializeField, FormerlySerializedAs("m_SphereBaseRadius")]
        float m_SphereRadius = 3f;
        [SerializeField, FormerlySerializedAs("m_SphereInfluenceFade")]
        float m_SphereBlendDistance;
        [SerializeField, FormerlySerializedAs("m_SphereInfluenceNormalFade")]
        float m_SphereBlendNormalDistance;

        internal void CopyTo(InfluenceVolume data)
        {
            //keep the m_Probe as it is used to reset the probe

            data.m_Shape = m_Shape;
            data.m_Offset = m_Offset;
            data.m_BoxSize = m_BoxSize;
            data.m_BoxBlendDistancePositive = m_BoxBlendDistancePositive;
            data.m_BoxBlendDistanceNegative = m_BoxBlendDistanceNegative;
            data.m_BoxBlendNormalDistancePositive = m_BoxBlendNormalDistancePositive;
            data.m_BoxBlendNormalDistanceNegative = m_BoxBlendNormalDistanceNegative;
            data.m_BoxSideFadePositive = m_BoxSideFadePositive;
            data.m_BoxSideFadeNegative = m_BoxSideFadeNegative;
            data.m_SphereRadius = m_SphereRadius;
            data.m_SphereBlendDistance = m_SphereBlendDistance;
            data.m_SphereBlendNormalDistance = m_SphereBlendNormalDistance;

            data.m_EditorAdvancedModeBlendDistancePositive = m_EditorAdvancedModeBlendDistancePositive;
            data.m_EditorAdvancedModeBlendDistanceNegative = m_EditorAdvancedModeBlendDistanceNegative;
            data.m_EditorSimplifiedModeBlendDistance = m_EditorSimplifiedModeBlendDistance;
            data.m_EditorAdvancedModeBlendNormalDistancePositive = m_EditorAdvancedModeBlendNormalDistancePositive;
            data.m_EditorAdvancedModeBlendNormalDistanceNegative = m_EditorAdvancedModeBlendNormalDistanceNegative;
            data.m_EditorSimplifiedModeBlendNormalDistance = m_EditorSimplifiedModeBlendNormalDistance;
            data.m_EditorAdvancedModeEnabled = m_EditorAdvancedModeEnabled;
            data.m_EditorAdvancedModeFaceFadePositive = m_EditorAdvancedModeFaceFadePositive;
            data.m_EditorAdvancedModeFaceFadeNegative = m_EditorAdvancedModeFaceFadeNegative;
        }

        /// <summary>Shape of this InfluenceVolume.</summary>
        public InfluenceShape shape
        {
            get { return m_Shape; }
            set
            {
                m_Shape = value;
                switch (m_Shape)
                {
                    case InfluenceShape.Box:
                        m_Probe.UpdatedInfluenceVolumeShape(m_BoxSize, offset);
                        break;
                    case InfluenceShape.Sphere:
                        m_Probe.UpdatedInfluenceVolumeShape(Vector3.one * (2 * m_SphereRadius), offset);
                        break;
                }
            }
        }

        /// <summary>Offset of this influence volume to the component handling him.</summary>
        public Vector3 offset
        {
            get { return m_Offset; }
            set
            {
                m_Offset = value;
                m_Probe.UpdatedInfluenceVolumeShape(boxSize, m_Offset);
            }
        }

        /// <summary>Size of the InfluenceVolume in Box Mode.</summary>
        public Vector3 boxSize
        {
            get { return m_BoxSize; }
            set
            {
                m_BoxSize = value;
                m_Probe.UpdatedInfluenceVolumeShape(m_BoxSize, offset);
            }
        }

        /// <summary>Offset of sub volume defining fading.</summary>
        public Vector3 boxBlendOffset { get { return (boxBlendDistanceNegative - boxBlendDistancePositive) * 0.5f; } }
        /// <summary>Size of sub volume defining fading.</summary>
        public Vector3 boxBlendSize { get { return -(boxBlendDistancePositive + boxBlendDistanceNegative); } }
        /// <summary>
        /// Position of fade sub volume maxOffset point relative to InfluenceVolume max corner.
        /// Values between 0 (on InfluenceVolume hull) to half of boxSize corresponding axis.
        /// </summary>
        public Vector3 boxBlendDistancePositive { get { return m_BoxBlendDistancePositive; } set { m_BoxBlendDistancePositive = value; } }
        /// <summary>
        /// Position of fade sub volume minOffset point relative to InfluenceVolume min corner.
        /// Values between 0 (on InfluenceVolume hull) to half of boxSize corresponding axis.
        /// </summary>
        public Vector3 boxBlendDistanceNegative { get { return m_BoxBlendDistanceNegative; } set { m_BoxBlendDistanceNegative = value; } }

        /// <summary>Offset of sub volume defining fading relative to normal orientation.</summary>
        public Vector3 boxBlendNormalOffset { get { return (boxBlendNormalDistanceNegative - boxBlendNormalDistancePositive) * 0.5f; } }
        /// <summary>Size of sub volume defining fading relative to normal orientation.</summary>
        public Vector3 boxBlendNormalSize { get { return -(boxBlendNormalDistancePositive + boxBlendNormalDistanceNegative); } }
        /// <summary>
        /// Position of normal fade sub volume maxOffset point relative to InfluenceVolume max corner.
        /// Values between 0 (on InfluenceVolume hull) to half of boxSize corresponding axis (on origin for this axis).
        /// </summary>
        public Vector3 boxBlendNormalDistancePositive { get { return m_BoxBlendNormalDistancePositive; } set { m_BoxBlendNormalDistancePositive = value; } }
        /// <summary>
        /// Position of normal fade sub volume minOffset point relative to InfluenceVolume min corner.
        /// Values between 0 (on InfluenceVolume hull) to half of boxSize corresponding axis (on origin for this axis).
        /// </summary>
        public Vector3 boxBlendNormalDistanceNegative { get { return m_BoxBlendNormalDistanceNegative; } set { m_BoxBlendNormalDistanceNegative = value; } }

        /// <summary>Define fading percent of +X, +Y and +Z locally oriented face. (values from 0 to 1)</summary>
        public Vector3 boxSideFadePositive { get { return m_BoxSideFadePositive; } set { m_BoxSideFadePositive = value; } }
        /// <summary>Define fading percent of -X, -Y and -Z locally oriented face. (values from 0 to 1)</summary>
        public Vector3 boxSideFadeNegative { get { return m_BoxSideFadeNegative; } set { m_BoxSideFadeNegative = value; } }


        /// <summary>Radius of the InfluenceVolume in Sphere Mode.</summary>
        public float sphereRadius
        {
            get { return m_SphereRadius; }
            set
            {
                m_SphereRadius = value;
                m_Probe.UpdatedInfluenceVolumeShape(Vector3.one * (2 * m_SphereRadius), offset);
            }
        }
        /// <summary>
        /// Offset of the fade sub volume from InfluenceVolume hull.
        /// Value between 0 (on InfluenceVolume hull) and sphereRadius (fade sub volume reduced to a point).
        /// </summary>
        public float sphereBlendDistance { get { return m_SphereBlendDistance; } set { m_SphereBlendDistance = value; } }
        /// <summary>
        /// Offset of the normal fade sub volume from InfluenceVolume hull.
        /// Value between 0 (on InfluenceVolume hull) and sphereRadius (fade sub volume reduced to a point).
        /// </summary>
        public float sphereBlendNormalDistance { get { return m_SphereBlendNormalDistance; } set { m_SphereBlendNormalDistance = value; } }

        internal void Init(HDProbe probe)
        {
            this.m_Probe = probe;
        }

        internal BoundingSphere GetBoundingSphereAt(Transform probeTransform)
        {
            switch (shape)
            {
                default:
                case InfluenceShape.Sphere:
                    return new BoundingSphere(probeTransform.TransformPoint(offset), sphereRadius);
                case InfluenceShape.Box:
                {
                    var position = probeTransform.TransformPoint(offset);
                    var radius = Mathf.Max(boxSize.x, Mathf.Max(boxSize.y, boxSize.z));
                    return new BoundingSphere(position, radius);
                }
            }
        }

        internal Bounds GetBoundsAt(Transform probeTransform)
        {
            switch (shape)
            {
                default:
                case InfluenceShape.Sphere:
                    return new Bounds(probeTransform.position, Vector3.one * sphereRadius);
                case InfluenceShape.Box:
                {
                    var position = probeTransform.TransformPoint(offset);
                    return new Bounds(position, boxSize);
                }
            }
        }

        internal Vector3 GetWorldPosition(Transform probeTransform)
        {
            return probeTransform.TransformPoint(offset);
        }

        internal Vector3 extends
        {
            get
            {
                switch (shape)
                {
                    default:
                    case InfluenceShape.Box:
                        return boxSize * 0.5f;
                    case InfluenceShape.Sphere:
                        return sphereRadius * Vector3.one;
                }
            }
        }

        internal EnvShapeType envShape
        {
            get
            {
                switch (shape)
                {
                    default:
                    case InfluenceShape.Box:
                        return EnvShapeType.Box;
                    case InfluenceShape.Sphere:
                        return EnvShapeType.Sphere;
                }
            }
        }
    }
}
