using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public struct PreviewProperty
    {
        public string name { get; set; }
        public PropertyType propType { get; private set; }

        public PreviewProperty(PropertyType type) : this()
        {
            propType = type;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct ClassData
        {
            [FieldOffset(0)]
            public Texture textureValue;
            [FieldOffset(0)]
            public Cubemap cubemapValue;
            [FieldOffset(0)]
            public Gradient gradientValue;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct StructData
        {

            [FieldOffset(0)]
            public Color colorValue;
            [FieldOffset(0)]
            public Vector4 vector4Value;
            [FieldOffset(0)]
            public float floatValue;
            [FieldOffset(0)]
            public bool booleanValue;
        }

        ClassData m_ClassData;
        StructData m_StructData;

        public Color colorValue
        {
            get
            {
                if (propType != PropertyType.Color)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Color, propType));
                return m_StructData.colorValue;
            }
            set
            {
                if (propType != PropertyType.Color)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Color, propType));
                m_StructData.colorValue = value;
            }
        }

        public Texture textureValue
        {
            get
            {
                if (propType != PropertyType.Texture2D && propType != PropertyType.Texture2DArray && propType != PropertyType.Texture3D)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Texture2D, propType));
                return m_ClassData.textureValue;
            }
            set
            {
                if (propType != PropertyType.Texture2D && propType != PropertyType.Texture2DArray && propType != PropertyType.Texture3D)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Texture2D, propType));
                m_ClassData.textureValue = value;
            }
        }

        public Cubemap cubemapValue
        {
            get
            {
                if (propType != PropertyType.Cubemap)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Cubemap, propType));
                return m_ClassData.cubemapValue;
            }
            set
            {
                if (propType != PropertyType.Cubemap)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Cubemap, propType));
                m_ClassData.cubemapValue = value;
            }
        }

        public Gradient gradientValue
        {
            get
            {
                if (propType != PropertyType.Gradient)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Gradient, propType));
                return m_ClassData.gradientValue;
            }
            set
            {
                if (propType != PropertyType.Gradient)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Gradient, propType));
                m_ClassData.gradientValue = value;
            }
        }

        public Vector4 vector4Value
        {
            get
            {
                if (propType != PropertyType.Vector2 && propType != PropertyType.Vector3 && propType != PropertyType.Vector4)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Vector4, propType));
                return m_StructData.vector4Value;
            }
            set
            {
                if (propType != PropertyType.Vector2 && propType != PropertyType.Vector3 && propType != PropertyType.Vector4
                    && propType != PropertyType.Matrix2 && propType != PropertyType.Matrix3 && propType != PropertyType.Matrix4)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Vector4, propType));
                m_StructData.vector4Value = value;
            }
        }

        public float floatValue
        {
            get
            {
                if (propType != PropertyType.Vector1)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Vector1, propType));
                return m_StructData.floatValue;
            }
            set
            {
                if (propType != PropertyType.Vector1)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Vector1, propType));
                m_StructData.floatValue = value;
            }
        }

        public bool booleanValue
        {
            get
            {
                if (propType != PropertyType.Boolean)
                    throw new ArgumentException(string.Format(k_GetErrorMessage, PropertyType.Boolean, propType));
                return m_StructData.booleanValue;
            }
            set
            {
                if (propType != PropertyType.Boolean)
                    throw new ArgumentException(string.Format(k_SetErrorMessage, PropertyType.Boolean, propType));
                m_StructData.booleanValue = value;
            }
        }

        const string k_SetErrorMessage = "Cannot set a {0} property on a PreviewProperty with type {1}.";
        const string k_GetErrorMessage = "Cannot get a {0} property on a PreviewProperty with type {1}.";

        public void SetMaterialPropertyBlockValue(MaterialPropertyBlock block)
        {
            if ((propType == PropertyType.Texture2D || propType == PropertyType.Texture2DArray || propType == PropertyType.Texture3D) && textureValue != null)
                block.SetTexture(name, m_ClassData.textureValue);
            else if (propType == PropertyType.Cubemap && cubemapValue != null)
                block.SetTexture(name, m_ClassData.cubemapValue);
            else if (propType == PropertyType.Color)
                block.SetColor(name, m_StructData.colorValue);
            else if (propType == PropertyType.Vector2 || propType == PropertyType.Vector3 || propType == PropertyType.Vector4)
                block.SetVector(name, m_StructData.vector4Value);
            else if (propType == PropertyType.Vector1)
                block.SetFloat(name, m_StructData.floatValue);
            else if (propType == PropertyType.Boolean)
                block.SetFloat(name, m_StructData.booleanValue ? 1 : 0);
        }
    }

    public static class PreviewPropertyExtensions
    {
        public static void SetPreviewProperty(this MaterialPropertyBlock block, PreviewProperty previewProperty)
        {
            previewProperty.SetMaterialPropertyBlockValue(block);
        }
    }
}
