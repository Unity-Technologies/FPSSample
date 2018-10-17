using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class SamplerStateShaderProperty : AbstractShaderProperty<TextureSamplerState>
    {
        public override PropertyType propertyType
        {
            get { return PropertyType.SamplerState; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override string GetPropertyBlockString()
        {
            return string.Empty;
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format(@"SAMPLER({0}){1}", referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return default(PreviewProperty);
        }

        public override INode ToConcreteNode()
        {
            return new SamplerStateNode();
        }

        public override IShaderProperty Copy()
        {
            var copied = new SamplerStateShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
