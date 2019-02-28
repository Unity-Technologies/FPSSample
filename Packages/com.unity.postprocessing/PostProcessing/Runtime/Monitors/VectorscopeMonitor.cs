using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the Vectorscope monitor.
    /// </summary>
    [Serializable]
    public sealed class VectorscopeMonitor : Monitor
    {
        /// <summary>
        /// The width and height of the rendered vectorscope.
        /// </summary>
        public int size = 256;

        /// <summary>
        /// The exposure multiplier applied to the vectorscope values.
        /// </summary>
        public float exposure = 0.12f;

        ComputeBuffer m_Data;
        const int k_ThreadGroupSizeX = 16;
        const int k_ThreadGroupSizeY = 16;

        internal override void OnDisable()
        {
            base.OnDisable();

            if (m_Data != null)
                m_Data.Release();

            m_Data = null;
        }

        internal override bool NeedsHalfRes()
        {
            return true;
        }

        internal override bool ShaderResourcesAvailable(PostProcessRenderContext context)
        {
            return context.resources.computeShaders.vectorscope;
        }

        internal override void Render(PostProcessRenderContext context)
        {
            CheckOutput(size, size);
            exposure = Mathf.Max(0f, exposure);

            int count = size * size;
            if (m_Data == null)
                m_Data = new ComputeBuffer(count, sizeof(uint));
            else if (m_Data.count != count)
            {
                m_Data.Release();
                m_Data = new ComputeBuffer(count, sizeof(uint));
            }

            var compute = context.resources.computeShaders.vectorscope;
            var cmd = context.command;
            cmd.BeginSample("Vectorscope");

            var parameters = new Vector4(
                context.width / 2,
                context.height / 2,
                size,
                RuntimeUtilities.isLinearColorSpace ? 1 : 0
            );

            // Clear the buffer on every frame as we use it to accumulate values on every frame
            int kernel = compute.FindKernel("KVectorscopeClear");
            cmd.SetComputeBufferParam(compute, kernel, "_VectorscopeBuffer", m_Data);
            cmd.SetComputeVectorParam(compute, "_Params", parameters);
            cmd.DispatchCompute(compute, kernel,
                Mathf.CeilToInt(size / (float)k_ThreadGroupSizeX),
                Mathf.CeilToInt(size / (float)k_ThreadGroupSizeY),
                1
            );

            // Gather all pixels and fill in our histogram
            kernel = compute.FindKernel("KVectorscopeGather");
            cmd.SetComputeBufferParam(compute, kernel, "_VectorscopeBuffer", m_Data);
            cmd.SetComputeTextureParam(compute, kernel, "_Source", ShaderIDs.HalfResFinalCopy);
            cmd.DispatchCompute(compute, kernel, 
                Mathf.CeilToInt(parameters.x / k_ThreadGroupSizeX),
                Mathf.CeilToInt(parameters.y / k_ThreadGroupSizeY),
                1
            );

            // Generate the histogram texture
            var sheet = context.propertySheets.Get(context.resources.shaders.vectorscope);
            sheet.properties.SetVector(ShaderIDs.Params, new Vector4(size, size, exposure, 0f));
            sheet.properties.SetBuffer(ShaderIDs.VectorscopeBuffer, m_Data);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, output, sheet, 0);

            cmd.EndSample("Vectorscope");
        }
    }
}
