using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract class MRTBufferManager
    {
        protected int m_BufferCount;
        protected RenderTargetIdentifier[] m_RTIDs;
        protected RTHandleSystem.RTHandle[] m_RTs;
        protected int[] m_TextureShaderIDs;

        public int bufferCount { get { return m_BufferCount; } }

        public MRTBufferManager(int maxBufferCount)
        {
            m_BufferCount = maxBufferCount;
            m_RTIDs = new RenderTargetIdentifier[maxBufferCount];
            m_RTs = new RTHandleSystem.RTHandle[maxBufferCount];
            m_TextureShaderIDs = new int[maxBufferCount];
        }

        public RenderTargetIdentifier[] GetBuffersRTI()
        {
            // nameID can change from one frame to another depending on the msaa flag so so we need to update this array to be sure it's up to date.
            for (int i = 0; i < m_BufferCount; ++i)
            {
                m_RTIDs[i] = m_RTs[i].nameID;
            }
            return m_RTIDs;
        }

        public RTHandleSystem.RTHandle GetBuffer(int index)
        {
            Debug.Assert(index < m_BufferCount);
            return m_RTs[index];
        }

        public abstract void CreateBuffers();

        public virtual void BindBufferAsTextures(CommandBuffer cmd)
        {
            for (int i = 0; i < m_BufferCount; ++i)
            {
                cmd.SetGlobalTexture(m_TextureShaderIDs[i], m_RTs[i]);
            }
        }

        virtual public void DestroyBuffers()
        {
            for (int i = 0; i < m_BufferCount; ++i)
            {
                RTHandles.Release(m_RTs[i]);
                m_RTs[i] = null;
            }
        }
    }
}
