using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the Histogram monitor.
    /// </summary>
    [Serializable]
    public sealed class HistogramMonitor : Monitor
    {
        /// <summary>
        /// Displayable channels.
        /// </summary>
        public enum Channel
        {
            /// <summary>
            /// The red channel.
            /// </summary>
            Red,

            /// <summary>
            /// The green channel.
            /// </summary>
            Green,

            /// <summary>
            /// The blue channel.
            /// </summary>
            Blue,

            /// <summary>
            /// The master (luminance) channel.
            /// </summary>
            Master
        }

        /// <summary>
        /// The width of the rendered histogram.
        /// </summary>
        public int width = 512;

        /// <summary>
        /// The height of the rendered histogram.
        /// </summary>
        public int height = 256;

        /// <summary>
        /// The channel to render.
        /// </summary>
        public Channel channel = Channel.Master;

        ComputeBuffer m_Data;
        const int k_NumBins = 256;
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
            return context.resources.computeShaders.gammaHistogram;
        }

        internal override void Render(PostProcessRenderContext context)
        {
            CheckOutput(width, height);

            if (m_Data == null)
                m_Data = new ComputeBuffer(k_NumBins, sizeof(uint));

            var compute = context.resources.computeShaders.gammaHistogram;
            var cmd = context.command;
            cmd.BeginSample("GammaHistogram");

            // Clear the buffer on every frame as we use it to accumulate values on every frame
            int kernel = compute.FindKernel("KHistogramClear");
            cmd.SetComputeBufferParam(compute, kernel, "_HistogramBuffer", m_Data);
            cmd.DispatchCompute(compute, kernel, Mathf.CeilToInt(k_NumBins / (float)k_ThreadGroupSizeX), 1, 1);

            // Gather all pixels and fill in our histogram
            kernel = compute.FindKernel("KHistogramGather");
            var parameters = new Vector4(
                context.width / 2,
                context.height / 2,
                RuntimeUtilities.isLinearColorSpace ? 1 : 0,
                (int)channel
            );

            cmd.SetComputeVectorParam(compute, "_Params", parameters);
            cmd.SetComputeTextureParam(compute, kernel, "_Source", ShaderIDs.HalfResFinalCopy);
            cmd.SetComputeBufferParam(compute, kernel, "_HistogramBuffer", m_Data);
            cmd.DispatchCompute(compute, kernel, 
                Mathf.CeilToInt(parameters.x / k_ThreadGroupSizeX),
                Mathf.CeilToInt(parameters.y / k_ThreadGroupSizeY),
                1
            );

            // Generate the histogram texture
            var sheet = context.propertySheets.Get(context.resources.shaders.gammaHistogram);
            sheet.properties.SetVector(ShaderIDs.Params, new Vector4(width, height, 0f, 0f));
            sheet.properties.SetBuffer(ShaderIDs.HistogramBuffer, m_Data);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, output, sheet, 0);

            cmd.EndSample("GammaHistogram");
        }
    }
}
