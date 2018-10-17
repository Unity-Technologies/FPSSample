using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class TextureShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        public enum DefaultType
        {
            White, Black, Grey, Bump
        }

        [SerializeField]
        private bool m_Modifiable = true;

        [SerializeField]
        private DefaultType m_DefaultType = TextureShaderProperty.DefaultType.White;

        public TextureShaderProperty()
        {
            value = new SerializableTexture();
            displayName = "Texture2D";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Texture2D; }
        }

        public bool modifiable
        {
            get { return m_Modifiable; }
            set { m_Modifiable = value; }
        }

        public DefaultType defaultType
        {
            get { return m_DefaultType; }
            set { m_DefaultType = value; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            if (!m_Modifiable)
            {
                result.Append("[NonModifiableTextureData] ");
            }
            result.Append("[NoScaleOffset] ");

            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", 2D) = \"" + defaultType.ToString().ToLower() + "\" {}");
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("TEXTURE2D({0}){1} SAMPLER(sampler{0}); float4 {0}_TexelSize{1}", referenceName, delimiter);
        }

        public override string GetPropertyAsArgumentString()
        {
            return string.Format("TEXTURE2D_ARGS({0}, sampler{0})", referenceName);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Texture2D)
            {
                name = referenceName,
                textureValue = value.texture
            };
        }

        public override INode ToConcreteNode()
        {
            return new Texture2DAssetNode { texture = value.texture };
        }

        public override IShaderProperty Copy()
        {
            var copied = new TextureShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
