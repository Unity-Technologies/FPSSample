using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "High Definition Render Pipeline", "Diffusion Profile")]
    public class DiffusionProfileNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public DiffusionProfileNode()
        {
            name = "Diffusion Profile";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            // This still needs to be added.
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Diffusion-Profile-Node"; }
        }

        [SerializeField]
        PopupList m_DiffusionProfile = new PopupList();

        [PopupControl]
        public PopupList diffusionProfile
        {
            get
            {
                return m_DiffusionProfile;
            }
            set
            {
                m_DiffusionProfile = value;
                Dirty(ModificationScope.Node);
            }
        }

        ButtonConfig m_ButtonConfig = new ButtonConfig()
        {
            text = "Goto",
            action = () =>
            {
                var hdPipeline = UnityEngine.Experimental.Rendering.RenderPipelineManager.currentPipeline as HDRenderPipeline;
                if (hdPipeline != null)
                {
                    var diffusionProfileSettings = hdPipeline.diffusionProfileSettings;
                    Selection.activeObject = diffusionProfileSettings;
                }
            }
        };

        [ButtonControl]
        public ButtonConfig buttonConfig
        {
            get
            {
                return m_ButtonConfig;
            }
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
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
                    m_DiffusionProfile.selectedEntry = Mathf.Min(m_DiffusionProfile.selectedEntry, profiles.Length+1);
                }
                else
                {
                    m_DiffusionProfile.selectedEntry = 0;
                    // Need something equivalent, perhaps via implementation of a warning interface for the node.
                    //EditorGUILayout.HelpBox("No diffusion profile Settings have been assigned to the render pipeline asset.", MessageType.Warning);
                }
            }

            AddSlot(new Vector1MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0.0f));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            visitor.AddShaderChunk(precision + " " + GetVariableNameForSlot(0) + " = " + m_DiffusionProfile.selectedEntry + ";", true);
        }
    }
}
