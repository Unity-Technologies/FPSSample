using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Texture 3D Asset")]
    public class Texture3DAssetNode : AbstractMaterialNode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;

        const string kOutputSlotName = "Out";

        public Texture3DAssetNode()
        {
            name = "Texture 3D Asset";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Texture-3D-Asset-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture3DMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [SerializeField]
        private SerializableTexture m_Texture = new SerializableTexture();

        [Texture3DControl("")]
        public Texture3D texture
        {
            get { return (Texture3D)m_Texture.texture; }
            set
            {
                if (m_Texture.texture == value)
                    return;
                m_Texture.texture = value;
                Dirty(ModificationScope.Node);
            }
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Texture3DShaderProperty()
            {
                overrideReferenceName = GetVariableNameForSlot(OutputSlotId),
                generatePropertyBlock = true,
                value = m_Texture,
                modifiable = false
            });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Texture3D)
            {
                name = GetVariableNameForSlot(OutputSlotId),
                textureValue = texture
            });
        }

        public IShaderProperty AsShaderProperty()
        {
            var prop = new Texture3DShaderProperty { value = m_Texture };
            if (texture != null)
                prop.displayName = texture.name;
            return prop;
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
