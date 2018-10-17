using System;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Matrix4ShaderProperty : MatrixShaderProperty
    {
        public Matrix4ShaderProperty()
        {
            displayName = "Matrix4";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Matrix4; }
        }

        public override INode ToConcreteNode()
        {
            return new Matrix4Node
            {
                row0 = new Vector4(value.m00, value.m01, value.m02, value.m03),
                row1 = new Vector4(value.m10, value.m11, value.m12, value.m13),
                row2 = new Vector4(value.m20, value.m21, value.m22, value.m23),
                row3 = new Vector4(value.m30, value.m31, value.m32, value.m33)
            };
        }

        public override IShaderProperty Copy()
        {
            var copied = new Matrix4ShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
