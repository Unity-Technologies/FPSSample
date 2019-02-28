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
    public class HDLitSettingsView : VisualElement
    {
        HDLitMasterNode m_Node;

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

        public HDLitSettingsView(HDLitMasterNode node)
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
                if (!m_Node.HasRefraction())
                {
                    ps.Add(new PropertyRow(CreateLabel("Blending Mode", indentLevel)), (row) =>
                    {
                        row.Add(new EnumField(HDLitMasterNode.AlphaModeLit.Additive), (field) =>
                        {
                            field.value = GetAlphaModeLit(m_Node.alphaMode);
                            field.OnValueChanged(ChangeBlendMode);
                        });
                    });
                }

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

                ps.Add(new PropertyRow(CreateLabel("Draw Before Refraction", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.drawBeforeRefraction.isOn;
                        toggle.OnToggleChanged(ChangeDrawBeforeRefraction);
                    });
                });

                if (!m_Node.drawBeforeRefraction.isOn)
                {
                    ps.Add(new PropertyRow(CreateLabel("Refraction Model", indentLevel)), (row) =>
                    {
                        row.Add(new EnumField(ScreenSpaceRefraction.RefractionModel.None), (field) =>
                        {
                            field.value = m_Node.refractionModel;
                            field.OnValueChanged(ChangeRefractionModel);
                        });
                    });
                }

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

            ps.Add(new PropertyRow(CreateLabel("Material Type", indentLevel)), (row) =>
            {
                row.Add(new EnumField(HDLitMasterNode.MaterialType.Standard), (field) =>
                {
                    field.value = m_Node.materialType;
                    field.OnValueChanged(ChangeMaterialType);
                });
            });

            ++indentLevel;
            if (m_Node.materialType == HDLitMasterNode.MaterialType.SubsurfaceScattering)
            {
                ps.Add(new PropertyRow(CreateLabel("Transmission", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.sssTransmission.isOn;
                        toggle.OnToggleChanged(ChangeSSSTransmission);
                    });
                });
            }

            if (m_Node.materialType == HDLitMasterNode.MaterialType.SpecularColor)
            {
                ps.Add(new PropertyRow(CreateLabel("Energy Conserving Specular", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.energyConservingSpecular.isOn;
                        toggle.OnToggleChanged(ChangeEnergyConservingSpecular);
                    });
                });
            }
            --indentLevel;

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

            ps.Add(new PropertyRow(CreateLabel("Specular AA", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.specularAA.isOn;
                    toggle.OnToggleChanged(ChangeSpecularAA);
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
            m_Node.materialType = (HDLitMasterNode.MaterialType)evt.newValue;
        }

        void ChangeSSSTransmission(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("SSS Transmission Change");
            ToggleData td = m_Node.sssTransmission;
            td.isOn = evt.newValue;
            m_Node.sssTransmission = td;
        }

        void ChangeBlendMode(ChangeEvent<Enum> evt)
        {
            // Make sure the mapping is correct by handling each case.
            AlphaMode alphaMode = GetAlphaMode((HDLitMasterNode.AlphaModeLit)evt.newValue);

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

        void ChangeDrawBeforeRefraction(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Draw Before Refraction Change");
            ToggleData td = m_Node.drawBeforeRefraction;
            td.isOn = evt.newValue;
            m_Node.drawBeforeRefraction = td;
        }

        void ChangeRefractionModel(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.refractionModel, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Refraction Model Change");
            m_Node.refractionModel = (ScreenSpaceRefraction.RefractionModel)evt.newValue;
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

        void ChangeSpecularAA(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Specular AA Change");
            ToggleData td = m_Node.specularAA;
            td.isOn = evt.newValue;
            m_Node.specularAA = td;
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

        public AlphaMode GetAlphaMode(HDLitMasterNode.AlphaModeLit alphaModeLit)
        {
            switch (alphaModeLit)
            {
                case HDLitMasterNode.AlphaModeLit.Alpha:
                    return AlphaMode.Alpha;
                case HDLitMasterNode.AlphaModeLit.PremultipliedAlpha:
                    return AlphaMode.Premultiply;
                case HDLitMasterNode.AlphaModeLit.Additive:
                    return AlphaMode.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaModeLit);
                        return AlphaMode.Alpha;
                    }
                    
            }
        }

        public HDLitMasterNode.AlphaModeLit GetAlphaModeLit(AlphaMode alphaMode)
        {
            switch (alphaMode)
            {
                case AlphaMode.Alpha:
                    return HDLitMasterNode.AlphaModeLit.Alpha;
                case AlphaMode.Premultiply:
                    return HDLitMasterNode.AlphaModeLit.PremultipliedAlpha;
                case AlphaMode.Additive:
                    return HDLitMasterNode.AlphaModeLit.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaMode);
                        return HDLitMasterNode.AlphaModeLit.Alpha;
                    }                    
            }
        }
    }
}
