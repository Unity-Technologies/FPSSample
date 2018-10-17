using System;
using UnityEngine.Rendering;
//using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class StackLit : RenderPipelineMaterial
    {
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            StackLitStandard                = 1 << 0,
            StackLitDualSpecularLobe        = 1 << 1,
            StackLitAnisotropy              = 1 << 2,
            StackLitCoat                    = 1 << 3,
            StackLitIridescence             = 1 << 4,
            StackLitSubsurfaceScattering    = 1 << 5,
            StackLitTransmission            = 1 << 6,
            StackLitCoatNormalMap           = 1 << 7,
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1100)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("Material Features")]
            public uint materialFeatures;

            // Bottom interface (2 lobes BSDF)
            // Standard parametrization
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;

            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            [SurfaceDataAttributes("Metallic")]
            public float metallic;

            [SurfaceDataAttributes("IOR")]
            public float dielectricIor;

            [SurfaceDataAttributes(new string[] {"Normal", "Normal View Space"}, true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] {"Geometric Normal", "Geometric Normal View Space"}, true)]
            public Vector3 geomNormalWS;

            [SurfaceDataAttributes(new string[] {"Coat Normal", "Coat Normal View Space"}, true)]
            public Vector3 coatNormalWS;

            [SurfaceDataAttributes("Smoothness A")]
            public float perceptualSmoothnessA;

            // Dual specular lobe
            [SurfaceDataAttributes("Smoothness B")]
            public float perceptualSmoothnessB;

            [SurfaceDataAttributes("Lobe Mixing")]
            public float lobeMix;

            // Anisotropic
            [SurfaceDataAttributes("Tangent", true)]
            public Vector3 tangentWS;

            [SurfaceDataAttributes("Anisotropy")]
            public float anisotropy; // anisotropic ratio(0->no isotropic; 1->full anisotropy in tangent direction, -1->full anisotropy in bitangent direction)

            // Iridescence
            [SurfaceDataAttributes("IridescenceIor")]
            public float iridescenceIor;
            [SurfaceDataAttributes("IridescenceThickness")]
            public float iridescenceThickness;
            [SurfaceDataAttributes("Iridescence Mask")]
            public float iridescenceMask;

            // Top interface and media (clearcoat)
            [SurfaceDataAttributes("Coat Smoothness")]
            public float coatPerceptualSmoothness;
            [SurfaceDataAttributes("Coat IOR")]
            public float coatIor;
            [SurfaceDataAttributes("Coat Thickness")]
            public float coatThickness;
            [SurfaceDataAttributes("Coat Extinction Coefficient")]
            public Vector3 coatExtinction;

            // SSS
            [SurfaceDataAttributes("Diffusion Profile")]
            public uint diffusionProfile;
            [SurfaceDataAttributes("Subsurface Mask")]
            public float subsurfaceMask;

            // Transmission
            // + Diffusion Profile
            [SurfaceDataAttributes("Thickness")]
            public float thickness;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------
        [GenerateHLSL(PackingRules.Exact, false, true, 1150)]
        public struct BSDFData
        {
            public uint materialFeatures;

            // Bottom interface (2 lobes BSDF)
            // Standard parametrization
            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 fresnel0;

            public float ambientOcclusion;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] {"Geometric Normal", "Geometric Normal View Space"}, true)]
            public Vector3 geomNormalWS;

            [SurfaceDataAttributes(new string[] {"Coat Normal", "Coat Normal View Space"}, true)]
            public Vector3 coatNormalWS;

            public float perceptualRoughnessA;

            // Dual specular lobe
            public float perceptualRoughnessB;
            public float lobeMix;

            // Anisotropic
            [SurfaceDataAttributes("", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("", true)]
            public Vector3 bitangentWS;
            public float roughnessAT;
            public float roughnessAB;
            public float roughnessBT;
            public float roughnessBB;
            public float anisotropy;

            // Top interface and media (clearcoat)
            public float coatRoughness;
            public float coatPerceptualRoughness;
            public float coatIor;
            public float coatThickness;
            public Vector3 coatExtinction;

            // iridescence
            public float iridescenceIor;
            public float iridescenceThickness;
            public float iridescenceMask;

            // SSS
            public uint diffusionProfile;
            public float subsurfaceMask;

            // Transmission
            // + Diffusion Profile
            public float thickness;
            public bool useThickObjectMode; // Read from the diffusion profile
            public Vector3 transmittance;   // Precomputation of transmittance
        };

        //-----------------------------------------------------------------------------
        // Init precomputed textures
        //-----------------------------------------------------------------------------

        public StackLit() {}

        public override void Build(HDRenderPipelineAsset hdAsset)
        {
            PreIntegratedFGD.instance.Build(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            //LTCAreaLight.instance.Build();
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            //LTCAreaLight.instance.Cleanup();
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse, cmd);
        }

        public override void Bind()
        {
            PreIntegratedFGD.instance.Bind(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            //LTCAreaLight.instance.Bind();
        }
    }
}
