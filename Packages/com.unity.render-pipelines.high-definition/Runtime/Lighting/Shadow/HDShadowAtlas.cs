using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDShadowAtlas
    {
        public readonly RenderTargetIdentifier  identifier;
        public readonly List<HDShadowRequest>   shadowRequests = new List<HDShadowRequest>();

        int                             m_Width;
        int                             m_Height;

        RTHandleSystem.RTHandle         m_Atlas;
        Material                        m_ClearMaterial;

        public HDShadowAtlas(int width, int height, Material clearMaterial, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "")
        {
            m_Atlas = RTHandles.Alloc(width, height, filterMode: filterMode, depthBufferBits: depthBufferBits, sRGB: false, colorFormat: format, name: name);
            m_Width = width;
            m_Height = height;
            identifier = new RenderTargetIdentifier(m_Atlas);
            m_ClearMaterial = clearMaterial;
        }

        public void Reserve(HDShadowRequest shadowRequest)
        {
            shadowRequests.Add(shadowRequest);
        }

        // Stable (unlike List.Sort) sorting algorithm which, unlike Linq's, doesn't use JIT (lol).
        // Sorts in place. Very efficient (O(n)) for already sorted data.
        void InsertionSort(List<HDShadowRequest> array)
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
            // TODO: change this sort (and maybe the list) by something that don't create garbage
            // Note: it is very important to keep the added order for shadow maps that are the same size (for punctual lights)
            // and because of that we can't use List.Sort because it messes up the list even with a good custom comparator

            // Perform a deep copy.
            int n = (shadowRequests != null) ? shadowRequests.Count : 0;
            var sortedRequests = new List<HDShadowRequest>(n);

            for (int i = 0; i < n; i++)
            {
                sortedRequests.Add(shadowRequests[i]);
            }

            // Sort in place.
            InsertionSort(sortedRequests);

            float curX = 0, curY = 0, curH = 0, xMax = m_Width, yMax = m_Height;

            // Assign to every view shadow viewport request a position in the atlas
            foreach (var shadowRequest in sortedRequests)
            {
                // shadow atlas layouting
                Rect viewport = new Rect(Vector2.zero, shadowRequest.viewportSize);
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
            while (index < shadowRequests.Count)
            {
                float y = 0;
                float currentMaxXCache = currentMaxX;
                do
                {
                    Rect r = new Rect(Vector2.zero, shadowRequests[index].viewportSize);
                    r.x = currentMaxX;
                    r.y = y;
                    y += r.height;
                    currentY = Mathf.Max(currentY, y);
                    currentMaxXCache = Mathf.Max(currentMaxXCache, currentMaxX + r.width);
                    shadowRequests[index].atlasViewport = r;
                    index++;
                } while (y < currentMaxY && index < shadowRequests.Count);
                currentMaxY = Mathf.Max(currentMaxY, currentY);
                currentMaxX = currentMaxXCache;
                if (index >= shadowRequests.Count)
                    continue;
                float x = 0;
                float currentMaxYCache = currentMaxY;
                do
                {
                    Rect r = new Rect(Vector2.zero, shadowRequests[index].viewportSize);
                    r.x = x;
                    r.y = currentMaxY;
                    x += r.width;
                    currentX = Mathf.Max(currentX, x);
                    currentMaxYCache = Mathf.Max(currentMaxYCache, currentMaxY + r.height);
                    shadowRequests[index].atlasViewport = r;
                    index++;
                } while (x < currentMaxX && index < shadowRequests.Count);
                currentMaxX = Mathf.Max(currentMaxX, currentX);
                currentMaxY = currentMaxYCache;
            }

            Vector4 scale = new Vector4(m_Width / currentMaxX, m_Height / currentMaxY, m_Width / currentMaxX, m_Height / currentMaxY);

            // Scale down every shadow rects to fit with the current atlas size
            foreach (var r in shadowRequests)
            {
                r.viewBias.w /= Mathf.Max(scale.x, scale.y);
                Vector4 s = new Vector4(r.atlasViewport.x, r.atlasViewport.y, r.atlasViewport.width, r.atlasViewport.height);
                Vector4 reScaled = Vector4.Scale(s, scale);

                r.atlasViewport = new Rect(reScaled.x, reScaled.y, reScaled.z, reScaled.w);
            }
        }

        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, DrawShadowsSettings dss)
        {
            cmd.SetRenderTarget(identifier);
            cmd.SetGlobalVector(HDShaderIDs._ShadowAtlasSize, new Vector4(m_Width, m_Height, 1.0f / m_Width, 1.0f / m_Height));

            foreach (var shadowRequest in shadowRequests)
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
            Vector4 validRange = new Vector4(minValue, 1.0f / (maxValue - minValue));
            float rWidth = 1.0f / m_Width;
            float rHeight = 1.0f / m_Height;
            Vector4 scaleBias = Vector4.Scale(new Vector4(rWidth, rHeight, rWidth, rHeight), new Vector4(atlasViewport.width, atlasViewport.height, atlasViewport.x, atlasViewport.y));

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetTexture("_AtlasTexture", m_Atlas.rt);
            propertyBlock.SetVector("_TextureScaleBias", scaleBias);
            propertyBlock.SetVector("_ValidRange", validRange);
            cmd.SetViewport(new Rect(screenX, screenY, screenSizeX, screenSizeY));
            cmd.DrawProcedural(Matrix4x4.identity, debugMaterial, debugMaterial.FindPass("VARIANCESHADOW"), MeshTopology.Triangles, 3, 1, propertyBlock);
        }

        public void Clear()
        {
            shadowRequests.Clear();
        }

        public void Release()
        {
            RTHandles.Release(m_Atlas);
        }
    }
}
