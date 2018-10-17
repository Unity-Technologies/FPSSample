using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class PreIntegratedFGD
    {
        [GenerateHLSL]
        public enum FGDTexture
        {
            Resolution = 64
        }

        static PreIntegratedFGD s_Instance;

        public static PreIntegratedFGD instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new PreIntegratedFGD();

                return s_Instance;
            }
        }

        public enum FGDIndex
        {
            FGD_GGXAndDisneyDiffuse = 0,
            FGD_CharlieAndFabricLambert = 1,
            Count = 2
        }

        bool[] m_isInit = new bool[(int)FGDIndex.Count];
        int[] m_refCounting = new int[(int)FGDIndex.Count];

        Material[] m_PreIntegratedFGDMaterial = new Material[(int)FGDIndex.Count];
        RenderTexture[] m_PreIntegratedFGD = new RenderTexture[(int)FGDIndex.Count];

        PreIntegratedFGD()
        {            
            for (int i = 0; i < (int)FGDIndex.Count; ++i)
            {
                m_isInit[i] = false;
                m_refCounting[i] = 0;
            }
        }

        public void Build(FGDIndex index)
        {
            Debug.Assert(index != FGDIndex.Count);
            Debug.Assert(m_refCounting[(int)index] >= 0);

            if (m_refCounting[(int)index] == 0)
            {
                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                int res  = (int)FGDTexture.Resolution;

                switch(index)
                {
                    case FGDIndex.FGD_GGXAndDisneyDiffuse:
                        m_PreIntegratedFGDMaterial[(int)index] = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.preIntegratedFGD_GGXDisneyDiffusePS);
                        m_PreIntegratedFGD[(int)index] = new RenderTexture(res, res, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
                        m_PreIntegratedFGD[(int)index].hideFlags = HideFlags.HideAndDontSave;
                        m_PreIntegratedFGD[(int)index].filterMode = FilterMode.Bilinear;
                        m_PreIntegratedFGD[(int)index].wrapMode = TextureWrapMode.Clamp;
                        m_PreIntegratedFGD[(int)index].name = CoreUtils.GetRenderTargetAutoName(res, res, 1, RenderTextureFormat.ARGB2101010, "preIntegratedFGD_GGXDisneyDiffuse");
                        m_PreIntegratedFGD[(int)index].Create();
                        break;

                    case FGDIndex.FGD_CharlieAndFabricLambert:
                        m_PreIntegratedFGDMaterial[(int)index] = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.preIntegratedFGD_CharlieFabricLambertPS);
                        m_PreIntegratedFGD[(int)index] = new RenderTexture(res, res, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
                        m_PreIntegratedFGD[(int)index].hideFlags = HideFlags.HideAndDontSave;
                        m_PreIntegratedFGD[(int)index].filterMode = FilterMode.Bilinear;
                        m_PreIntegratedFGD[(int)index].wrapMode = TextureWrapMode.Clamp;
                        m_PreIntegratedFGD[(int)index].name = CoreUtils.GetRenderTargetAutoName(res, res, 1, RenderTextureFormat.ARGB2101010, "preIntegratedFGD_CharlieFabricLambert");
                        m_PreIntegratedFGD[(int)index].Create();
                        break;

                    default:
                        break;
                }

                m_isInit[(int)index] = false;
            }

            m_refCounting[(int)index]++;
        }

        public void RenderInit(FGDIndex index, CommandBuffer cmd)
        {
            // Here we have to test IsCreated because in some circumstances (like loading RenderDoc), the texture is internally destroyed but we don't know from C# side.
            // In this case IsCreated will return false, allowing us to re-render the texture (setting the texture as current RT during DrawFullScreen will automatically re-create it internally)
            if (m_isInit[(int)index] && m_PreIntegratedFGD[(int)index].IsCreated())
                return;

            using (new ProfilingSample(cmd, "PreIntegratedFGD Material Generation"))
            {
                CoreUtils.DrawFullScreen(cmd, m_PreIntegratedFGDMaterial[(int)index], new RenderTargetIdentifier(m_PreIntegratedFGD[(int)index]));
            }

            m_isInit[(int)index] = true;
        }

        public void Cleanup(FGDIndex index)
        {
            m_refCounting[(int)index]--;

            if (m_refCounting[(int)index] == 0)
            {
                CoreUtils.Destroy(m_PreIntegratedFGDMaterial[(int)index]);
                CoreUtils.Destroy(m_PreIntegratedFGD[(int)index]);

                m_isInit[(int)index] = false;
            }

            Debug.Assert(m_refCounting[(int)index] >= 0);
        }

        public void Bind(FGDIndex index)
        {
            switch (index)
            {
                case FGDIndex.FGD_GGXAndDisneyDiffuse:
                    Shader.SetGlobalTexture(HDShaderIDs._PreIntegratedFGD_GGXDisneyDiffuse, m_PreIntegratedFGD[(int)index]);
                    break;

                case FGDIndex.FGD_CharlieAndFabricLambert:
                    Shader.SetGlobalTexture(HDShaderIDs._PreIntegratedFGD_CharlieAndFabric, m_PreIntegratedFGD[(int)index]);
                    break;

                default:
                    break;
            }
        }
    }
}
