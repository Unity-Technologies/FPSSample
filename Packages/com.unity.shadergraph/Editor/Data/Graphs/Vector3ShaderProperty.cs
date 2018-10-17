using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Vector3ShaderProperty : VectorShaderProperty
    {
        public Vector3ShaderProperty()
        {
            displayName = "Vector3";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector3; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value.x, value.y, value.z, 0); }
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("float3 {0}{1}", referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Vector3)
            {
                name = referenceName,
                vector4Value = value
            };
        }

        public override INode ToConcreteNode()
        {
            var node = new Vector3Node();
            node.FindInputSlot<Vector1MaterialSlot>(Vector3Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1MaterialSlot>(Vector3Node.InputSlotYId).value = value.y;
            node.FindInputSlot<Vector1MaterialSlot>(Vector3Node.InputSlotZId).value = value.z;
            return node;
        }

        public override IShaderProperty Copy()
        {
            var copied = new Vector3ShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
