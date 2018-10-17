using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class GradientInputMaterialSlot : GradientMaterialSlot, IMaterialSlotHasValue<Gradient>
    {
        [SerializeField]
        Gradient m_Value = new Gradient();

        [SerializeField]
        Gradient m_DefaultValue = new Gradient();

        public GradientInputMaterialSlot()
        {
        }

        public GradientInputMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, stageCapability, hidden)
        {
        }

        public Gradient value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public Gradient defaultValue { get { return m_DefaultValue; } }

        public override VisualElement InstantiateControl()
        {
            return new GradientSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return string.Format("Unity{0}()", matOwner.GetVariableNameForSlot(id));
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            if (generationMode == GenerationMode.Preview)
            {
                properties.AddShaderProperty(new Vector1ShaderProperty()
                {
                    overrideReferenceName = string.Format("{0}_Type", matOwner.GetVariableNameForSlot(id)),
                    value = (int)value.mode,
                    generatePropertyBlock = false
                });

                properties.AddShaderProperty(new Vector1ShaderProperty()
                {
                    overrideReferenceName = string.Format("{0}_ColorsLength", matOwner.GetVariableNameForSlot(id)),
                    value = value.colorKeys.Length,
                    generatePropertyBlock = false
                });

                properties.AddShaderProperty(new Vector1ShaderProperty()
                {
                    overrideReferenceName = string.Format("{0}_AlphasLength", matOwner.GetVariableNameForSlot(id)),
                    value = value.alphaKeys.Length,
                    generatePropertyBlock = false
                });

                for (int i = 0; i < 8; i++)
                {
                    properties.AddShaderProperty(new Vector4ShaderProperty()
                    {
                        overrideReferenceName = string.Format("{0}_ColorKey{1}", matOwner.GetVariableNameForSlot(id), i),
                        value = i < value.colorKeys.Length ? GradientUtils.ColorKeyToVector(value.colorKeys[i]) : Vector4.zero,
                        generatePropertyBlock = false
                    });
                }

                for (int i = 0; i < 8; i++)
                {
                    properties.AddShaderProperty(new Vector4ShaderProperty()
                    {
                        overrideReferenceName = string.Format("{0}_AlphaKey{1}", matOwner.GetVariableNameForSlot(id), i),
                        value = i < value.alphaKeys.Length ? GradientUtils.AlphaKeyToVector(value.alphaKeys[i]) : Vector2.zero,
                        generatePropertyBlock = false
                    });
                }
            }

            var prop = new GradientShaderProperty();
            prop.overrideReferenceName = matOwner.GetVariableNameForSlot(id);
            prop.generatePropertyBlock = false;
            prop.value = value;

            if (generationMode == GenerationMode.Preview)
                prop.OverrideMembers(matOwner.GetVariableNameForSlot(id));

            properties.AddShaderProperty(prop);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            properties.Add(new PreviewProperty(PropertyType.Vector1)
            {
                name = string.Format("{0}_Type", name),
                floatValue = (int)value.mode
            });

            properties.Add(new PreviewProperty(PropertyType.Vector1)
            {
                name = string.Format("{0}_ColorsLength", name),
                floatValue = value.colorKeys.Length
            });

            properties.Add(new PreviewProperty(PropertyType.Vector1)
            {
                name = string.Format("{0}_AlphasLength", name),
                floatValue = value.alphaKeys.Length
            });

            for (int i = 0; i < 8; i++)
            {
                properties.Add(new PreviewProperty(PropertyType.Vector4)
                {
                    name = string.Format("{0}_ColorKey{1}", name, i),
                    vector4Value = i < value.colorKeys.Length ? GradientUtils.ColorKeyToVector(value.colorKeys[i]) : Vector4.zero
                });
            }

            for (int i = 0; i < 8; i++)
            {
                properties.Add(new PreviewProperty(PropertyType.Vector2)
                {
                    name = string.Format("{0}_AlphaKey{1}", name, i),
                    vector4Value = i < value.alphaKeys.Length ? GradientUtils.AlphaKeyToVector(value.alphaKeys[i]) : Vector2.zero
                });
            }
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as GradientInputMaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
