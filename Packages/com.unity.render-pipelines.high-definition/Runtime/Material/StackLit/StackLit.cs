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
            StackLitSpecularColor           = 1 << 8,
            StackLitHazyGloss               = 1 << 9,
        };

        // We will use keywords no need for [GenerateHLSL] as we don't test in HLSL such a value
        public enum BaseParametrization
        {
            BaseMetallic = 0,
            SpecularColor = 1, // MaterialFeatureFlags.StackLitSpecularColor
        };

        // We will use keywords no need for [GenerateHLSL] as we don't test in HLSL such a value
        public enum DualSpecularLobeParametrization
        {
            Direct = 0,
            HazyGloss = 1, // MaterialFeatureFlags.StackLitHazyGloss
            // Pascal Barla, Romain Pacanowski, Peter Vangorp. A Composite BRDF Model for Hazy Gloss.
            // Computer Graphics Forum, Wiley, 2018, 37, <10.1111/cgf.13475>. <hal-01818666v2>
            // https://hal.inria.fr/hal-01818666v2
            // Submitted on 6 Jul 2018
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

            [SurfaceDataAttributes("Dielectric IOR")]
            public float dielectricIor;

            [SurfaceDataAttributes("Specular Color", false, true)]
            public Vector3 specularColor;

            [SurfaceDataAttributes(new string[] {"Normal", "Normal View Space"}, true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] {"Geometric Normal", "Geometric Normal View Space"}, true)]
            public Vector3 geomNormalWS;

            [SurfaceDataAttributes(new string[] {"Coat Normal", "Coat Normal View Space"}, true)]
            public Vector3 coatNormalWS;

            [SurfaceDataAttributes(new string[] {"Bent Normal", "Bent Normal View Space"}, true)]
            public Vector3 bentNormalWS;

            [SurfaceDataAttributes("Smoothness A")]
            public float perceptualSmoothnessA;

            // Dual specular lobe: Direct parametrization (engine parameters)
            [SurfaceDataAttributes("Smoothness B")]
            public float perceptualSmoothnessB;

            [SurfaceDataAttributes("Lobe Mixing")]
            public float lobeMix;

            // Dual specular lobe: DualSpecularLobeParametrization == HazyGloss parametrization.
            //
            // Lobe B is the haze.
            //
            // In that mode, perceptual parameters of "haziness" and "hazeExtent" are used.
            // The base layer f0 parameter when the DualSpecularLobeParametrization was == "Direct"
            // is now also a perceptual parameter corresponding to a pseudo-f0 term, Fc(0) or
            // "coreFresnel0" (r_c in the paper). Although an intermediate value, this original
            // fresnel0 never reach the engine side (BSDFData). 
            //
            // [ Without the HazyGloss parametrization, the original base layer f0 is directly inferred
            // as f0 = f(baseColor, metallic) when the BaseParametrization is BaseMetallic
            // or directly given via f0 = SpecularColor when the BaseParametrization == SpecularColor.
            //
            // When the DualSpecularLobeParametrization is "HazyGloss", this base layer f0 is
            // now reinterpreted as the perceptual "core lobe reflectivity" or Fc(0) or r_c in
            // the paper.]
            //
            // From these perceptual parameters, the engine-used lobeMix, fresnel0 (SpecularColor)
            // and SmoothnessB parameters are set.
            //
            // [ TODO: We could actually scrap metallic and dielectricIor here and update specularColor
            // to always hold the f0 intermediate value (r_c), although you could then go further and
            // put the final "engine input" f0 in there, and other perceptuals like haziness and 
            // hazeExtent and update directly lobeMix here. For now we keep the shader mostly organized
            // like Lit ]
            [SurfaceDataAttributes("Haziness")]
            public float haziness;

            [SurfaceDataAttributes("Haze Extent")]
            public float hazeExtent;

            [SurfaceDataAttributes("Hazy Gloss Max Dielectric f0 When Using Metallic Input")]
            public float hazyGlossMaxDielectricF0;

            // Anisotropy
            [SurfaceDataAttributes("Tangent", true)]
            public Vector3 tangentWS;

            [SurfaceDataAttributes("AnisotropyA")]
            public float anisotropyA; // anisotropic ratio(0->no isotropic; 1->full anisotropy in tangent direction, -1->full anisotropy in bitangent direction)

            // Anisotropy for secondary specular lobe
            [SurfaceDataAttributes("AnisotropyB")]
            public float anisotropyB; // anisotropic ratio(0->no isotropic; 1->full anisotropy in tangent direction, -1->full anisotropy in bitangent direction)

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

            [SurfaceDataAttributes(new string[] {"Bent Normal", "Bent Normal View Space"}, true)]
            public Vector3 bentNormalWS;

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
            public float anisotropyA;
            public float anisotropyB;

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
            LTCAreaLight.instance.Build();
            SPTDistribution.instance.Build();
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Cleanup();
            SPTDistribution.instance.Cleanup();
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse, cmd);
        }

        public override void Bind()
        {
            PreIntegratedFGD.instance.Bind(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Bind();
            SPTDistribution.instance.Bind();
        }
    }
}
