using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class FabricSettingsView : VisualElement
    {
        FabricMasterNode m_Node;

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

        public FabricSettingsView(FabricMasterNode node)
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

                ps.Add(new PropertyRow(CreateLabel("Back Then Front Rendering", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.backThenFrontRendering.isOn;
                        toggle.OnToggleChanged(ChangeBackThenFrontRendering);
                    });
                });

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

            if (m_Node.surfaceType == SurfaceType.Transparent && m_Node.alphaTest.isOn)
            {
                ++indentLevel;
                ps.Add(new PropertyRow(CreateLabel("Alpha Cutoff Depth Prepass", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.alphaTestDepthPrepass.isOn;
                        toggle.OnToggleChanged(ChangeAlphaTestPrepass);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Alpha Cutoff Depth Postpass", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.alphaTestDepthPostpass.isOn;
                        toggle.OnToggleChanged(ChangeAlphaTestPostpass);
                    });
                });
                --indentLevel;
            }

            ps.Add(new PropertyRow(CreateLabel("Double Sided", indentLevel)), (row) =>
            {
                row.Add(new EnumField(DoubleSidedMode.Disabled), (field) =>
                {
                    field.value = m_Node.doubleSidedMode;
                    field.OnValueChanged(ChangeDoubleSidedMode);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Energy Conserving Specular", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.energyConservingSpecular.isOn;
                    toggle.OnToggleChanged(ChangeEnergyConservingSpecular);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Material Type", indentLevel)), (row) =>
            {
                row.Add(new EnumField(FabricMasterNode.MaterialType.CottonWool), (field) =>
                {
                    field.value = m_Node.materialType;
                    field.OnValueChanged(ChangeMaterialType);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Transmission", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.transmission.isOn;
                    toggle.OnToggleChanged(ChangeTransmission);
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

                

           ps.Add(new PropertyRow(CreateLabel("Receive Decals", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.receiveDecals.isOn;
                    toggle.OnToggleChanged(ChangeDecal);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Receives SSR", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.receiveSSR.isOn;
                    toggle.OnToggleChanged(ChangeSSR);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Specular Occlusion Mode", indentLevel)), (row) =>
            {
                row.Add(new EnumField(SpecularOcclusionMode.Off), (field) =>
                {
                    field.value = m_Node.specularOcclusionMode;
                    field.OnValueChanged(ChangeSpecularOcclusionMode);
                });
            });

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

        void ChangeMaterialType(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.materialType, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Material Type Change");
            m_Node.materialType = (FabricMasterNode.MaterialType)evt.newValue;
        }

        void ChangeTransmission(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Transmission Change");
            ToggleData td = m_Node.transmission;
            td.isOn = evt.newValue;
            m_Node.transmission = td;
        }

        void ChangeSubsurfaceScattering(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("SSS Change");
            ToggleData td = m_Node.subsurfaceScattering;
            td.isOn = evt.newValue;
            m_Node.subsurfaceScattering = td;
        }

        void ChangeBlendMode(ChangeEvent<Enum> evt)
        {
            // Make sure the mapping is correct by handling each case.
            AlphaMode alphaMode = GetAlphaMode((FabricMasterNode.AlphaModeFabric)evt.newValue);

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

        void ChangeBackThenFrontRendering(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Back Then Front Rendering Change");
            ToggleData td = m_Node.backThenFrontRendering;
            td.isOn = evt.newValue;
            m_Node.backThenFrontRendering = td;
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

        void ChangeAlphaTestPrepass(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Test Depth Prepass Change");
            ToggleData td = m_Node.alphaTestDepthPrepass;
            td.isOn = evt.newValue;
            m_Node.alphaTestDepthPrepass = td;
        }

        void ChangeAlphaTestPostpass(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Test Depth Postpass Change");
            ToggleData td = m_Node.alphaTestDepthPostpass;
            td.isOn = evt.newValue;
            m_Node.alphaTestDepthPostpass = td;
        }

        void ChangeDecal(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Decal Change");
            ToggleData td = m_Node.receiveDecals;
            td.isOn = evt.newValue;
            m_Node.receiveDecals = td;
        }

        void ChangeSSR(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("SSR Change");
            ToggleData td = m_Node.receiveSSR;
            td.isOn = evt.newValue;
            m_Node.receiveSSR = td;
        }

        void ChangeEnergyConservingSpecular(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Energy Conserving Specular Change");
            ToggleData td = m_Node.energyConservingSpecular;
            td.isOn = evt.newValue;
            m_Node.energyConservingSpecular = td;
        }

        void ChangeSpecularOcclusionMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.specularOcclusionMode, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Specular Occlusion Mode Change");
            m_Node.specularOcclusionMode = (SpecularOcclusionMode)evt.newValue;
        }

        public AlphaMode GetAlphaMode(FabricMasterNode.AlphaModeFabric alphaModeLit)
        {
            switch (alphaModeLit)
            {
                case FabricMasterNode.AlphaModeFabric.Alpha:
                    return AlphaMode.Alpha;
                case FabricMasterNode.AlphaModeFabric.PremultipliedAlpha:
                    return AlphaMode.Premultiply;
                case FabricMasterNode.AlphaModeFabric.Additive:
                    return AlphaMode.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaModeLit);
                        return AlphaMode.Alpha;
                    }
                    
            }
        }

        public FabricMasterNode.AlphaModeFabric GetAlphaModeLit(AlphaMode alphaMode)
        {
            switch (alphaMode)
            {
                case AlphaMode.Alpha:
                    return FabricMasterNode.AlphaModeFabric.Alpha;
                case AlphaMode.Premultiply:
                    return FabricMasterNode.AlphaModeFabric.PremultipliedAlpha;
                case AlphaMode.Additive:
                    return FabricMasterNode.AlphaModeFabric.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaMode);
                        return FabricMasterNode.AlphaModeFabric.Alpha;
                    }                    
            }
        }
    }
}
