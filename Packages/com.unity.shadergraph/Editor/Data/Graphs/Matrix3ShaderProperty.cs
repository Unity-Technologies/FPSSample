using System;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Matrix3ShaderProperty : MatrixShaderProperty
    {
        public Matrix3ShaderProperty()
        {
            displayName = "Matrix3";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Matrix3; }
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return "float4x4 " + referenceName + " = float4x4(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0)" + delimiter;
        }

        public override INode ToConcreteNode()
        {
            return new Matrix3Node
            {
                row0 = new Vector3(value.m00, value.m01, value.m02),
                row1 = new Vector3(value.m10, value.m11, value.m12),
                row2 = new Vector3(value.m20, value.m21, value.m22)
            };
        }

        public override IShaderProperty Copy()
        {
            var copied = new Matrix3ShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
