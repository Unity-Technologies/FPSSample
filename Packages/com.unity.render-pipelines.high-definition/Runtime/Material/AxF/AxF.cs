using System;
using UnityEngine;
using UnityEngine.Rendering;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class AxF : RenderPipelineMaterial
    {
        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1200)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes(new string[] {"Normal", "Normal View Space"}, true)]
            public Vector3  normalWS;

            [SurfaceDataAttributes("Tangent", true)]
            public Vector3  tangentWS;

            [SurfaceDataAttributes("BiTangent", true)]
            public Vector3  biTangentWS;

            // SVBRDF Variables
            [SurfaceDataAttributes("Diffuse Color", false, true)]
            public Vector3  diffuseColor;

            [SurfaceDataAttributes("Specular Color", false, true)]
            public Vector3  specularColor;

            [SurfaceDataAttributes("Fresnel F0", false, true)]
            public Vector3  fresnelF0;

            [SurfaceDataAttributes("Specular Lobe", false, true)]
            public Vector2  specularLobe;

            [SurfaceDataAttributes("Height")]
            public float    height_mm;

            [SurfaceDataAttributes("Anisotropic Angle")]
            public float    anisotropyAngle;

            // Car Paint Variables
            [SurfaceDataAttributes("Flakes UV")]
            public Vector2  flakesUV;

            [SurfaceDataAttributes("Flakes Mip")]
            public float    flakesMipLevel;

            // BTF Variables

            // Clearcoat
            [SurfaceDataAttributes("Clearcoat Color")]
            public Vector3  clearcoatColor;

            [SurfaceDataAttributes("Clearcoat Normal", true, true)]
            public Vector3  clearcoatNormalWS;

            [SurfaceDataAttributes("Clearcoat IOR")]
            public float    clearcoatIOR;

            [SurfaceDataAttributes(new string[] {"Geometric Normal"}, true)]
            public Vector3 geomNormalWS;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 1250)]
        public struct BSDFData
        {
            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3  normalWS;
            public Vector3  tangentWS;
            public Vector3  biTangentWS;

            // SVBRDF Variables
            public Vector3  diffuseColor;
            public Vector3  specularColor;
            public Vector3  fresnelF0;
            public Vector2  roughness;
            public float    height_mm;
            public float    anisotropyAngle;

            // Car Paint Variables
            [SurfaceDataAttributes("")]
            public Vector2  flakesUV;

            [SurfaceDataAttributes("Flakes Mip")]
            public float    flakesMipLevel;

            // BTF Variables

            // Clearcoat
            public Vector3  clearcoatColor;
            public Vector3  clearcoatNormalWS;
            public float    clearcoatIOR;

            public Vector3 geomNormalWS;
        };

        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------

        // For area lighting - We pack all texture inside a texture array to reduce the number of resource required
        Texture2DArray m_LtcData; // 0: m_LtcGGXMatrix - RGBA;

        Material        m_preIntegratedFGDMaterial_Ward = null;
        Material        m_preIntegratedFGDMaterial_CookTorrance = null;
        RenderTexture   m_preIntegratedFGD_Ward = null;
        RenderTexture   m_preIntegratedFGD_CookTorrance = null;

        private bool m_precomputedFGDTablesAreInit = false;

        public static readonly int _PreIntegratedFGD_Ward = Shader.PropertyToID("_PreIntegratedFGD_Ward");
        public static readonly int _PreIntegratedFGD_CookTorrance = Shader.PropertyToID("_PreIntegratedFGD_CookTorrance");
        public static readonly int _AxFLtcData = Shader.PropertyToID("_AxFLtcData");

        public AxF() {}

        public override void Build(HDRenderPipelineAsset hdAsset)
        {
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;

            // Create Materials
            m_preIntegratedFGDMaterial_Ward = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.preIntegratedFGD_WardPS);
            if (m_preIntegratedFGDMaterial_Ward == null)
                throw new Exception("Failed to create material for Ward BRDF pre-integration!");

            m_preIntegratedFGDMaterial_CookTorrance = CoreUtils.CreateEngineMaterial(hdrp.renderPipelineResources.shaders.preIntegratedFGD_CookTorrancePS);
            if (m_preIntegratedFGDMaterial_CookTorrance == null)
                throw new Exception("Failed to create material for Cook-Torrance BRDF pre-integration!");

            // Create render textures where we will render the FGD tables
            m_preIntegratedFGD_Ward = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
            m_preIntegratedFGD_Ward.hideFlags = HideFlags.HideAndDontSave;
            m_preIntegratedFGD_Ward.filterMode = FilterMode.Bilinear;
            m_preIntegratedFGD_Ward.wrapMode = TextureWrapMode.Clamp;
            m_preIntegratedFGD_Ward.hideFlags = HideFlags.DontSave;
            m_preIntegratedFGD_Ward.name = CoreUtils.GetRenderTargetAutoName(128, 128, 1, RenderTextureFormat.ARGB2101010, "PreIntegratedFGD_Ward");
            m_preIntegratedFGD_Ward.Create();

            m_preIntegratedFGD_CookTorrance = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
            m_preIntegratedFGD_CookTorrance.hideFlags = HideFlags.HideAndDontSave;
            m_preIntegratedFGD_CookTorrance.filterMode = FilterMode.Bilinear;
            m_preIntegratedFGD_CookTorrance.wrapMode = TextureWrapMode.Clamp;
            m_preIntegratedFGD_CookTorrance.hideFlags = HideFlags.DontSave;
            m_preIntegratedFGD_CookTorrance.name = CoreUtils.GetRenderTargetAutoName(128, 128, 1, RenderTextureFormat.ARGB2101010, "PreIntegratedFGD_CookTorrance");
            m_preIntegratedFGD_CookTorrance.Create();

            // LTC data

            m_LtcData = new Texture2DArray(LTCAreaLight.k_LtcLUTResolution, LTCAreaLight.k_LtcLUTResolution, 3, TextureFormat.RGBAHalf, false /*mipmap*/, true /* linear */)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = CoreUtils.GetTextureAutoName(LTCAreaLight.k_LtcLUTResolution, LTCAreaLight.k_LtcLUTResolution, TextureFormat.RGBAHalf, depth: 2, dim: TextureDimension.Tex2DArray, name: "LTC_LUT")
            };

            // Caution: This need to match order define in AxFLTCAreaLight
            LTCAreaLight.LoadLUT(m_LtcData, 0, TextureFormat.RGBAHalf, LTCAreaLight.s_LtcMatrixData_GGX);

            m_LtcData.Apply();
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_preIntegratedFGD_CookTorrance);
            CoreUtils.Destroy(m_preIntegratedFGD_Ward);
            CoreUtils.Destroy(m_preIntegratedFGDMaterial_CookTorrance);
            CoreUtils.Destroy(m_preIntegratedFGDMaterial_Ward);
            m_preIntegratedFGD_CookTorrance = null;
            m_preIntegratedFGD_Ward = null;
            m_preIntegratedFGDMaterial_Ward = null;
            m_preIntegratedFGDMaterial_CookTorrance = null;
            m_precomputedFGDTablesAreInit = false;

            // LTC data
            CoreUtils.Destroy(m_LtcData);
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            if (m_precomputedFGDTablesAreInit || m_preIntegratedFGDMaterial_Ward == null || m_preIntegratedFGDMaterial_CookTorrance == null)
            {
                return;
            }

            using (new ProfilingSample(cmd, "PreIntegratedFGD Material Generation for Ward & Cook-Torrance BRDF"))
            {
                CoreUtils.DrawFullScreen(cmd, m_preIntegratedFGDMaterial_Ward, new RenderTargetIdentifier(m_preIntegratedFGD_Ward));
                CoreUtils.DrawFullScreen(cmd, m_preIntegratedFGDMaterial_CookTorrance, new RenderTargetIdentifier(m_preIntegratedFGD_CookTorrance));
            }

            m_precomputedFGDTablesAreInit = true;
        }

        public override void Bind()
        {
            if (m_preIntegratedFGD_Ward == null ||  m_preIntegratedFGD_CookTorrance == null)
            {
                throw new Exception("Ward & Cook-Torrance BRDF pre-integration table not available!");
            }

            Shader.SetGlobalTexture(_PreIntegratedFGD_Ward, m_preIntegratedFGD_Ward);
            Shader.SetGlobalTexture(_PreIntegratedFGD_CookTorrance, m_preIntegratedFGD_CookTorrance);

            // LTC Data
            Shader.SetGlobalTexture(_AxFLtcData, m_LtcData);
        }
    }
}
