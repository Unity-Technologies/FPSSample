using UnityEngine;
using System;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugOverlay : MonoBehaviour
{
    [Header("Overlay size")]
    [SerializeField]
    int width = 80;
    [SerializeField]
    int height = 25;

    [Header("Font material info")]
    public Material instanceMaterialProc;
    [Tooltip("Number of columns of glyphs on texture")]
    public int charCols = 30;
    [Tooltip("Number of rows of glyphs on texture")]
    public int charRows = 16;
    [Tooltip("Width in pixels of each glyph")]
    public int cellWidth = 32;
    [Tooltip("Height in pixels of each glyph")]
    public int cellHeight = 32;

    public Shader lineShaderProc;

    public static DebugOverlay instance;

    public static int Width { get { return instance.width; } }
    public static int Height { get { return instance.height; } }

    void Awake()
    {
        m_LineMaterial = new Material(lineShaderProc);
        m_line3DBuffer = new Line3DBuffer();

#if UNITY_EDITOR
        Camera[] sceneCameras = UnityEditor.SceneView.GetAllSceneCameras();
        foreach(var camera in sceneCameras)
        {
            camera.gameObject.AddComponent<DebugOverlayCamera>();
        }
#endif
    }

    public void Init()
    {
        instance = this;
    }

    public void Shutdown()
    {
        if (m_QuadInstanceBuffer != null)
            m_QuadInstanceBuffer.Release();
        m_QuadInstanceBuffer = null;
        m_QuadInstanceData = null;

        if (m_LineInstanceBuffer != null)
            m_LineInstanceBuffer.Release();
        m_LineInstanceBuffer = null;
        m_LineInstanceData = null;

        m_line3DBuffer.Shutdown();
        m_line3DBuffer = null;

        instance = null;
    }

    public void TickLateUpdate()
    {
        // Recreate compute buffer if needed.
        if (m_QuadInstanceBuffer == null || m_QuadInstanceBuffer.count != m_QuadInstanceData.Length)
        {
            if (m_QuadInstanceBuffer != null)
            {
                m_QuadInstanceBuffer.Release();
                m_QuadInstanceBuffer = null;
            }

            m_QuadInstanceBuffer = new ComputeBuffer(m_QuadInstanceData.Length, 16 + 16 + 16);
            instanceMaterialProc.SetBuffer("positionBuffer", m_QuadInstanceBuffer);
        }

        if (m_LineInstanceBuffer == null || m_LineInstanceBuffer.count != m_LineInstanceData.Length)
        {
            if (m_LineInstanceBuffer != null)
            {
                m_LineInstanceBuffer.Release();
                m_LineInstanceBuffer = null;
            }

            m_LineInstanceBuffer = new ComputeBuffer(m_LineInstanceData.Length, 16 + 16);
            m_LineMaterial.SetBuffer("positionBuffer", m_LineInstanceBuffer);
        }

        m_line3DBuffer.PrepareBuffer();

        m_QuadInstanceBuffer.SetData(m_QuadInstanceData, 0, 0, m_NumQuadsUsed);
        m_NumQuadsToDraw = m_NumQuadsUsed;

        m_LineInstanceBuffer.SetData(m_LineInstanceData, 0, 0, m_NumLinesUsed);
        m_NumLinesToDraw = m_NumLinesUsed;

        instanceMaterialProc.SetVector("scales", new Vector4(
            1.0f / width,
            1.0f / height,
            (float)cellWidth / instanceMaterialProc.mainTexture.width,
            (float)cellHeight / instanceMaterialProc.mainTexture.height));

        m_LineMaterial.SetVector("scales", new Vector4(1.0f / width, 1.0f / height, 1.0f / 1280.0f, 1.0f / 720.0f));

        _Clear();
    }

    /// <summary>
    /// Set color of text. 
    /// </summary>
    /// <param name="col"></param>
    public static void SetColor(Color col)
    {
        if (instance == null)
            return;
        instance.m_CurrentColor = col;
    }

    public static void SetOrigin(float x, float y)
    {
        if (instance == null)
            return;
        instance.m_OriginX = x;
        instance.m_OriginY = y;
    }

    // TODO (petera) Reconsider decision to go 'virtual character' coordinate system
    // Write in raw pixel
    public static void WriteAbsolute(float x, float y, float size, char[] buf, int count)
    {
        if (instance == null)
            return;
        float scalex = (float)instance.width / Screen.width;
        float scaley = (float)instance.height / Screen.height;
        x *= scalex;
        y *= scaley;
        float sizex = scalex * size;
        float sizey = scaley * size * 1.5f;
        //instance.width
        for (var i = 0; i < count; i++)
            instance.AddQuad(x + i*sizex, y, sizex, sizey, buf[i], instance.m_CurrentColor);
    }
    public static void AddQuadAbsolute(float x, float y, float width, float height, char c, Vector4 col)
    {
        if (instance == null)
            return;
        float scalex = (float)instance.width / Screen.width;
        float scaley = (float)instance.height / Screen.height;
        x *= scalex;
        y *= scaley;
        width *= scalex;
        height *= scaley;
        instance.AddQuad(x, y, width, height, c, col);
    }

    public static void Write(float x, float y, string format)
    {
        if (instance == null)
            return;

        var l = StringFormatter.Write(ref _buf, 0, format);
        instance._DrawText(x, y, ref _buf, l);
    }
    public static void Write<T>(float x, float y, string format, T arg)
    {
        if (instance == null)
            return;
        var l = StringFormatter.Write(ref _buf, 0, format, arg);
        instance._DrawText(x, y, ref _buf, l);
    }

    public static void Write(Color col, float x, float y, string format)
    {
        if (instance == null)
            return;
        Color c = instance.m_CurrentColor;
        instance.m_CurrentColor = col;
        var l = StringFormatter.Write(ref _buf, 0, format);
        instance._DrawText(x, y, ref _buf, l);
        instance.m_CurrentColor = c;
    }
    
    
    public static void Write<T>(Color col, float x, float y, string format, T arg)
    {
        if (instance == null)
            return;
        Color c = instance.m_CurrentColor;
        instance.m_CurrentColor = col;
        var l = StringFormatter.Write(ref _buf, 0, format, arg);
        instance._DrawText(x, y, ref _buf, l);
        instance.m_CurrentColor = c;
    }
    public static void Write<T0,T1>(Color col, float x, float y, string format, T0 arg0, T1 arg1)
    {
        if (instance == null)
            return;
        Color c = instance.m_CurrentColor;
        instance.m_CurrentColor = col;
        var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1);
        instance._DrawText(x, y, ref _buf, l);
        instance.m_CurrentColor = c;
    }
    public static void Write<T0, T1>(float x, float y, string format, T0 arg0, T1 arg1)
    {
        if (instance == null)
            return;
        var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1);
        instance._DrawText(x, y, ref _buf, l);
    }
    public static void Write<T0, T1, T2>(float x, float y, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        if (instance == null)
            return;
        var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1, arg2);
        instance._DrawText(x, y, ref _buf, l);
    }

    public static void Write<T0,T1,T2>(Color col, float x, float y, string format, T0 arg0, T1 arg1, T2 arg2)
    {
        if (instance == null)
            return;
        Color c = instance.m_CurrentColor;
        instance.m_CurrentColor = col;
        var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1, arg2);
        instance._DrawText(x, y, ref _buf, l);
        instance.m_CurrentColor = c;
    }

    public static void Write<T0, T1, T2, T3>(float x, float y, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
    {
        if (instance == null)
            return;
        var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1, arg2, arg3);
        instance._DrawText(x, y, ref _buf, l);
    }

    public static void Write<T0, T1, T2, T3, T4>(float x, float y, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (instance == null)
            return;
        var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1, arg2, arg3, arg4);
        instance._DrawText(x, y, ref _buf, l);
    }

    // Draw a histogram of one set of data. Data must contain non-negative datapoints.
    public static void DrawHist(float x, float y, float w, float h, float[] data, int startSample, Color color, float maxRange = -1.0f)
    {
        if (instance == null)
            return;
        s_TempData[0] = data;
        s_TempColors[0] = color;
        instance._DrawHist(x, y, w, h, s_TempData, startSample, s_TempColors, maxRange);
        s_TempData[0] = null;
    }
    static float[][] s_TempData = new float[1][];
    static Color[] s_TempColors = new Color[1];

    // Draw a stacked histogram multiple sets of data. Data must contain non-negative datapoints.
    public static void DrawHist(float x, float y, float w, float h, float[][] data, int startSample, Color[] color, float maxRange = -1.0f)
    {
        if (instance == null)
            return;
        instance._DrawHist(x, y, w, h, data, startSample, color, maxRange);
    }

    public static void DrawGraph(float x, float y, float w, float h, float[] data, int startSample, Color color, float maxRange = -1.0f)
    {
        if (instance == null)
            return;
        s_TempData[0] = data;
        s_TempColors[0] = color;
        instance._DrawGraph(x, y, w, h, s_TempData, startSample, s_TempColors, maxRange);
    }

    public static void DrawGraph(float x, float y, float w, float h, float[][] data, int startSample, Color[] color, float maxRange = -1.0f)
    {
        if (instance == null)
            return;
        instance._DrawGraph(x, y, w, h, data, startSample, color, maxRange);
    }

    public static void DrawRect(float x, float y, float w, float h, Color col)
    {
        if (instance == null)
            return;
        instance._DrawRect(x, y, w, h, col);
    }

    public static void DrawLine(float x1, float y1, float x2, float y2, Color col)
    {
        if (instance == null)
            return;
        instance.AddLine(x1, y1, x2, y2, col);
    }

    public static void DrawLine3D(Vector3 start, Vector3 end, Color col)
    {
        if (instance == null)
            return;
        instance.m_line3DBuffer.AddLine3D(start, end, col);
    }

    public static Line3DBuffer GetLine3DBuffer()
    {
        if (instance == null)
            return null;
        return instance.m_line3DBuffer;
    }

    void _DrawText(float x, float y, ref char[] text, int length)
    {
        const string hexes = "0123456789ABCDEF";
        Vector4 col = m_CurrentColor;
        int xpos = 0;
        if (x < 0) x += Width;
        if (y < 0) y += Height;
        for (var i = 0; i < length; i++)
        {
            if (text[i] == '^' && i < length - 3)
            {
                var r = hexes.IndexOf(text[i + 1]);
                var g = hexes.IndexOf(text[i + 2]);
                var b = hexes.IndexOf(text[i + 3]);
                col.x = (float)(r * 16 + r) / 255.0f;
                col.y = (float)(g * 16 + g) / 255.0f;
                col.z = (float)(b * 16 + b) / 255.0f;
                i += 3;
                continue;
            }
            AddQuad(m_OriginX + x + xpos, m_OriginY + y, 1, 1, text[i], col);
            xpos++;
        }
    }

    void _DrawGraph(float x, float y, float w, float h, float[][] data, int startSample, Color[] color, float maxRange = -1.0f)
    {
        if(data == null || data.Length == 0 || data[0] == null)
            throw new System.ArgumentException("Invalid data argument (data must contain at least one non null array");

        var numSamples = data[0].Length;
        for(int i = 1; i < data.Length; ++i)
        {
            if(data[i] == null || data[i].Length != numSamples)
                throw new System.ArgumentException("Length of data of all arrays must be the same");
        }

        if (color.Length != data.Length)
            throw new System.ArgumentException("Length of colors must match number of datasets");

        float maxData = float.MinValue;

        foreach (var dataset in data)
        {
            for (var i = 0; i < numSamples; i++)
            {
                if (dataset[i] > maxData)
                    maxData = dataset[i];
            }
        }

        if (maxData > maxRange)
            maxRange = maxData;

        float dx = w / numSamples;
        float scale = maxRange > 0 ? h / maxRange : 1.0f;

        for (var j = 0; j < data.Length; j++)
        {
            float old_pos_x = 0;
            float old_pos_y = 0;
            Vector4 col = color[j];
            for (var i = 0; i < numSamples; i++)
            {
                float d = data[j][(i + startSample) % numSamples];
                var pos_x = m_OriginX + x + dx * i;
                var pos_y = m_OriginY + y + h - d * scale;
                if (i > 0)
                    AddLine(old_pos_x, old_pos_y, pos_x, pos_y, col);
                old_pos_x = pos_x;
                old_pos_y = pos_y;
            }
        }

        AddLine(x, y + h, x + w, y + h, color[0]);
        AddLine(x, y, x, y + h, color[0]);
    }

    void _DrawHist(float x, float y, float w, float h, float[][] data, int startSample, Color[] color, float maxRange = -1.0f)
    {
        if (data == null || data.Length == 0 || data[0] == null)
            throw new System.ArgumentException("Invalid data argument (data must contain at least one non null array");

        if (x < 0) x += Width;
        if (y < 0) y += Height;

        var numSamples = data[0].Length;
        for (int i = 1; i < data.Length; ++i)
        {
            if (data[i] == null || data[i].Length != numSamples)
                throw new System.ArgumentException("Length of data of all arrays must be the same");
        }

        if (color.Length != data.Length)
            throw new System.ArgumentException("Length of colors must match number of datasets");

        var dataLength = data.Length;

        float maxData = float.MinValue;

        // Find tallest stack of values
        for (var i = 0; i < numSamples; i++)
        {
            float sum = 0;

            foreach(var dataset in data)
                sum += dataset[i];

            if (sum > maxData)
                maxData = sum;
        }

        if (maxData > maxRange)
            maxRange = maxData;

        float dx = w / numSamples;
        float scale = maxRange > 0 ? h / maxRange : 1.0f;

        float stackOffset = 0;
        for (var i = 0; i < numSamples; i++)
        {
            stackOffset = 0;
            for (var j = 0; j < data.Length; j++)
            {
                var c = color[j];
                float d = data[j][(i + startSample) % numSamples];
                float barHeight = d * scale; // now in [0, h]
                var pos_x = m_OriginX + x + dx * i;
                var pos_y = m_OriginY + y + h - barHeight - stackOffset;
                var width = dx;
                var height = barHeight;
                stackOffset += barHeight;
                AddQuad(pos_x, pos_y, width, height, '\0', new Vector4(c.r, c.g, c.b, c.a));
            }
        }
    }

    void _DrawRect(float x, float y, float w, float h, Color col)
    {
        AddQuad(m_OriginX + x, m_OriginY + y, w, h, '\0', col);
    }

    void _Clear()
    {
        m_NumQuadsUsed = 0;
        m_NumLinesUsed = 0;
       
        SetOrigin(0, 0);
    }

    static char[] _buf = new char[1024];

    void OnPostRender()
    {
        m_LineMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, m_NumLinesToDraw * 6, 1);

        instanceMaterialProc.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, m_NumQuadsToDraw * 6, 1);
    }

    public static void Render(HDCamera hdCamera, CommandBuffer cmd)
    {
        if (!instance)
            return;
        instance._Render(hdCamera, cmd);
    }

    public static void Render3D(HDCamera hdCamera, CommandBuffer cmd)
    {
        if (!instance)
            return;
        instance._Render3D(hdCamera, cmd);
    }
    
    void _Render(HDCamera hdCamera, CommandBuffer cmd)
    {
        if (hdCamera.camera.cameraType != CameraType.Game)
             return;

        cmd.DrawProcedural(Matrix4x4.identity, m_LineMaterial, 0, MeshTopology.Triangles, m_NumLinesToDraw * 6, 1);
        cmd.DrawProcedural(Matrix4x4.identity, instanceMaterialProc, 0, MeshTopology.Triangles, m_NumQuadsToDraw * 6, 1);
    }

    void _Render3D(HDCamera hdCamera, CommandBuffer cmd)
    {
        if(m_line3DBuffer != null)
        {
            m_line3DBuffer.HDDraw(cmd);
        }
    }

    unsafe void AddLine(float x1, float y1, float x2, float y2, Vector4 col)
    {
        if (m_NumLinesUsed >= m_LineInstanceData.Length)
        {
            // Resize
            var newBuf = new LineInstanceData[m_LineInstanceData.Length + 128];
            System.Array.Copy(m_LineInstanceData, newBuf, m_LineInstanceData.Length);
            m_LineInstanceData = newBuf;
        }
        fixed (LineInstanceData* d = &m_LineInstanceData[m_NumLinesUsed])
        {
            d->color = col;
            d->position.x = x1;
            d->position.y = y1;
            d->position.z = x2;
            d->position.w = y2;
        }
        m_NumLinesUsed++;
    }

    public unsafe void AddQuad(float x, float y, float w, float h, char c, Vector4 col)
    {
        if (m_NumQuadsUsed >= m_QuadInstanceData.Length)
        {
            // Resize
            var newBuf = new QuadInstanceData[m_QuadInstanceData.Length + 128];
            System.Array.Copy(m_QuadInstanceData, newBuf, m_QuadInstanceData.Length);
            m_QuadInstanceData = newBuf;
        }

        fixed (QuadInstanceData* d = &m_QuadInstanceData[m_NumQuadsUsed])
        {
            if (c != '\0')
            {
                d->positionAndUV.z = (c - 32) % charCols;
                d->positionAndUV.w = (c - 32) / charCols;
                col.w = 0.0f;
            }
            else
            {
                d->positionAndUV.z = 0;
                d->positionAndUV.w = 0;
            }

            d->color = col;
            d->positionAndUV.x = x;
            d->positionAndUV.y = y;
            d->size.x = w;
            d->size.y = h;
            d->size.z = 0;
            d->size.w = 0;
        }

        m_NumQuadsUsed++;
    }


    float m_OriginX;
    float m_OriginY;
    Color m_CurrentColor = Color.white;

    struct QuadInstanceData
    {
        public Vector4 positionAndUV; // if UV are zero, dont sample
        public Vector4 size; // zw unused
        public Vector4 color;
    }

    struct LineInstanceData
    {
        public Vector4 position; // segment from (x,y) to (z,w)
        public Vector4 color;
    }

    Line3DBuffer m_line3DBuffer;

    int m_NumQuadsUsed = 0;
    int m_NumLinesUsed = 0;

    ComputeBuffer m_QuadInstanceBuffer;
    ComputeBuffer m_LineInstanceBuffer;
    int m_NumQuadsToDraw = 0;
    int m_NumLinesToDraw = 0;
    QuadInstanceData[] m_QuadInstanceData = new QuadInstanceData[128];
    LineInstanceData[] m_LineInstanceData = new LineInstanceData[128];

    Material m_LineMaterial;

}
