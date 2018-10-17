using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SurfaceMaterialTags
    {
        public enum RenderType
        {
            Opaque,
            Transparent,
            TransparentCutout,
            Background,
            Overlay
        }

        public enum RenderQueue
        {
            Background,
            Geometry,
            Transparent,
            Overlay,
            AlphaTest
        }

        [SerializeField]
        private RenderType m_RenderType = RenderType.Opaque;

        [SerializeField]
        private RenderQueue m_RenderQueue = RenderQueue.Geometry;

        [SerializeField]
        private int m_RenderQueueOffset = 0;

        public RenderQueue renderQueue { get { return m_RenderQueue; } set { m_RenderQueue = value; } }
        public int renderQueueOffset { get { return m_RenderQueueOffset; } set { m_RenderQueueOffset = value; } }
        public RenderType renderType { get { return m_RenderType; } set { m_RenderType = value; } }

        public void Init()
        {
            renderQueue = RenderQueue.Geometry;
            renderQueueOffset = 0;
            renderType = RenderType.Opaque;
        }

        public void GetTags(ShaderStringBuilder builder)
        {
            builder.AppendLine("Tags");
            using (builder.BlockScope())
            {
                builder.AppendLine(@"""RenderPipeline""=""HDRenderPipeline""");
                builder.AppendLine("\"RenderType\"=\"{0}\"", renderType);

                string seperator = renderQueueOffset >= 0 ? "+" : "";
                builder.AppendLine("\"Queue\"=\"{0}{1}{2}\"", renderQueue, seperator, renderQueueOffset);
            }
        }
    }
}
