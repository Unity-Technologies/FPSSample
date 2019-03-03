using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.PostProcessing
{
    class TextureLerper
    {
        static TextureLerper m_Instance;
        internal static TextureLerper instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new TextureLerper();

                return m_Instance;
            }
        }

        CommandBuffer m_Command;
        PropertySheetFactory m_PropertySheets;
        PostProcessResources m_Resources;

        List<RenderTexture> m_Recycled;
        List<RenderTexture> m_Actives;

        TextureLerper()
        {
            m_Recycled = new List<RenderTexture>();
            m_Actives = new List<RenderTexture>();
        }

        internal void BeginFrame(PostProcessRenderContext context)
        {
            m_Command = context.command;
            m_PropertySheets = context.propertySheets;
            m_Resources = context.resources;
        }

        internal void EndFrame()
        {
            // Release any remaining RT in the recycled list
            if (m_Recycled.Count > 0)
            {
                foreach (var rt in m_Recycled)
                    RuntimeUtilities.Destroy(rt);

                m_Recycled.Clear();
            }

            // There's a high probability that RTs will be requested in the same order on next
            // frame so keep them in the same order
            if (m_Actives.Count > 0)
            {
                foreach (var rt in m_Actives)
                    m_Recycled.Add(rt);

                m_Actives.Clear();
            }
        }

        RenderTexture Get(RenderTextureFormat format, int w, int h, int d = 1, bool enableRandomWrite = false, bool force3D = false)
        {
            RenderTexture rt = null;
            int i, len = m_Recycled.Count;

            for (i = 0; i < len; i++)
            {
                var r = m_Recycled[i];
                if (r.width == w && r.height == h && r.volumeDepth == d && r.format == format && r.enableRandomWrite == enableRandomWrite && (!force3D || (r.dimension == TextureDimension.Tex3D)))
                {
                    rt = r;
                    break;
                }
            }

            if (rt == null)
            {
                var dimension = (d > 1) || force3D
                    ? TextureDimension.Tex3D
                    : TextureDimension.Tex2D;

                rt = new RenderTexture(w, h, 0, format)
                {
                    dimension = dimension,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0,
                    volumeDepth = d,
                    enableRandomWrite = enableRandomWrite
                };
                rt.Create();
            }
            else m_Recycled.RemoveAt(i);

            m_Actives.Add(rt);
            return rt;
        }

        internal Texture Lerp(Texture from, Texture to, float t)
        {
            Assert.IsNotNull(from);
            Assert.IsNotNull(to);
            Assert.AreEqual(from.width, to.width);
            Assert.AreEqual(from.height, to.height);

            // Saves a potentially expensive fullscreen blit when using dirt textures & the likes
            if (from == to)
                return from;

            // Don't need to lerp boundary conditions
            if (t <= 0f) return from;
            if (t >= 1f) return to;

            bool is3D = from is Texture3D
                    || (from is RenderTexture && ((RenderTexture)from).volumeDepth > 1);

            RenderTexture rt;

            // 3D texture blending is a special case and only works on compute enabled platforms
            if (is3D)
            {
                int dpth = @from is Texture3D ? ((Texture3D) @from).depth : ((RenderTexture) @from).volumeDepth;
                int size = Mathf.Max(from.width, from.height);
                size = Mathf.Max(size, dpth);
                
                rt = Get(RenderTextureFormat.ARGBHalf, from.width, from.height, dpth, true, true);

                var compute = m_Resources.computeShaders.texture3dLerp;
                int kernel = compute.FindKernel("KTexture3DLerp");
                m_Command.SetComputeVectorParam(compute, "_DimensionsAndLerp", new Vector4(from.width, from.height, dpth, t));
                m_Command.SetComputeTextureParam(compute, kernel, "_Output", rt);
                m_Command.SetComputeTextureParam(compute, kernel, "_From", from);
                m_Command.SetComputeTextureParam(compute, kernel, "_To", to);

                uint tgsX, tgsY, tgsZ;
                compute.GetKernelThreadGroupSizes(kernel, out tgsX, out tgsY, out tgsZ);
                Assert.AreEqual(tgsX, tgsY);
                int groupSizeXY = Mathf.CeilToInt(size / (float)tgsX);
                int groupSizeZ = Mathf.CeilToInt(size / (float)tgsZ);

                m_Command.DispatchCompute(compute, kernel, groupSizeXY, groupSizeXY, groupSizeZ);
                return rt;
            }

            // 2D texture blending
            // We could handle textures with different sizes by picking the biggest one to avoid
            // popping effects. This would work in most cases but will still pop if one texture is
            // wider but shorter than the other. Generally speaking you're expected to use same-size
            // textures anyway so we decided not to handle this case at the moment, especially since
            // it would waste a lot of texture memory as soon as you start using bigger textures
            // (snow ball effect).
            var format = TextureFormatUtilities.GetUncompressedRenderTextureFormat(to);
            rt = Get(format, to.width, to.height);

            var sheet = m_PropertySheets.Get(m_Resources.shaders.texture2dLerp);
            sheet.properties.SetTexture(ShaderIDs.To, to);
            sheet.properties.SetFloat(ShaderIDs.Interp, t);

            m_Command.BlitFullscreenTriangle(from, rt, sheet, 0);

            return rt;
        }

        internal Texture Lerp(Texture from, Color to, float t)
        {
            Assert.IsNotNull(from);

            if (t < 0.00001)
                return from;

            bool is3D = from is Texture3D
                    || (from is RenderTexture && ((RenderTexture)from).volumeDepth > 1);

            RenderTexture rt;

            // 3D texture blending is a special case and only works on compute enabled platforms
            if (is3D)
            {
                int dpth = @from is Texture3D ? ((Texture3D) @from).depth : ((RenderTexture) @from).volumeDepth;
                int size = Mathf.Max(from.width, from.height);
                size = Mathf.Max(size, dpth);
                
                rt = Get(RenderTextureFormat.ARGBHalf, from.width, from.height, dpth, true, true);

                var compute = m_Resources.computeShaders.texture3dLerp;
                int kernel = compute.FindKernel("KTexture3DLerpToColor");
                m_Command.SetComputeVectorParam(compute, "_DimensionsAndLerp", new Vector4(from.width, from.height, dpth, t));
                m_Command.SetComputeVectorParam(compute, "_TargetColor", new Vector4(to.r, to.g, to.b, to.a));
                m_Command.SetComputeTextureParam(compute, kernel, "_Output", rt);
                m_Command.SetComputeTextureParam(compute, kernel, "_From", from);

                int groupSize = Mathf.CeilToInt(size / 4f);
                m_Command.DispatchCompute(compute, kernel, groupSize, groupSize, groupSize);
                return rt;
            }

            // 2D texture blending
            // We could handle textures with different sizes by picking the biggest one to avoid
            // popping effects. This would work in most cases but will still pop if one texture is
            // wider but shorter than the other. Generally speaking you're expected to use same-size
            // textures anyway so we decided not to handle this case at the moment, especially since
            // it would waste a lot of texture memory as soon as you start using bigger textures
            // (snow ball effect).
            var format = TextureFormatUtilities.GetUncompressedRenderTextureFormat(from);
            rt = Get(format, from.width, from.height);

            var sheet = m_PropertySheets.Get(m_Resources.shaders.texture2dLerp);
            sheet.properties.SetVector(ShaderIDs.TargetColor, new Vector4(to.r, to.g, to.b, to.a));
            sheet.properties.SetFloat(ShaderIDs.Interp, t);

            m_Command.BlitFullscreenTriangle(from, rt, sheet, 1);

            return rt;
        }
        
        
        internal void Clear()
        {
            foreach (var rt in m_Actives)
                RuntimeUtilities.Destroy(rt);

            foreach (var rt in m_Recycled)
                RuntimeUtilities.Destroy(rt);

            m_Actives.Clear();
            m_Recycled.Clear();
        }
    }
}
