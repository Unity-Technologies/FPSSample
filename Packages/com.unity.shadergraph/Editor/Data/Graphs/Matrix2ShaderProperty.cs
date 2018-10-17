using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Matrix2ShaderProperty : MatrixShaderProperty
    {
        public Matrix2ShaderProperty()
        {
            displayName = "Matrix2";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Matrix2; }
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return "float4x4 " + referenceName + " = float4x4(1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)" + delimiter;
        }

        public override INode ToConcreteNode()
        {
            return new Matrix2Node
            {
                row0 = new Vector2(value.m00, value.m01),
                row1 = new Vector2(value.m10, value.m11)
            };
        }

        public override IShaderProperty Copy()
        {
            var copied = new Matrix2ShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
