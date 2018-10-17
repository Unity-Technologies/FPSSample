using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class ColorShaderProperty : AbstractShaderProperty<Color>
    {
        [SerializeField]
        private ColorMode m_ColorMode;

        [SerializeField]
        private bool m_Hidden = false;

        public ColorMode colorMode
        {
            get { return m_ColorMode; }
            set
            {
                if (m_ColorMode == value)
                    return;

                m_ColorMode = value;
            }
        }

        public bool hidden
        {
            get { return m_Hidden; }
            set { m_Hidden = value; }
        }

        public ColorShaderProperty()
        {
            displayName = "Color";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Color; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value.r, value.g, value.b, value.a); }
        }

        public override string GetPropertyBlockString()
        {
            if (!generatePropertyBlock)
                return string.Empty;

            var result = new StringBuilder();
            if (colorMode == ColorMode.HDR)
                result.Append("[HDR]");
            if (m_Hidden)
            {
                result.Append("[HideInInspector] ");
            }
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", Color) = (");
            result.Append(NodeUtils.FloatToShaderValue(value.r));
            result.Append(",");
            result.Append(NodeUtils.FloatToShaderValue(value.g));
            result.Append(",");
            result.Append(NodeUtils.FloatToShaderValue(value.b));
            result.Append(",");
            result.Append(NodeUtils.FloatToShaderValue(value.a));
            result.Append(")");
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("float4 {0}{1}", referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Color)
            {
                name = referenceName,
                colorValue = value
            };
        }

        public override INode ToConcreteNode()
        {
            return new ColorNode { color = new ColorNode.Color(value, colorMode) };
        }

        public override IShaderProperty Copy()
        {
            var copied = new ColorShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            copied.hidden = hidden;
            copied.colorMode = colorMode;
            return copied;
        }
    }
}
