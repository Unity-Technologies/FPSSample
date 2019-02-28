using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.Rendering.HDPipeline;

//TODOTODO: 
// clamp in shader code the ranged() properties
// or let inputs (eg mask?) follow invalid values ? Lit does that (let them free running).
namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Master", "StackLit")]
    public class StackLitMasterNode : MasterNode<IStackLitSubShader>, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string PositionSlotName = "Position";

        public const string BaseColorSlotName = "BaseColor";

        public const string NormalSlotName = "Normal";
        public const string BentNormalSlotName = "BentNormal";
        public const string TangentSlotName = "Tangent";

        public const string SubsurfaceMaskSlotName = "SubsurfaceMask";
        public const string ThicknessSlotName = "Thickness";
        public const string DiffusionProfileSlotName = "DiffusionProfile";

        public const string IridescenceMaskSlotName = "IridescenceMask";
        public const string IridescenceThicknessSlotName = "IridescenceThickness";

        public const string SpecularColorSlotName = "SpecularColor";
        public const string MetallicSlotName = "Metallic";
        public const string DielectricIorSlotName = "DielectricIor";

        public const string EmissionSlotName = "Emission";
        public const string SmoothnessASlotName = "SmoothnessA";
        public const string SmoothnessBSlotName = "SmoothnessB";
        public const string AmbientOcclusionSlotName = "AmbientOcclusion";
        public const string AlphaSlotName = "Alpha";
        public const string AlphaClipThresholdSlotName = "AlphaClipThreshold";
        public const string AnisotropyASlotName = "AnisotropyA";
        public const string AnisotropyBSlotName = "AnisotropyB";
        public const string SpecularAAScreenSpaceVarianceSlotName = "SpecularAAScreenSpaceVariance";
        public const string SpecularAAThresholdSlotName = "SpecularAAThreshold";
        public const string DistortionSlotName = "Distortion";
        public const string DistortionBlurSlotName = "DistortionBlur";

        public const string CoatSmoothnessSlotName = "CoatSmoothness";
        public const string CoatIorSlotName = "CoatIor";
        public const string CoatThicknessSlotName = "CoatThickness";
        public const string CoatExtinctionSlotName = "CoatExtinction";
        public const string CoatNormalSlotName = "CoatNormal";

        public const string LobeMixSlotName = "LobeMix";
        public const string HazinessSlotName = "Haziness";
        public const string HazeExtentSlotName = "HazeExtent";
        public const string HazyGlossMaxDielectricF0SlotName = "HazyGlossMaxDielectricF0"; // only valid if above option enabled and we have a basecolor + metallic input parametrization

        public const int PositionSlotId = 0;
        public const int BaseColorSlotId = 1;
        public const int NormalSlotId = 2;
        public const int BentNormalSlotId = 3;
        public const int TangentSlotId = 4;
        public const int SubsurfaceMaskSlotId = 5;
        public const int ThicknessSlotId = 6;
        public const int DiffusionProfileSlotId = 7;
        public const int IridescenceMaskSlotId = 8;
        public const int IridescenceThicknessSlotId = 9;
        public const int SpecularColorSlotId = 10;
        public const int DielectricIorSlotId = 11;
        public const int MetallicSlotId = 12;
        public const int EmissionSlotId = 13;
        public const int SmoothnessASlotId = 14;
        public const int SmoothnessBSlotId = 15;
        public const int AmbientOcclusionSlotId = 16;
        public const int AlphaSlotId = 17;
        public const int AlphaClipThresholdSlotId = 18;
        public const int AnisotropyASlotId = 19;
        public const int AnisotropyBSlotId = 20;
        public const int SpecularAAScreenSpaceVarianceSlotId = 21;
        public const int SpecularAAThresholdSlotId = 22;
        public const int DistortionSlotId = 23;
        public const int DistortionBlurSlotId = 24;

        public const int CoatSmoothnessSlotId = 25;
        public const int CoatIorSlotId = 26;
        public const int CoatThicknessSlotId = 27;
        public const int CoatExtinctionSlotId = 28;
        public const int CoatNormalSlotId = 29;

        public const int LobeMixSlotId = 30;
        public const int HazinessSlotId = 31;
        public const int HazeExtentSlotId = 32;
        public const int HazyGlossMaxDielectricF0SlotId = 33;

        // In StackLit.hlsl engine side
        //public enum BaseParametrization
        //public enum DualSpecularLobeParametrization

        // TODO: Add other available options for computing Vs based on:
        //
        // baked diffuse visibility (aka "data based AO") orientation 
        // (ie baked visibility cone (aka "bent visibility cone") orientation)
        // := { normal aligned (default bentnormal value), bent normal }
        // X
        // baked diffuse visibility solid angle inference algo from baked visibility scalar
        // (ie baked visibility cone aperture angle or solid angle)
        // := { uniform (solid angle measure), cos weighted (projected solid angle measure with cone oriented with normal), 
        //      cos properly weighted wrt bentnormal (projected solid angle measure with cone oriented with bent normal) }
        // X
        // Vs (aka specular occlusion) calculation algo from baked diffuse values above and BSDF lobe properties
        // := {triACE - not tuned to account for bent normal, cone BSDF proxy intersection with bent cone, precise SPTD BSDF proxy lobe integration against the bent cone} }
        //
        // Note that SSAO is used with triACE as a clamp value to combine it with the calculations done with the baked AO,
        // by doing a min(VsFromTriACE+SSAO, VsFromBakedVisibility).
        // This is true for Lit also, see in particular Lit.hlsl:PostEvaluateBSDF(), MaterialEvaluation.hlsl:GetScreenSpaceAmbientOcclusionMultibounce(),
        // where the handed bsdfData.specularOcclusion is data based (baked texture).
        //
        // Of the algos described above, we can narrow to these combined options:
        // { Off, NoBentNormalTriACE, *ConeCone, *SPTD }, where * is any combination of using the normal or the bentnormal with any of 3 choices to interpret the AO
        // measure for the cone aperture.
        //
        // The bentnormal port can be used to always control baked visibility orientation,
        // a SpecularOcclusionBaseMode enum could be { Off, TriACE, ConeCone, SPTD }
        // and we could provide another enum for ConeCone and SPTD like 
        // SpecularOcclusionBakedVisibilityMeasureMode = { uniform, cos weighted, cos bent weighted }
        //
        // These are the optional combinations that are available in the stacklit SO debug properties,
        // see _DebugSpecularOcclusion.
        // For now, we only allow an On / Off toggle, but it is easy to add two global defines in StackLit.hlsl
        // to set specularOcclusionAlgorithm and bentVisibilityAlgorithm if these def (ifdef) are found.
        public enum SpecularOcclusionBaseMode
        {
            Off,
            DirectFromAO, // TriACE
            ConeConeFromBentAO,
            SPTDIntegrationOfBentAO
        }

        public enum SpecularOcclusionAOConeSize
        {
            UniformAO,
            CosWeightedAO,
            CosWeightedBentCorrectAO
        }

        // Don't support Multiply
        public enum AlphaModeLit
        {
            Alpha,
            PremultipliedAlpha,
            Additive,
        }


        // Common surface config:
        //
        [SerializeField]
        SurfaceType m_SurfaceType;

        public SurfaceType surfaceType
        {
            get { return m_SurfaceType; }
            set
            {
                if (m_SurfaceType == value)
                    return;

                m_SurfaceType = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        AlphaMode m_AlphaMode;

        public AlphaMode alphaMode
        {
            get { return m_AlphaMode; }
            set
            {
                if (m_AlphaMode == value)
                    return;

                m_AlphaMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_BlendPreserveSpecular = true;

        public ToggleData blendPreserveSpecular
        {
            get { return new ToggleData(m_BlendPreserveSpecular); }
            set
            {
                if (m_BlendPreserveSpecular == value.isOn)
                    return;
                m_BlendPreserveSpecular = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_TransparencyFog = true;

        public ToggleData transparencyFog
        {
            get { return new ToggleData(m_TransparencyFog); }
            set
            {
                if (m_TransparencyFog == value.isOn)
                    return;
                m_TransparencyFog = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_Distortion;

        public ToggleData distortion
        {
            get { return new ToggleData(m_Distortion); }
            set
            {
                if (m_Distortion == value.isOn)
                    return;
                m_Distortion = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        DistortionMode m_DistortionMode;

        public DistortionMode distortionMode
        {
            get { return m_DistortionMode; }
            set
            {
                if (m_DistortionMode == value)
                    return;

                m_DistortionMode = value;
                UpdateNodeAfterDeserialization(); // TODOTODO: no need, ModificationScope.Graph is enough?
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_DistortionDepthTest = true;

        public ToggleData distortionDepthTest
        {
            get { return new ToggleData(m_DistortionDepthTest); }
            set
            {
                if (m_DistortionDepthTest == value.isOn)
                    return;
                m_DistortionDepthTest = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_AlphaTest;

        public ToggleData alphaTest
        {
            get { return new ToggleData(m_AlphaTest); }
            set
            {
                if (m_AlphaTest == value.isOn)
                    return;
                m_AlphaTest = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        int m_SortPriority;

        public int sortPriority
        {
            get { return m_SortPriority; }
            set
            {
                if (m_SortPriority == value)
                    return;
                m_SortPriority = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        DoubleSidedMode m_DoubleSidedMode;

        public DoubleSidedMode doubleSidedMode
        {
            get { return m_DoubleSidedMode; }
            set
            {
                if (m_DoubleSidedMode == value)
                    return;

                m_DoubleSidedMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        // Features: material surface input parametrizations
        //
        [SerializeField]
        StackLit.BaseParametrization m_BaseParametrization;

        public StackLit.BaseParametrization baseParametrization
        {
            get { return m_BaseParametrization; }
            set
            {
                if (m_BaseParametrization == value)
                    return;

                m_BaseParametrization = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_EnergyConservingSpecular = true;

        public ToggleData energyConservingSpecular
        {
            get { return new ToggleData(m_EnergyConservingSpecular); }
            set
            {
                if (m_EnergyConservingSpecular == value.isOn)
                    return;
                m_EnergyConservingSpecular = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        StackLit.DualSpecularLobeParametrization m_DualSpecularLobeParametrization;

        public StackLit.DualSpecularLobeParametrization dualSpecularLobeParametrization
        {
            get { return m_DualSpecularLobeParametrization; }
            set
            {
                if (m_DualSpecularLobeParametrization == value)
                    return;

                m_DualSpecularLobeParametrization = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        // TODOTODO Change all to enable* ?

        // Features: "physical" material type enables
        //
        [SerializeField]
        bool m_Anisotropy;

        public ToggleData anisotropy
        {
            get { return new ToggleData(m_Anisotropy); }
            set
            {
                if (m_Anisotropy == value.isOn)
                    return;
                m_Anisotropy = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_Coat;

        public ToggleData coat
        {
            get { return new ToggleData(m_Coat); }
            set
            {
                if (m_Coat == value.isOn)
                    return;
                m_Coat = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_CoatNormal;

        public ToggleData coatNormal
        {
            get { return new ToggleData(m_CoatNormal); }
            set
            {
                if (m_CoatNormal == value.isOn)
                    return;
                m_CoatNormal = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_DualSpecularLobe;

        public ToggleData dualSpecularLobe
        {
            get { return new ToggleData(m_DualSpecularLobe); }
            set
            {
                if (m_DualSpecularLobe == value.isOn)
                    return;
                m_DualSpecularLobe = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_CapHazinessWrtMetallic = true;

        public ToggleData capHazinessWrtMetallic
        {
            get { return new ToggleData(m_CapHazinessWrtMetallic); }
            set
            {
                if (m_CapHazinessWrtMetallic == value.isOn)
                    return;
                m_CapHazinessWrtMetallic = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_Iridescence;

        public ToggleData iridescence
        {
            get { return new ToggleData(m_Iridescence); }
            set
            {
                if (m_Iridescence == value.isOn)
                    return;
                m_Iridescence = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_SubsurfaceScattering;

        public ToggleData subsurfaceScattering
        {
            get { return new ToggleData(m_SubsurfaceScattering); }
            set
            {
                if (m_SubsurfaceScattering == value.isOn)
                    return;
                m_SubsurfaceScattering = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_Transmission;

        public ToggleData transmission
        {
            get { return new ToggleData(m_Transmission); }
            set
            {
                if (m_Transmission == value.isOn)
                    return;
                m_Transmission = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        // Features: other options
        //
        [SerializeField]
        bool m_ReceiveDecals = true;

        public ToggleData receiveDecals
        {
            get { return new ToggleData(m_ReceiveDecals); }
            set
            {
                if (m_ReceiveDecals == value.isOn)
                    return;
                m_ReceiveDecals = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_ReceiveSSR = true;

        public ToggleData receiveSSR
        {
            get { return new ToggleData(m_ReceiveSSR); }
            set
            {
                if (m_ReceiveSSR == value.isOn)
                    return;
                m_ReceiveSSR = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_GeometricSpecularAA;

        public ToggleData geometricSpecularAA
        {
            get { return new ToggleData(m_GeometricSpecularAA); }
            set
            {
                if (m_GeometricSpecularAA == value.isOn)
                    return;
                m_GeometricSpecularAA = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        // TODOTODO: Allow the combinations of the debug mode (fromAO, bentcone+cone, bentone+SPTD) ?
        [SerializeField]
        bool m_SpecularOcclusion;

        public ToggleData specularOcclusion
        {
            get { return new ToggleData(m_SpecularOcclusion); }
            set
            {
                if (m_SpecularOcclusion == value.isOn)
                    return;
                m_SpecularOcclusion = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        // Features: Advanced options
        //
        [SerializeField]
        bool m_AnisotropyForAreaLights = true;

        public ToggleData anisotropyForAreaLights
        {
            get { return new ToggleData(m_AnisotropyForAreaLights); }
            set
            {
                if (m_AnisotropyForAreaLights == value.isOn)
                    return;
                m_AnisotropyForAreaLights = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_RecomputeStackPerLight;

        public ToggleData recomputeStackPerLight
        {
            get { return new ToggleData(m_RecomputeStackPerLight); }
            set
            {
                if (m_RecomputeStackPerLight == value.isOn)
                    return;
                m_RecomputeStackPerLight = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_ShadeBaseUsingRefractedAngles;

        public ToggleData shadeBaseUsingRefractedAngles
        {
            get { return new ToggleData(m_ShadeBaseUsingRefractedAngles); }
            set
            {
                if (m_ShadeBaseUsingRefractedAngles == value.isOn)
                    return;
                m_ShadeBaseUsingRefractedAngles = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_Debug;

        public ToggleData debug
        {
            get { return new ToggleData(m_Debug); }
            set
            {
                if (m_Debug == value.isOn)
                    return;
                m_Debug = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        public StackLitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/StackLit-Master-Node"; }
        }

        public bool HasDistortion()
        {
            return (surfaceType == SurfaceType.Transparent && distortion.isOn);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "StackLit Master";

            List<int> validSlots = new List<int>();

            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionSlotName, PositionSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            validSlots.Add(PositionSlotId);

            AddSlot(new NormalMaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
            validSlots.Add(NormalSlotId);

            AddSlot(new NormalMaterialSlot(BentNormalSlotId, BentNormalSlotName, BentNormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
            validSlots.Add(BentNormalSlotId);

            AddSlot(new TangentMaterialSlot(TangentSlotId, TangentSlotName, TangentSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
            validSlots.Add(TangentSlotId);

            AddSlot(new ColorRGBMaterialSlot(BaseColorSlotId, BaseColorSlotName, BaseColorSlotName, SlotType.Input, Color.white, ColorMode.Default, ShaderStageCapability.Fragment));
            validSlots.Add(BaseColorSlotId);

            if (baseParametrization == StackLit.BaseParametrization.BaseMetallic)
            {
                AddSlot(new Vector1MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(MetallicSlotId);
                AddSlot(new Vector1MaterialSlot(DielectricIorSlotId, DielectricIorSlotName, DielectricIorSlotName, SlotType.Input, 1.5f, ShaderStageCapability.Fragment));
                validSlots.Add(DielectricIorSlotId);
            }
            else if (baseParametrization == StackLit.BaseParametrization.SpecularColor)
            {
                AddSlot(new ColorRGBMaterialSlot(SpecularColorSlotId, SpecularColorSlotName, SpecularColorSlotName, SlotType.Input, Color.white, ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularColorSlotId);
            }

            AddSlot(new Vector1MaterialSlot(SmoothnessASlotId, SmoothnessASlotName, SmoothnessASlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            validSlots.Add(SmoothnessASlotId);

            if (anisotropy.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AnisotropyASlotId, AnisotropyASlotName, AnisotropyASlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(AnisotropyASlotId);
            }

            AddSlot(new Vector1MaterialSlot(AmbientOcclusionSlotId, AmbientOcclusionSlotName, AmbientOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            validSlots.Add(AmbientOcclusionSlotId);

            if (coat.isOn)
            {
                AddSlot(new Vector1MaterialSlot(CoatSmoothnessSlotId, CoatSmoothnessSlotName, CoatSmoothnessSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(CoatSmoothnessSlotId);
                AddSlot(new Vector1MaterialSlot(CoatIorSlotId, CoatIorSlotName, CoatIorSlotName, SlotType.Input, 1.5f, ShaderStageCapability.Fragment));
                validSlots.Add(CoatIorSlotId);
                AddSlot(new Vector1MaterialSlot(CoatThicknessSlotId, CoatThicknessSlotName, CoatThicknessSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(CoatThicknessSlotId);
                AddSlot(new ColorRGBMaterialSlot(CoatExtinctionSlotId, CoatExtinctionSlotName, CoatExtinctionSlotName, SlotType.Input, Color.white, ColorMode.HDR, ShaderStageCapability.Fragment));
                validSlots.Add(CoatExtinctionSlotId);

                if (coatNormal.isOn)
                {
                    AddSlot(new NormalMaterialSlot(CoatNormalSlotId, CoatNormalSlotName, CoatNormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
                    validSlots.Add(CoatNormalSlotId);
                }
            }

            if (dualSpecularLobe.isOn)
            {
                if (dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.Direct)
                {
                    AddSlot(new Vector1MaterialSlot(SmoothnessBSlotId, SmoothnessBSlotName, SmoothnessBSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                    validSlots.Add(SmoothnessBSlotId);
                    AddSlot(new Vector1MaterialSlot(LobeMixSlotId, LobeMixSlotName, LobeMixSlotName, SlotType.Input, 0.3f, ShaderStageCapability.Fragment));
                    validSlots.Add(LobeMixSlotId);
                }
                else if (dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss)
                {
                    AddSlot(new Vector1MaterialSlot(HazinessSlotId, HazinessSlotName, HazinessSlotName, SlotType.Input, 0.2f, ShaderStageCapability.Fragment));
                    validSlots.Add(HazinessSlotId);
                    AddSlot(new Vector1MaterialSlot(HazeExtentSlotId, HazeExtentSlotName, HazeExtentSlotName, SlotType.Input, 3.0f, ShaderStageCapability.Fragment));
                    validSlots.Add(HazeExtentSlotId);

                    if (capHazinessWrtMetallic.isOn && baseParametrization == StackLit.BaseParametrization.BaseMetallic) // the later should be an assert really
                    {
                        AddSlot(new Vector1MaterialSlot(HazyGlossMaxDielectricF0SlotId, HazyGlossMaxDielectricF0SlotName, HazyGlossMaxDielectricF0SlotName, SlotType.Input, 0.25f, ShaderStageCapability.Fragment));
                        validSlots.Add(HazyGlossMaxDielectricF0SlotId);
                    }
                }

                if (anisotropy.isOn)
                {
                    AddSlot(new Vector1MaterialSlot(AnisotropyBSlotId, AnisotropyBSlotName, AnisotropyBSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                    validSlots.Add(AnisotropyBSlotId);
                }
            }

            if (iridescence.isOn)
            {
                AddSlot(new Vector1MaterialSlot(IridescenceMaskSlotId, IridescenceMaskSlotName, IridescenceMaskSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(IridescenceMaskSlotId);
                AddSlot(new Vector1MaterialSlot(IridescenceThicknessSlotId, IridescenceThicknessSlotName, IridescenceThicknessSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(IridescenceThicknessSlotId);
            }

            if (subsurfaceScattering.isOn)
            {
                AddSlot(new Vector1MaterialSlot(SubsurfaceMaskSlotId, SubsurfaceMaskSlotName, SubsurfaceMaskSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SubsurfaceMaskSlotId);
            }

            if (transmission.isOn)
            {
                AddSlot(new Vector1MaterialSlot(ThicknessSlotId, ThicknessSlotName, ThicknessSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(ThicknessSlotId);
            }

            if (subsurfaceScattering.isOn || transmission.isOn)
            {
                AddSlot(new DiffusionProfileInputMaterialSlot(DiffusionProfileSlotId, DiffusionProfileSlotName, DiffusionProfileSlotName, ShaderStageCapability.Fragment));
                validSlots.Add(DiffusionProfileSlotId);
            }

            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            validSlots.Add(AlphaSlotId);

            if (alphaTest.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AlphaClipThresholdSlotId, AlphaClipThresholdSlotName, AlphaClipThresholdSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaClipThresholdSlotId);
            }

            AddSlot(new ColorRGBMaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, Color.black, ColorMode.HDR, ShaderStageCapability.Fragment));
            validSlots.Add(EmissionSlotId);

            if (HasDistortion())
            {
                AddSlot(new Vector2MaterialSlot(DistortionSlotId, DistortionSlotName, DistortionSlotName, SlotType.Input, new Vector2(2.0f, -1.0f), ShaderStageCapability.Fragment));
                validSlots.Add(DistortionSlotId);

                AddSlot(new Vector1MaterialSlot(DistortionBlurSlotId, DistortionBlurSlotName, DistortionBlurSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(DistortionBlurSlotId);
            }

            if (geometricSpecularAA.isOn)
            {
                AddSlot(new Vector1MaterialSlot(SpecularAAScreenSpaceVarianceSlotId, SpecularAAScreenSpaceVarianceSlotName, SpecularAAScreenSpaceVarianceSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularAAScreenSpaceVarianceSlotId);

                AddSlot(new Vector1MaterialSlot(SpecularAAThresholdSlotId, SpecularAAThresholdSlotName, SpecularAAThresholdSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularAAThresholdSlotId);
            }

            RemoveSlotsNameNotMatching(validSlots, true);
        }

        protected override VisualElement CreateCommonSettingsElement()
        {
            return new StackLitSettingsView(this);
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetSlots(slots);

            List<MaterialSlot> validSlots = new List<MaterialSlot>();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].stageCapability != ShaderStageCapability.All && slots[i].stageCapability != stageCapability)
                    continue;

                validSlots.Add(slots[i]);
            }
            return validSlots.OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresNormal(stageCapability));
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetSlots(slots);

            List<MaterialSlot> validSlots = new List<MaterialSlot>();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].stageCapability != ShaderStageCapability.All && slots[i].stageCapability != stageCapability)
                    continue;

                validSlots.Add(slots[i]);
            }
            return validSlots.OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresTangent(stageCapability));
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetSlots(slots);

            List<MaterialSlot> validSlots = new List<MaterialSlot>();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].stageCapability != ShaderStageCapability.All && slots[i].stageCapability != stageCapability)
                    continue;

                validSlots.Add(slots[i]);
            }
            return validSlots.OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresPosition(stageCapability));
        }

        public bool RequiresSplitLighting()
        {
            return subsurfaceScattering.isOn;
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (debug.isOn)
            {
                // We have useful debug options in StackLit, so add them always, and let the UI editor (non shadergraph) handle displaying them
                // since this is also the editor that controls the keyword switching for the debug mode.
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugEnvLobeMask", // xyz is environments lights lobe 0 1 2 Enable, w is Enable VLayering
                    displayName = "_DebugEnvLobeMask",
                    value = new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                });
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugLobeMask", // xyz is analytical dirac lights lobe 0 1 2 Enable", false),
                    displayName = "_DebugLobeMask",
                    value = new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                });
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugAniso", // x is Hack Enable, w is factor
                    displayName = "_DebugAniso",
                    value = new Vector4(1.0f, 0.0f, 0.0f, 1000.0f)
                });
                // _DebugSpecularOcclusion:
                //
                // eg (2,2,1,2) :
                // .x = SO method {0 = fromAO, 1 = conecone, 2 = SPTD},
                // .y = bentao algo {0 = uniform, cos, bent cos},
                // .z = use upper visible hemisphere clipping,
                // .w = The last component of _DebugSpecularOcclusion controls debug visualization:
                //      -1 colors the object according to the SO algorithm used, 
                //      and values from 1 to 4 controls what the lighting debug display mode will show when set to show "indirect specular occlusion":
                //      Since there's not one value in our case,
                //      0 will show the object all red to indicate to choose one, 1-4 corresponds to showing
                //      1 = coat SO, 2 = base lobe A SO, 3 = base lobe B SO, 4 = shows the result of sampling the SSAO texture (screenSpaceAmbientOcclusion).
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugSpecularOcclusion",
                    displayName = "_DebugSpecularOcclusion",
                    value = new Vector4(2.0f, 2.0f, 1.0f, 2.0f)
                });
            }

            // Trunk currently relies on checking material property "_EmissionColor" to allow emissive GI. If it doesn't find that property, or it is black, GI is forced off.
            // ShaderGraph doesn't use this property, so currently it inserts a dummy color (white). This dummy color may be removed entirely once the following PR has been merged in trunk: Pull request #74105
            // The user will then need to explicitly disable emissive GI if it is not needed.
            // To be able to automatically disable emission based on the ShaderGraph config when emission is black,
            // we will need a more general way to communicate this to the engine (not directly tied to a material property).
            collector.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = "_EmissionColor",
                hidden = true,
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f)
            });

            base.CollectShaderProperties(collector, generationMode);
        }
    }
}
