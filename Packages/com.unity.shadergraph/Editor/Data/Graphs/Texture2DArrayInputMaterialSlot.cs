using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Texture2DArrayInputMaterialSlot : Texture2DArrayMaterialSlot
    {
        [SerializeField]
        private SerializableTextureArray m_TextureArray = new SerializableTextureArray();

        public Texture2DArray textureArray
        {
            get { return m_TextureArray.textureArray; }
            set { m_TextureArray.textureArray = value; }
        }

        public Texture2DArrayInputMaterialSlot()
        {}

        public Texture2DArrayInputMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStageCapability shaderStageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, shaderStageCapability, hidden)
        {}

        public override VisualElement InstantiateControl()
        {
            return new TextureArraySlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return matOwner.GetVariableNameForSlot(id);
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var prop = new Texture2DArrayShaderProperty();
            prop.overrideReferenceName = matOwner.GetVariableNameForSlot(id);
            prop.modifiable = false;
            prop.generatePropertyBlock = true;
            prop.value.textureArray = textureArray;
            properties.AddShaderProperty(prop);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Texture2DArray)
            {
                name = name,
                textureValue = textureArray,
            };
            properties.Add(pp);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Texture2DArrayInputMaterialSlot;
            if (slot != null)
                m_TextureArray = slot.m_TextureArray;
        }
    }
}
