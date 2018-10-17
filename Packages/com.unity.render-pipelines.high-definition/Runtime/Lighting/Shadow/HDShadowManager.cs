using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public struct HDShadowData
    {
        public Vector3      rot0;
        public Vector3      rot1;
        public Vector3      rot2;
        public Vector3      pos;
        public Vector4      proj;

        public Vector2      atlasOffset;
        public float        edgeTolerance;
        public int          flags;

        public Vector4      zBufferParam;
        public Vector4      shadowMapSize;

        public Vector4      viewBias;
        public Vector3      normalBias;
        public float        _padding;

        public Vector4      shadowFilterParams0;

        public Matrix4x4    shadowToWorld;
    }

    // We use a different structure for directional light because these is a lot of data there
    // and it will add too much useless stuff for other lights
    // Note: In order to support HLSL array generation, we need to use fixed arrays and so a unsafe context for this struct
    [GenerateHLSL]
    public unsafe struct HDDirectionalShadowData
    {
        // We can't use Vector4 here because the vector4[] makes this struct non blittable
        [HLSLArray(4, typeof(Vector4))]
        public fixed float      sphereCascades[4 * 4];

        public Vector4          cascadeDirection;

        [HLSLArray(4, typeof(float))]
        public fixed float      cascadeBorders[4];
    }

    [GenerateHLSL]
    public enum HDShadowFlag
    {
        SampleBiasScale     = (1 << 0),
        EdgeLeakFixup       = (1 << 1),
        EdgeToleranceNormal = (1 << 2),
    }

    public class HDShadowRequest
    {
        public Matrix4x4            view;
        // Use device projection matrix for shader and projection for CommandBuffer.SetViewProjectionMatrices
        public Matrix4x4            deviceProjection;
        public Matrix4x4            projection;
        public Matrix4x4            shadowToWorld;
        public Vector3              position;
        public Vector2              viewportSize;
        public Vector4              zBufferParam;
        // Warning: this field is updated by ProcessShadowRequests and is invalid before
        public Rect                 atlasViewport;
        public bool                 zClip;

        // Store the final shadow indice in the shadow data array
        // Warning: the index is computed during ProcessShadowRequest and so is invalid before calling this function
        public int                  shadowIndex;

        // Determine in which atlas the shadow will be rendered
        public bool                 allowResize = true;

        // TODO: Remove these field once scriptable culling is here (currently required by ScriptableRenderContext.DrawShadows)
        public int                  lightIndex;
        public ShadowSplitData      splitData;
        // end

        public Vector4              viewBias;
        public Vector3              normalBias;
        public float                edgeTolerance;
        public int                  flags;

        // PCSS parameters
        public float                shadowSoftness;
        public int                  blockerSampleCount;
        public int                  filterSampleCount;
    }

    public enum HDShadowQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    [Serializable]
    public class HDShadowInitParameters
    {
        public const int        k_DefaultShadowAtlasSize = 4096;
        public const int        k_DefaultMaxShadowRequests = 128;
        // TODO: 32 bit shadowmap are not supported by RThandle currently, when they will, change Depth24 to Depth32
        public const DepthBits  k_DefaultShadowMapDepthBits = DepthBits.Depth24;

        public int              shadowAtlasWidth = k_DefaultShadowAtlasSize;
        public int              shadowAtlasHeight = k_DefaultShadowAtlasSize;
        public int              maxShadowRequests = k_DefaultMaxShadowRequests;
        public DepthBits        shadowMapsDepthBits = k_DefaultShadowMapDepthBits;
        public bool             useDynamicViewportRescale = true;

        public HDShadowQuality  punctualShadowQuality;
        public HDShadowQuality  directionalShadowQuality;
    }

    public class HDShadowManager : IDisposable
    {
        public const int            k_DirectionalShadowCascadeCount = 4;

        List<HDShadowData>          m_ShadowDatas = new List<HDShadowData>();
        List<HDShadowRequest>       m_ShadowRequests = new List<HDShadowRequest>();

        HDDirectionalShadowData     m_DirectionalShadowData;

        // Structured buffer of shadow datas
        // TODO: hardcoded max shadow data value
        ComputeBuffer               m_ShadowDataBuffer;
        ComputeBuffer               m_DirectionalShadowDataBuffer;

        // The two shadowmaps atlases we uses, one for directional cascade (without resize) and the second for the rest of the shadows
        HDShadowAtlas               m_CascadeAtlas;
        HDShadowAtlas               m_Atlas;

        int                         m_Width;
        int                         m_Height;
        int                         m_maxShadowRequests;

        public HDShadowManager(int width, int height, int maxShadowRequests, DepthBits shadowMapDepthBits, Shader clearShader)
        {
            Material clearMaterial = CoreUtils.CreateEngineMaterial(clearShader);
            m_CascadeAtlas = new HDShadowAtlas(width, height, clearMaterial, depthBufferBits: shadowMapDepthBits, name: "Cascade Shadow Map Atlas");
            m_Atlas = new HDShadowAtlas(width, height, clearMaterial, depthBufferBits: shadowMapDepthBits, name: "Shadow Map Atlas");

            m_ShadowDataBuffer = new ComputeBuffer(maxShadowRequests, System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDShadowData)));
            m_DirectionalShadowDataBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(HDDirectionalShadowData)));

            m_Width = width;
            m_maxShadowRequests = maxShadowRequests;
            m_Height = height;
        }

        public int AddShadowRequest(HDShadowRequest shadowRequest)
        {
            if (m_ShadowRequests.Count >= m_maxShadowRequests)
                return -1;

            if (shadowRequest.allowResize)
                m_Atlas.Reserve(shadowRequest);
            else
                m_CascadeAtlas.Reserve(shadowRequest);

            // Keep track of all shadow request and the order they was requested
            m_ShadowRequests.Add(shadowRequest);

            return m_ShadowRequests.Count - 1;
        }

        public void UpdateCascade(int cascadeIndex, Vector4 cullingSphere, float border)
        {
            if (cullingSphere.w != float.NegativeInfinity)
            {
                cullingSphere.w *= cullingSphere.w;
            }

            unsafe
            {
                fixed (float * sphereCascadesBuffer = m_DirectionalShadowData.sphereCascades)
                    ((Vector4 *)sphereCascadesBuffer)[cascadeIndex] = cullingSphere;
                fixed (float * cascadeBorders = m_DirectionalShadowData.cascadeBorders)
                    cascadeBorders[cascadeIndex] = border;
            }
        }

        HDShadowData CreateShadowData(HDShadowRequest shadowRequest)
        {
            HDShadowData data = new HDShadowData();

            var devProj = shadowRequest.deviceProjection;
            var view = shadowRequest.view;
            data.proj = new Vector4(devProj.m00, devProj.m11, devProj.m22, devProj.m23);
            data.pos = shadowRequest.position;
            data.rot0 = new Vector3(view.m00, view.m01, view.m02);
            data.rot1 = new Vector3(view.m10, view.m11, view.m12);
            data.rot2 = new Vector3(view.m20, view.m21, view.m22);
            data.shadowToWorld = shadowRequest.shadowToWorld;

            // Compute the scale and offset (between 0 and 1) for the atlas coordinates
            float rWidth = 1.0f / m_Width;
            float rHeight = 1.0f / m_Height;
            data.atlasOffset = Vector2.Scale(new Vector2(rWidth, rHeight), new Vector2(shadowRequest.atlasViewport.x, shadowRequest.atlasViewport.y));

            data.shadowMapSize = new Vector4(shadowRequest.atlasViewport.width, shadowRequest.atlasViewport.height, 1.0f / shadowRequest.atlasViewport.width, 1.0f / shadowRequest.atlasViewport.height);

            data.viewBias = shadowRequest.viewBias;
            data.normalBias = shadowRequest.normalBias;
            data.flags = shadowRequest.flags;
            data.edgeTolerance = shadowRequest.edgeTolerance;

            data.shadowFilterParams0.x = shadowRequest.shadowSoftness;
            data.shadowFilterParams0.y = HDShadowUtils.Asfloat(shadowRequest.blockerSampleCount);
            data.shadowFilterParams0.z = HDShadowUtils.Asfloat(shadowRequest.filterSampleCount);
            data.shadowFilterParams0.w = 0;

            return data;
        }

        unsafe Vector4 GetCascadeSphereAtIndex(int index)
        {
            fixed (float * sphereCascadesBuffer = m_DirectionalShadowData.sphereCascades)
            {
                return ((Vector4 *)sphereCascadesBuffer)[index];
            }
        }

        public void UpdateCullingParameters(ref ScriptableCullingParameters cullingParams)
        {
            cullingParams.shadowDistance = Mathf.Min(VolumeManager.instance.stack.GetComponent<HDShadowSettings>().maxShadowDistance, cullingParams.shadowDistance);
        }

        unsafe public void ProcessShadowRequests(CullResults cullResults, Camera camera, LightingDebugSettings lightingDebugSettings)
        {
            int shadowIndex = 0;

            if (lightingDebugSettings.shadowResolutionScaleFactor != 1.0f)
            {
                foreach (var shadowRequest in m_ShadowRequests)
                    shadowRequest.viewportSize *= lightingDebugSettings.shadowResolutionScaleFactor;
            }

            // Assign a position to all the shadows in the atlas, and scale shadows if needed
            if (!m_CascadeAtlas.Layout(false))
                Debug.LogWarning("Cascade Shadow atlasing has failed, try reducing the shadow resolution of the directional light or increase the shadow atlas size");
            m_Atlas.Layout();

            m_ShadowDatas.Clear();

            // Create all HDShadowDatas and update them with shadow request datas
            foreach (var shadowRequest in m_ShadowRequests)
            {
                m_ShadowDatas.Add(CreateShadowData(shadowRequest));
                shadowRequest.shadowIndex = shadowIndex++;
            }

            int first = k_DirectionalShadowCascadeCount, second = k_DirectionalShadowCascadeCount;

            fixed (float *sphereBuffer = m_DirectionalShadowData.sphereCascades)
            {
                Vector4 * sphere = (Vector4 *)sphereBuffer;
                for (int i = 0; i < k_DirectionalShadowCascadeCount; i++)
                {
                    first  = (first  == k_DirectionalShadowCascadeCount                       && sphere[i].w > 0.0f) ? i : first;
                    second = ((second == k_DirectionalShadowCascadeCount || second == first)  && sphere[i].w > 0.0f) ? i : second;
                }
            }

            // Update directional datas:
            if (second != k_DirectionalShadowCascadeCount)
                m_DirectionalShadowData.cascadeDirection = (GetCascadeSphereAtIndex(second) - GetCascadeSphereAtIndex(first)).normalized;
            else
                m_DirectionalShadowData.cascadeDirection = Vector4.zero;

            m_DirectionalShadowData.cascadeDirection.w = k_DirectionalShadowCascadeCount;
        }

        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, CullResults cullResults)
        {
            // Avoid to do any commands if there is no shadow to draw
            if (m_ShadowRequests.Count == 0)
                return ;

            // TODO remove DrawShadowSettings, lightIndex and splitData when scriptable culling is available
            DrawShadowsSettings dss = new DrawShadowsSettings(cullResults, 0);

            // Clear atlas render targets and draw shadows
            m_Atlas.RenderShadows(renderContext, cmd, dss);
            m_CascadeAtlas.RenderShadows(renderContext, cmd, dss);
        }

        public void SyncData()
        {
            // Avoid to upload datas which will not be used
            if (m_ShadowRequests.Count == 0)
                return;

            // Upload the shadow buffers to GPU
            m_ShadowDataBuffer.SetData(m_ShadowDatas);
            m_DirectionalShadowDataBuffer.SetData(new HDDirectionalShadowData[]{ m_DirectionalShadowData });
        }

        public void BindResources(CommandBuffer cmd)
        {
            // This code must be in sync with ShadowContext.hlsl
            cmd.SetGlobalBuffer(HDShaderIDs._HDShadowDatas, m_ShadowDataBuffer);
            cmd.SetGlobalBuffer(HDShaderIDs._HDDirectionalShadowData, m_DirectionalShadowDataBuffer);

            cmd.SetGlobalTexture(HDShaderIDs._ShadowmapAtlas, m_Atlas.identifier);
            cmd.SetGlobalTexture(HDShaderIDs._ShadowmapCascadeAtlas, m_CascadeAtlas.identifier);
        }

        public int GetShadowRequestCount()
        {
            return m_ShadowRequests.Count;
        }

        public void Clear()
        {
            // Clear the shadows atlas infos and requests
            m_Atlas.Clear();
            m_CascadeAtlas.Clear();

            m_ShadowRequests.Clear();
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            m_Atlas.DisplayAtlas(cmd, debugMaterial, new Rect(0, 0, m_Width, m_Height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, flipY);
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowCascadeAtlas(CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            m_CascadeAtlas.DisplayAtlas(cmd, debugMaterial, new Rect(0, 0, m_Width, m_Height), screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, flipY);
        }

        // Warning: must be called after ProcessShadowRequests and RenderShadows to have valid informations
        public void DisplayShadowMap(int shadowIndex, CommandBuffer cmd, Material debugMaterial, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, bool flipY)
        {
            if (shadowIndex >= m_ShadowRequests.Count)
                return;

            HDShadowRequest   shadowRequest = m_ShadowRequests[shadowIndex];

            if (shadowRequest.allowResize)
                m_Atlas.DisplayAtlas(cmd, debugMaterial, shadowRequest.atlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, flipY);
            else
                m_CascadeAtlas.DisplayAtlas(cmd, debugMaterial, shadowRequest.atlasViewport, screenX, screenY, screenSizeX, screenSizeY, minValue, maxValue, flipY);
        }

        public void Dispose()
        {
            m_ShadowDataBuffer.Dispose();
            m_DirectionalShadowDataBuffer.Dispose();
            m_Atlas.Release();
            m_CascadeAtlas.Release();
        }
    }
}
