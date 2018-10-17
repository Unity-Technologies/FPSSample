using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class CubemapShaderProperty : AbstractShaderProperty<SerializableCubemap>
    {
        [SerializeField]
        private bool m_Modifiable = true;

        public CubemapShaderProperty()
        {
            value = new SerializableCubemap();
            displayName = "Cubemap";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Cubemap; }
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
            result.Append("\", CUBE) = \"\" {}");
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("TEXTURECUBE({0}){1} SAMPLER(sampler{0}){1}", referenceName, delimiter);
        }

        public override string GetPropertyAsArgumentString()
        {
            return string.Format("TEXTURECUBE_ARGS({0}, sampler{0})", referenceName);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Cubemap)
            {
                name = referenceName,
                cubemapValue = value.cubemap
            };
        }

        public override INode ToConcreteNode()
        {
            return new CubemapAssetNode { cubemap = value.cubemap };
        }

        public override IShaderProperty Copy()
        {
            var copied = new CubemapShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
