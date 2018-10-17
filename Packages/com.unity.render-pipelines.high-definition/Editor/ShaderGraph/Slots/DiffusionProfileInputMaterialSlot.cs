using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class DiffusionProfileInputMaterialSlot : Vector1MaterialSlot
    {
        [SerializeField]
        PopupList m_DiffusionProfile = new PopupList();

        public PopupList diffusionProfile
        {
            get 
            { 
                return m_DiffusionProfile; 
            }
            set
            {
                m_DiffusionProfile = value;
            }
        }

        public DiffusionProfileInputMaterialSlot()
        {
        }

        public DiffusionProfileInputMaterialSlot(int slotId, string displayName, string shaderOutputName,
                                          ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, 0.0f, stageCapability, hidden: hidden)
        {
            var hdPipeline = UnityEngine.Experimental.Rendering.RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                var diffusionProfileSettings = hdPipeline.diffusionProfileSettings;
                m_DiffusionProfile.popupEntries = new List<string>();
                m_DiffusionProfile.popupEntries.Add("None");

                if (!hdPipeline.IsInternalDiffusionProfile(diffusionProfileSettings))
                {
                    var profiles = diffusionProfileSettings.profiles;
                    for (int i = 0; i < profiles.Length; i++)
                    {
                        m_DiffusionProfile.popupEntries.Add(profiles[i].name);
                    }
                    m_DiffusionProfile.selectedEntry = Mathf.Min(m_DiffusionProfile.selectedEntry, profiles.Length + 1);
                }
                else
                {
                    m_DiffusionProfile.selectedEntry = 0;
                }
            }
        }

        public override VisualElement InstantiateControl()
        {
            return new DiffusionProfileSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return m_DiffusionProfile.selectedEntry.ToString();
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as DiffusionProfileInputMaterialSlot;
            if (slot != null)
            {
                diffusionProfile = slot.diffusionProfile;
            }
        }
    }
}
