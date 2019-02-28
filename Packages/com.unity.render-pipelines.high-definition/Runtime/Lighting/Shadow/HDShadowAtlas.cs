using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDShadowAtlas
    {
        public RenderTargetIdentifier               identifier { get; private set; }
        readonly List<HDShadowResolutionRequest>    m_ShadowResolutionRequests = new List<HDShadowResolutionRequest>();
        readonly List<HDShadowRequest>              m_ShadowRequests = new List<HDShadowRequest>();

        public int                  width { get; private set; }
        public int                  height  { get; private set; }

        RTHandleSystem.RTHandle     m_Atlas;
        Material                    m_ClearMaterial;
        LightingDebugSettings       m_LightingDebugSettings;
        float                       m_RcpScaleFactor = 1;
        FilterMode                  m_FilterMode;
        DepthBits                   m_DepthBufferBits;
        RenderTextureFormat         m_Format;
        string                      m_Name;
        int                         m_AtlasSizeShaderID;

        public HDShadowAtlas(int width, int height, int atlasSizeShaderID, Material clearMaterial, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "")
        {
            this.width = width;
            this.height = height;
            m_FilterMode = filterMode;
            m_DepthBufferBits = depthBufferBits;
            m_Format = format;
            m_Name = name;
            m_AtlasSizeShaderID = atlasSizeShaderID;
            m_ClearMaterial = clearMaterial;

            AllocateRenderTexture();
        }

        void AllocateRenderTexture()
        {
            if (m_Atlas != null)
                m_Atlas.Release();
            
            m_Atlas = RTHandles.Alloc(width, height, filterMode: m_FilterMode, depthBufferBits: m_DepthBufferBits, sRGB: false, colorFormat: m_Format, name: m_Name);
            identifier = new RenderTargetIdentifier(m_Atlas);
        }

        public void UpdateSize(Vector2Int size)
        {
            if (m_Atlas == null || m_Atlas.referenceSize != size)
            {
                width = size.x;
                height = size.y;
                AllocateRenderTexture();
            }
        }

        public void ReserveResolution(HDShadowResolutionRequest shadowRequest)
        {
            m_ShadowResolutionRequests.Add(shadowRequest);
        }
        
        public void AddShadowRequest(HDShadowRequest shadowRequest)
        {
            m_ShadowRequests.Add(shadowRequest);
        }

        public void UpdateDebugSettings(LightingDebugSettings lightingDebugSettings)
        {
            m_LightingDebugSettings = lightingDebugSettings;
        }

        // Stable (unlike List.Sort) sorting algorithm which, unlike Linq's, doesn't use JIT (lol).
        // Sorts in place. Very efficient (O(n)) for already sorted data.
        void InsertionSort(List<HDShadowResolutionRequest> array)
        {
            int i = 1;
            int n = array.Count;

            while (i < n)
            {
                var curr = array[i];

                int j = i - 1;

                // Sort in descending order.
                while ((j >= 0) && ((curr.atlasViewport.height > array[j].atlasViewport.height) ||
                                    (curr.atlasViewport.width  > array[j].atlasViewport.width)))
                {
                    array[j + 1] = array[j];
                    j--;
                }

                array[j + 1] = curr;
                i++;
            }
        }

        public bool Layout(bool allowResize = true)
        {
            // Perform a deep copy.
            int n = (m_ShadowResolutionRequests != null) ? m_ShadowResolutionRequests.Count : 0;
            var sortedRequests = new List<HDShadowResolutionRequest>(n);

            for (int i = 0; i < n; i++)
            {
                sortedRequests.Add(m_ShadowResolutionRequests[i]);
            }

            // Note: it is very important to keep the added order for shadow maps that are the same size (for punctual lights)
            // and because of that we can't use List.Sort because it messes up the list even with a good custom comparator
            // Sort in place.
            InsertionSort(sortedRequests);

            float curX = 0, curY = 0, curH = 0, xMax = width, yMax = height;
            m_RcpScaleFactor = 1;

            // Assign to every view shadow viewport request a position in the atlas
            foreach (var shadowRequest in sortedRequests)
            {
                // shadow atlas layouting
                Rect viewport = new Rect(Vector2.zero, shadowRequest.resolution);
                curH = Mathf.Max(curH, viewport.height);

                if (curX + viewport.width > xMax)
                {
                    curX = 0;
                    curY += curH;
                    curH = viewport.height;
                }
                if (curY + curH > yMax)
                {
                    if (allowResize)
                    {
                        LayoutResize();
                        return true;
                    }
                    else
                        return false;
                }
                viewport.x = curX;
                viewport.y = curY;
                shadowRequest.atlasViewport = viewport;
                shadowRequest.resolution = viewport.size;
                curX += viewport.width;
            }

            return true;
        }

        void LayoutResize()
        {
            int index = 0;
            float currentX = 0;
            float currentY = 0;
            float currentMaxY = 0;
            float currentMaxX = 0;

            // Place shadows in a square shape
            while (index < m_ShadowResolutionRequests.Count)
            {
                float y = 0;
                float currentMaxXCache = currentMaxX;
                do
                {
                    Rect r = new Rect(Vector2.zero, m_ShadowResolutionRequests[index].resolution);
                    r.x = currentMaxX;
                    r.y = y;
                    y += r.height;
                    currentY = Mathf.Max(currentY, y);
                    currentMaxXCache = Mathf.Max(currentMaxXCache, currentMaxX + r.width);
                    m_ShadowResolutionRequests[index].atlasViewport = r;
                    index++;
                } while (y < currentMaxY && index < m_ShadowResolutionRequests.Count);
                currentMaxY = Mathf.Max(currentMaxY, currentY);
                currentMaxX = currentMaxXCache;
                if (index >= m_ShadowResolutionRequests.Count)
                    continue;
                float x = 0;
                float currentMaxYCache = currentMaxY;
                do
                {
                    Rect r = new Rect(Vector2.zero, m_ShadowResolutionRequests[index].resolution);
                    r.x = x;
                    r.y = currentMaxY;
                    x += r.width;
                    currentX = Mathf.Max(currentX, x);
                    currentMaxYCache = Mathf.Max(currentMaxYCache, currentMaxY + r.height);
                    m_ShadowResolutionRequests[index].atlasViewport = r;
                    index++;
                } while (x < currentMaxX && index < m_ShadowResolutionRequests.Count);
                currentMaxX = Mathf.Max(currentMaxX, currentX);
                currentMaxY = currentMaxYCache;
            }

            float maxResolution = Math.Max(currentMaxX, currentMaxY);
            Vector4 scale = new Vector4(width / maxResolution, height / maxResolution, width / maxResolution, height / maxResolution);
            m_RcpScaleFactor = Mathf.Min(scale.x, scale.y);

            // Scale down every shadow rects to fit with the current atlas size
            foreach (var r in m_ShadowResolutionRequests)
            {
                Vector4 s = new Vector4(r.atlasViewport.x, r.atlasViewport.y, r.atlasViewport.width, r.atlasViewport.height);
                Vector4 reScaled = Vector4.Scale(s, scale);

                r.atlasViewport = new Rect(reScaled.x, reScaled.y, reScaled.z, reScaled.w);
                r.resolution = r.atlasViewport.size;
            }
        }

        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, DrawShadowsSettings dss)
        {
            cmd.SetRenderTarget(identifier);
            cmd.SetGlobalVector(m_AtlasSizeShaderID, new Vector4(width, height, 1.0f / width, 1.0f / height));
            
            if (m_LightingDebugSettings.clearShadowAtlas)
                CoreUtils.DrawFullScreen(cmd, m_ClearMaterial, null, 0);

            foreach (var shadowRequest in m_ShadowRequests)
            {
                cmd.SetViewport(shadowRequest.atlasViewport);
                cmd.SetViewProjectionMatrices(shadowRequest.view, shadowRequest.projection);

                cmd.SetGlobalFloat(HDShaderIDs._ZClip, shadowRequest.zClip ? 1.0f : 0.0f);
                CoreUtils.DrawFullScreen(cmd, m_ClearMaterial, null, 0);

                dss.lightIndex = shadowRequest.lightIndex;
                dss.splitData = shadowRequest.splitData;

                // TODO: remove this execute when DrawShadows will use a CommandBuffer
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                renderContext.DrawShadows(ref dss);
            }

            cmd.SetGlobalFloat(HDShaderIDs._ZClip, 1.0f);   // Re-enable zclip globally
        }
        
        public void DisplayAtlas(CommandBuffer cmd, Material debugMaterial, Rect atlasViewport, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            if (m_Atlas == null)
                return;
            
            Vector4 validRange = new Vector4(minValue, 1.0f / (maxValue - minValue));
            float rWidth = 1.0f / width;
            float rHeight = 1.0f / height;
            Vector4 scaleBias = Vector4.Scale(new Vector4(rWidth, rHeight, rWidth, rHeight), new Vector4(atlasViewport.width, atlasViewport.height, atlasViewport.x, atlasViewport.y));

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture("_AtlasTexture", m_Atlas.rt);
            propertyBlock.SetVector("_TextureScaleBias", scaleBias);
            propertyBlock.SetVector("_ValidRange", validRange);
            propertyBlock.SetFloat("_RcpGlobalScaleFactor", m_RcpScaleFactor);
            cmd.SetViewport(new Rect(screenX, screenY, screenSizeX, screenSizeY));
            cmd.DrawProcedural(Matrix4x4.identity, debugMaterial, debugMaterial.FindPass("RegularShadow"), MeshTopology.Triangles, 3, 1, propertyBlock);
        }

        public void Clear()
        {
            m_ShadowResolutionRequests.Clear();
            m_ShadowRequests.Clear();
        }

        public void Release()
        {
            if (m_Atlas != null)
                RTHandles.Release(m_Atlas);
        }
    }
}
