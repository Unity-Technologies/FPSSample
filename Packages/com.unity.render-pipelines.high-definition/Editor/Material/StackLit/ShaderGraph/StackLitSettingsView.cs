using System;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.ShaderGraph.Drawing
{
    class StackLitSettingsView : VisualElement
    {
        StackLitMasterNode m_Node;

        IntegerField m_SortPiorityField;

        Label CreateLabel(string text, int indentLevel)
        {
            string label = "";
            for (var i = 0; i < indentLevel; i++)
            {
                label += "    ";
            }
            return new Label(label + text);
        }

        public StackLitSettingsView(StackLitMasterNode node)
        {
            m_Node = node;
            PropertySheet ps = new PropertySheet();

            int indentLevel = 0;
            ps.Add(new PropertyRow(CreateLabel("Surface Type", indentLevel)), (row) =>
            {
                row.Add(new EnumField(SurfaceType.Opaque), (field) =>
                {
                    field.value = m_Node.surfaceType;
                    field.OnValueChanged(ChangeSurfaceType);
                });
            });

            if (m_Node.surfaceType == SurfaceType.Transparent)
            {
                ++indentLevel;

                // No refraction in StackLit, always show this:
                ps.Add(new PropertyRow(CreateLabel("Blending Mode", indentLevel)), (row) =>
                {
                    row.Add(new EnumField(StackLitMasterNode.AlphaModeLit.Additive), (field) =>
                    {
                        field.value = GetAlphaModeLit(m_Node.alphaMode);
                        field.OnValueChanged(ChangeBlendMode);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Blend Preserves Specular", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.blendPreserveSpecular.isOn;
                        toggle.OnToggleChanged(ChangeBlendPreserveSpecular);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Fog", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.transparencyFog.isOn;
                        toggle.OnToggleChanged(ChangeTransparencyFog);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Distortion", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.distortion.isOn;
                        toggle.OnToggleChanged(ChangeDistortion);
                    });
                });

                if (m_Node.distortion.isOn)
                {
                    ++indentLevel;
                    ps.Add(new PropertyRow(CreateLabel("Mode", indentLevel)), (row) =>
                    {
                        row.Add(new EnumField(DistortionMode.Add), (field) =>
                        {
                            field.value = m_Node.distortionMode;
                            field.OnValueChanged(ChangeDistortionMode);
                        });
                    });
                    ps.Add(new PropertyRow(CreateLabel("Depth Test", indentLevel)), (row) =>
                    {
                        row.Add(new Toggle(), (toggle) =>
                        {
                            toggle.value = m_Node.distortionDepthTest.isOn;
                            toggle.OnToggleChanged(ChangeDistortionDepthTest);
                        });
                    });
                    --indentLevel;
                }

                m_SortPiorityField = new IntegerField();
                ps.Add(new PropertyRow(CreateLabel("Sort Priority", indentLevel)), (row) =>
                {
                    row.Add(m_SortPiorityField, (field) =>
                    {
                        field.value = m_Node.sortPriority;
                        field.OnValueChanged(ChangeSortPriority);
                    });
                });
                --indentLevel;
            }

            ps.Add(new PropertyRow(CreateLabel("Alpha Cutoff", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.alphaTest.isOn;
                    toggle.OnToggleChanged(ChangeAlphaTest);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Double Sided", indentLevel)), (row) =>
            {
                row.Add(new EnumField(DoubleSidedMode.Disabled), (field) =>
                {
                    field.value = m_Node.doubleSidedMode;
                    field.OnValueChanged(ChangeDoubleSidedMode);
                });
            });

            // Rest of UI looks like this:
            //
            //  baseParametrization
            //    energyConservingSpecular
            //  
            //  anisotropy
            //  coat
            //  coatNormal
            //  dualSpecularLobe
            //    dualSpecularLobeParametrization
            //    capHazinessWrtMetallic
            //  iridescence
            //  subsurfaceScattering
            //  transmission
            //  
            //  receiveDecals
            //  receiveSSR
            //  geometricSpecularAA
            //  specularOcclusion
            //  
            //  anisotropyForAreaLights
            //  recomputeStackPerLight
            //  shadeBaseUsingRefractedAngles

            // Base parametrization:

            ps.Add(new PropertyRow(CreateLabel("Base Color Parametrization", indentLevel)), (row) =>
            {
                row.Add(new EnumField(StackLit.BaseParametrization.BaseMetallic), (field) =>
                {
                    field.value = m_Node.baseParametrization;
                    field.OnValueChanged(ChangeBaseParametrization);
                });
            });

            if (m_Node.baseParametrization == StackLit.BaseParametrization.SpecularColor)
            {
                ++indentLevel;
                ps.Add(new PropertyRow(CreateLabel("Energy Conserving Specular", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.energyConservingSpecular.isOn;
                        toggle.OnToggleChanged(ChangeEnergyConservingSpecular);
                    });
                });
                --indentLevel;
            }

            // Material type enables:
            ps.Add(new PropertyRow(CreateLabel("Material Core Features", indentLevel)), (row) => {} );
            ++indentLevel;

            ps.Add(new PropertyRow(CreateLabel("Anisotropy", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.anisotropy.isOn;
                    toggle.OnToggleChanged(ChangeAnisotropy);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Coat", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.coat.isOn;
                    toggle.OnToggleChanged(ChangeCoat);
                });
            });

            if (m_Node.coat.isOn)
            {
                ++indentLevel;
                ps.Add(new PropertyRow(CreateLabel("Coat Normal", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.coatNormal.isOn;
                        toggle.OnToggleChanged(ChangeCoatNormal);
                    });
                });
                --indentLevel;
            }

            ps.Add(new PropertyRow(CreateLabel("Dual Specular Lobe", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.dualSpecularLobe.isOn;
                    toggle.OnToggleChanged(ChangeDualSpecularLobe);
                });
            });

            if (m_Node.dualSpecularLobe.isOn)
            {
                ++indentLevel;
                ps.Add(new PropertyRow(CreateLabel("Dual SpecularLobe Parametrization", indentLevel)), (row) =>
                {
                    row.Add(new EnumField(StackLit.DualSpecularLobeParametrization.HazyGloss), (field) =>
                    {
                        field.value = m_Node.dualSpecularLobeParametrization;
                        field.OnValueChanged(ChangeDualSpecularLobeParametrization);
                    });
                });
                if ((m_Node.baseParametrization == StackLit.BaseParametrization.BaseMetallic)
                    && (m_Node.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss))
                {
                    ps.Add(new PropertyRow(CreateLabel("Cap Haziness For Non Metallic", indentLevel)), (row) =>
                    {
                        row.Add(new Toggle(), (toggle) =>
                        {
                            toggle.value = m_Node.capHazinessWrtMetallic.isOn;
                            toggle.OnToggleChanged(ChangeCapHazinessWrtMetallic);
                        });
                    });
                }
                --indentLevel;
            }

            ps.Add(new PropertyRow(CreateLabel("Iridescence", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.iridescence.isOn;
                    toggle.OnToggleChanged(ChangeIridescence);
                });
            });

            if (m_Node.surfaceType != SurfaceType.Transparent)
            {
                ps.Add(new PropertyRow(CreateLabel("Subsurface Scattering", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.subsurfaceScattering.isOn;
                        toggle.OnToggleChanged(ChangeSubsurfaceScattering);
                    });
                });
            }

            ps.Add(new PropertyRow(CreateLabel("Transmission", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.transmission.isOn;
                    toggle.OnToggleChanged(ChangeTransmission);
                });
            });
            --indentLevel; // ...Material type enables.

            ps.Add(new PropertyRow(CreateLabel("Receive Decals", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.receiveDecals.isOn;
                    toggle.OnToggleChanged(ChangeReceiveDecals);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Receive SSR", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.receiveSSR.isOn;
                    toggle.OnToggleChanged(ChangeReceiveSSR);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Specular AA (for geometry)", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.geometricSpecularAA.isOn;
                    toggle.OnToggleChanged(ChangeGeometricSpecularAA);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Specular Occlusion", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.specularOcclusion.isOn;
                    toggle.OnToggleChanged(ChangeSpecularOcclusion);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Advanced Options", indentLevel)), (row) => {} );
            ++indentLevel;

            ps.Add(new PropertyRow(CreateLabel("Anisotropy For Area Lights", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.anisotropyForAreaLights.isOn;
                    toggle.OnToggleChanged(ChangeAnisotropyForAreaLights);
                });
            });

            if (m_Node.coat.isOn || m_Node.iridescence.isOn)
            {
                ps.Add(new PropertyRow(CreateLabel("Per Punctual/Directional Lights:", indentLevel)), (row) => { });
                ++indentLevel;
            }
            if (m_Node.coat.isOn)
            {
                ps.Add(new PropertyRow(CreateLabel("Base Layer Uses Refracted Angles", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.shadeBaseUsingRefractedAngles.isOn;
                        toggle.OnToggleChanged(ChangeShadeBaseUsingRefractedAngles);
                    });
                });
            }
            if (m_Node.coat.isOn || m_Node.iridescence.isOn)
            {
                ps.Add(new PropertyRow(CreateLabel("Recompute Stack & Iridescence", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.recomputeStackPerLight.isOn;
                        toggle.OnToggleChanged(ChangeRecomputeStackPerLight);
                    });
                });
            }
            if (m_Node.coat.isOn || m_Node.iridescence.isOn)
            {
                --indentLevel; // Per Punctual/Directional Lights:
            }

            ps.Add(new PropertyRow(CreateLabel("Show And Enable StackLit Debugs", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.debug.isOn;
                    toggle.OnToggleChanged(ChangeDebug);
                });
            });

            --indentLevel; //...Advanced options

            Add(ps);
        }

        void ChangeSurfaceType(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.surfaceType, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Surface Type Change");
            m_Node.surfaceType = (SurfaceType)evt.newValue;
        }

        void ChangeDoubleSidedMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.doubleSidedMode, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Double Sided Mode Change");
            m_Node.doubleSidedMode = (DoubleSidedMode)evt.newValue;
        }

        void ChangeBaseParametrization(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.baseParametrization, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Base Parametrization Change");
            m_Node.baseParametrization = (StackLit.BaseParametrization)evt.newValue;
        }

        void ChangeDualSpecularLobeParametrization(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.dualSpecularLobeParametrization, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Dual Specular Lobe Parametrization Change");
            m_Node.dualSpecularLobeParametrization = (StackLit.DualSpecularLobeParametrization)evt.newValue;
        }

        void ChangeBlendMode(ChangeEvent<Enum> evt)
        {
            // Make sure the mapping is correct by handling each case.
            AlphaMode alphaMode = GetAlphaMode((StackLitMasterNode.AlphaModeLit)evt.newValue);

            if (Equals(m_Node.alphaMode, alphaMode))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Mode Change");
            m_Node.alphaMode = alphaMode;
        }

        void ChangeBlendPreserveSpecular(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Blend Preserve Specular Change");
            ToggleData td = m_Node.blendPreserveSpecular;
            td.isOn = evt.newValue;
            m_Node.blendPreserveSpecular = td;
        }

        void ChangeTransparencyFog(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Transparency Fog Change");
            ToggleData td = m_Node.transparencyFog;
            td.isOn = evt.newValue;
            m_Node.transparencyFog = td;
        }

        void ChangeDistortion(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Distortion Change");
            ToggleData td = m_Node.distortion;
            td.isOn = evt.newValue;
            m_Node.distortion = td;
        }

        void ChangeDistortionMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.distortionMode, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Distortion Mode Change");
            m_Node.distortionMode = (DistortionMode)evt.newValue;
        }

        void ChangeDistortionDepthTest(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Distortion Depth Test Change");
            ToggleData td = m_Node.distortionDepthTest;
            td.isOn = evt.newValue;
            m_Node.distortionDepthTest = td;
        }

        void ChangeSortPriority(ChangeEvent<int> evt)
        {
            m_Node.sortPriority = Math.Max(-HDRenderQueue.k_TransparentPriorityQueueRange, Math.Min(evt.newValue, HDRenderQueue.k_TransparentPriorityQueueRange));
            // Force the text to match.
            m_SortPiorityField.value = m_Node.sortPriority;
            if (Equals(m_Node.sortPriority, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Sort Priority Change");
        }

        void ChangeAlphaTest(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Test Change");
            ToggleData td = m_Node.alphaTest;
            td.isOn = evt.newValue;
            m_Node.alphaTest = td;
        }

        void ChangeReceiveDecals(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Receive Decals Change");
            ToggleData td = m_Node.receiveDecals;
            td.isOn = evt.newValue;
            m_Node.receiveDecals = td;
        }

        void ChangeReceiveSSR(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Receive SSR Change");
            ToggleData td = m_Node.receiveSSR;
            td.isOn = evt.newValue;
            m_Node.receiveSSR = td;
        }

        void ChangeGeometricSpecularAA(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Specular AA Change");
            ToggleData td = m_Node.geometricSpecularAA;
            td.isOn = evt.newValue;
            m_Node.geometricSpecularAA = td;
        }

        void ChangeEnergyConservingSpecular(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Energy Conserving Specular Change");
            ToggleData td = m_Node.energyConservingSpecular;
            td.isOn = evt.newValue;
            m_Node.energyConservingSpecular = td;
        }

        void ChangeAnisotropy(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Anisotropy Change");
            ToggleData td = m_Node.anisotropy;
            td.isOn = evt.newValue;
            m_Node.anisotropy = td;
        }

        void ChangeCoat(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Coat Change");
            ToggleData td = m_Node.coat;
            td.isOn = evt.newValue;
            m_Node.coat = td;
        }

        void ChangeCoatNormal(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Coat Normal Change");
            ToggleData td = m_Node.coatNormal;
            td.isOn = evt.newValue;
            m_Node.coatNormal = td;
        }

        void ChangeDualSpecularLobe(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("DualSpecularLobe Change");
            ToggleData td = m_Node.dualSpecularLobe;
            td.isOn = evt.newValue;
            m_Node.dualSpecularLobe = td;
        }

        void ChangeCapHazinessWrtMetallic(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("CapHazinessWrtMetallic Change");
            ToggleData td = m_Node.capHazinessWrtMetallic;
            td.isOn = evt.newValue;
            m_Node.capHazinessWrtMetallic = td;
        }

        void ChangeIridescence(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Iridescence Change");
            ToggleData td = m_Node.iridescence;
            td.isOn = evt.newValue;
            m_Node.iridescence = td;
        }

        void ChangeSubsurfaceScattering(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("SubsurfaceScattering Change");
            ToggleData td = m_Node.subsurfaceScattering;
            td.isOn = evt.newValue;
            m_Node.subsurfaceScattering = td;
        }

        void ChangeTransmission(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Transmission Change");
            ToggleData td = m_Node.transmission;
            td.isOn = evt.newValue;
            m_Node.transmission = td;
        }

        void ChangeSpecularOcclusion(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("SpecularOcclusion Change");
            ToggleData td = m_Node.specularOcclusion;
            td.isOn = evt.newValue;
            m_Node.specularOcclusion = td;
        }

        void ChangeAnisotropyForAreaLights(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("AnisotropyForAreaLights Change");
            ToggleData td = m_Node.anisotropyForAreaLights;
            td.isOn = evt.newValue;
            m_Node.anisotropyForAreaLights = td;
        }

        void ChangeRecomputeStackPerLight(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("RecomputeStackPerLight Change");
            ToggleData td = m_Node.recomputeStackPerLight;
            td.isOn = evt.newValue;
            m_Node.recomputeStackPerLight = td;
        }

        void ChangeShadeBaseUsingRefractedAngles(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("ShadeBaseUsingRefractedAngles Change");
            ToggleData td = m_Node.shadeBaseUsingRefractedAngles;
            td.isOn = evt.newValue;
            m_Node.shadeBaseUsingRefractedAngles = td;
        }

        void ChangeDebug(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("StackLit Debug Change");
            ToggleData td = m_Node.debug;
            td.isOn = evt.newValue;
            m_Node.debug = td;
        }

        public AlphaMode GetAlphaMode(StackLitMasterNode.AlphaModeLit alphaModeLit)
        {
            switch (alphaModeLit)
            {
                case StackLitMasterNode.AlphaModeLit.Alpha:
                    return AlphaMode.Alpha;
                case StackLitMasterNode.AlphaModeLit.PremultipliedAlpha:
                    return AlphaMode.Premultiply;
                case StackLitMasterNode.AlphaModeLit.Additive:
                    return AlphaMode.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaModeLit);
                        return AlphaMode.Alpha;
                    }
                    
            }
        }

        public StackLitMasterNode.AlphaModeLit GetAlphaModeLit(AlphaMode alphaMode)
        {
            switch (alphaMode)
            {
                case AlphaMode.Alpha:
                    return StackLitMasterNode.AlphaModeLit.Alpha;
                case AlphaMode.Premultiply:
                    return StackLitMasterNode.AlphaModeLit.PremultipliedAlpha;
                case AlphaMode.Additive:
                    return StackLitMasterNode.AlphaModeLit.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaMode);
                        return StackLitMasterNode.AlphaModeLit.Alpha;
                    }                    
            }
        }
    }
}
