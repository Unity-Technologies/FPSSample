using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Texture3DShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        [SerializeField]
        private bool m_Modifiable = true;

        public Texture3DShaderProperty()
        {
            value = new SerializableTexture();
            displayName = "Texture3D";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Texture3D; }
        }

        public bool modifiable
        {
            get { return m_Modifiable; }
            set { m_Modifiable = value; }
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
            result.Append("\", 3D) = \"white\" {}");
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("TEXTURE3D({0}){1} SAMPLER(sampler{0}){1}", referenceName, delimiter);
        }

        public override string GetPropertyAsArgumentString()
        {
            return string.Format("TEXTURE3D_ARGS({0}, sampler{0})", referenceName);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Texture3D)
            {
                name = referenceName,
                textureValue = value.texture
            };
        }

        public override INode ToConcreteNode()
        {
            return new Texture3DAssetNode { texture = (Texture3D)value.texture };
        }

        public override IShaderProperty Copy()
        {
            var copied = new Texture3DShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
