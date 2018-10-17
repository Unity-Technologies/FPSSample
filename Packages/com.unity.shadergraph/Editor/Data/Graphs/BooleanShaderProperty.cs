using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class BooleanShaderProperty : AbstractShaderProperty<bool>
    {
        public BooleanShaderProperty()
        {
            displayName = "Boolean";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Boolean; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            result.Append("[Toggle] ");
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", Float) = ");
            result.Append(value == true ? 1 : 0);
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("float {0}{1}", referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Boolean)
            {
                name = referenceName,
                booleanValue = value
            };
        }

        public override INode ToConcreteNode()
        {
            return new BooleanNode { value = new ToggleData(value) };
        }

        public override IShaderProperty Copy()
        {
            var copied = new BooleanShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
