using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SurfaceMaterialOptions
    {
        public enum BlendMode
        {
            One,
            Zero,
            SrcColor,
            SrcAlpha,
            DstColor,
            DstAlpha,
            OneMinusSrcColor,
            OneMinusSrcAlpha,
            OneMinusDstColor,
            OneMinusDstAlpha,
        }

        public enum CullMode
        {
            Back,
            Front,
            Off
        }

        public enum ZTest
        {
            Less,
            Greater,
            LEqual,
            GEqual,
            Equal,
            NotEqual,
            Always
        }

        public enum ZWrite
        {
            On,
            Off
        }

        [SerializeField]
        private BlendMode m_SrcBlend = BlendMode.One;

        [SerializeField]
        private BlendMode m_DstBlend = BlendMode.Zero;

        [SerializeField]
        private CullMode m_CullMode = CullMode.Back;

        [SerializeField]
        private ZTest m_ZTest = ZTest.LEqual;

        [SerializeField]
        private ZWrite m_ZWrite = ZWrite.On;

        [SerializeField]
        private int m_LOD = 200;

        public void Init()
        {
            srcBlend = BlendMode.One;
            dstBlend = BlendMode.Zero;
            cullMode = CullMode.Back;
            zTest = ZTest.LEqual;
            zWrite = ZWrite.On;
            lod = 200;
        }

        public void GetBlend(ShaderStringBuilder builder)
        {
            builder.AppendLine("Blend {0} {1}", srcBlend, dstBlend);
        }

        public void GetCull(ShaderStringBuilder builder)
        {
            builder.AppendLine("Cull {0}", cullMode);
        }

        public void GetDepthWrite(ShaderStringBuilder builder)
        {
            builder.AppendLine("ZWrite {0}", zWrite);
        }

        public void GetDepthClip(ShaderStringBuilder builder)
        {
            builder.AppendLine("ZClip [_ZClip]");
        }

        public void GetDepthTest(ShaderStringBuilder builder)
        {
            builder.AppendLine("ZTest {0}", zTest);
        }

        public BlendMode srcBlend { get { return m_SrcBlend; } set { m_SrcBlend = value; } }
        public BlendMode dstBlend { get { return m_DstBlend; } set { m_DstBlend = value; } }
        public CullMode cullMode { get { return m_CullMode; } set { m_CullMode = value; } }
        public ZTest zTest { get { return m_ZTest; } set { m_ZTest = value; } }
        public ZWrite zWrite { get { return m_ZWrite; } set { m_ZWrite = value; } }
        public int lod { get { return m_LOD; } set { m_LOD = value; } }
    }
}
