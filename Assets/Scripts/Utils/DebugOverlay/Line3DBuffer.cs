using UnityEngine;

public class Line3DBuffer
{
    struct Line3DInstanceData
    {
        public Vector4 start;
        public Vector4 end;
        public Vector4 color;
    }

    public Line3DBuffer()
    {
        Shader shader = Shader.Find("Debug/Line3DShaderProc");
        if (shader == null)
        {
            Debug.LogError("Line3DBuffer cant find shader resource");
        }
        m_Line3DMaterial = new Material(shader);
    }

    public void Shutdown()
    {
        if (m_Line3DInstanceBuffer != null)
            m_Line3DInstanceBuffer.Release();
        m_Line3DInstanceBuffer = null;
        m_Line3DInstanceData = null;
    }

    public void PrepareBuffer()
    {
        if (m_Line3DInstanceBuffer == null || m_Line3DInstanceBuffer.count != m_Line3DInstanceData.Length)
        {
            if (m_Line3DInstanceBuffer != null)
            {
                m_Line3DInstanceBuffer.Release();
                m_Line3DInstanceBuffer = null;
            }

            m_Line3DInstanceBuffer = new ComputeBuffer(m_Line3DInstanceData.Length, 16 + 16 + 16);
            m_Line3DMaterial.SetBuffer("positionBuffer", m_Line3DInstanceBuffer);
        }

        m_Line3DInstanceBuffer.SetData(m_Line3DInstanceData, 0, 0, m_NumLine3DsUsed);
        m_NumLine3DsToDraw = m_NumLine3DsUsed;
        m_NumLine3DsUsed = 0;

        //  m_Line3DMaterial.SetVector("scales", new Vector4(1.0f / width, 1.0f / height, 1.0f / 1280.0f, 1.0f / 720.0f));
    }

    public void Draw()
    {
        m_Line3DMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, m_NumLine3DsToDraw * 6, 1);
    }

    public void HDDraw(UnityEngine.Rendering.CommandBuffer cmd)
    {
        cmd.DrawProcedural(Matrix4x4.identity, m_Line3DMaterial, 0, MeshTopology.Triangles, m_NumLine3DsToDraw * 6, 1);
    }


    unsafe public void AddLine3D(Vector3 start, Vector3 end, Vector4 col)
    {
        if (m_NumLine3DsUsed >= m_Line3DInstanceData.Length)
        {
            // Resize
            var newBuf = new Line3DInstanceData[m_Line3DInstanceData.Length + 128];
            System.Array.Copy(m_Line3DInstanceData, newBuf, m_Line3DInstanceData.Length);
            m_Line3DInstanceData = newBuf;
        }
        fixed (Line3DInstanceData* d = &m_Line3DInstanceData[m_NumLine3DsUsed])
        {
            d->color = col;
            d->start = start;
            d->start.w = 1.0f;
            d->end = end;
            d->end.w = 1.0f;
        }
        m_NumLine3DsUsed++;
    }

    ComputeBuffer m_Line3DInstanceBuffer;
    int m_NumLine3DsUsed = 0;
    int m_NumLine3DsToDraw = 0;
    Line3DInstanceData[] m_Line3DInstanceData = new Line3DInstanceData[128];
    Material m_Line3DMaterial;
}
