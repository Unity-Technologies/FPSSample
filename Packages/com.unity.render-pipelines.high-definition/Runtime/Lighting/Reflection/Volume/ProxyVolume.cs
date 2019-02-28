using System;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class ProxyVolume
    {
        [SerializeField, FormerlySerializedAs("m_ShapeType")]
        ProxyShape m_Shape = ProxyShape.Box;

        // Box
        [SerializeField]
        Vector3 m_BoxSize = Vector3.one;
        [SerializeField, Obsolete("Kept only for compatibility. Use m_Shape instead")]
        bool m_BoxInfiniteProjection = false;

        // Sphere
        [SerializeField]
        float m_SphereRadius = 1;
        [SerializeField, Obsolete("Kept only for compatibility. Use m_Shape instead")]
        bool m_SphereInfiniteProjection = false;

        /// <summary>The shape of the proxy</summary>
        public ProxyShape shape { get { return m_Shape; } private set { m_Shape = value; } }

        /// <summary>The size of the proxy if it as a shape Box</summary>
        public Vector3 boxSize { get { return m_BoxSize; } set { m_BoxSize = value; } }

        /// <summary>The radius of the proxy if it as a shape Sphere</summary>
        public float sphereRadius { get { return m_SphereRadius; } set { m_SphereRadius = value; } }


        internal Vector3 extents
        {
            get
            {
                switch (shape)
                {
                    case ProxyShape.Box: return m_BoxSize * 0.5f;
                    case ProxyShape.Sphere: return Vector3.one * m_SphereRadius;
                    default: return Vector3.one;
                }
            }
        }

        internal void MigrateInfiniteProhjectionInShape()
        {
#pragma warning disable 618 // Type or member is obsolete
            if (shape == ProxyShape.Sphere && m_SphereInfiniteProjection
                || shape == ProxyShape.Box && m_BoxInfiniteProjection)
#pragma warning restore 618 // Type or member is obsolete
            {
                shape = ProxyShape.Infinite;
            }
        }
    }
}
