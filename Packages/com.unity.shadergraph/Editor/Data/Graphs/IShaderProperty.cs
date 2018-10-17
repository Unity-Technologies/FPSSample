using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public interface IShaderProperty
    {
        string displayName { get; set; }

        string referenceName { get; }

        PropertyType propertyType { get; }
        Guid guid { get; }
        bool generatePropertyBlock { get; set; }
        Vector4 defaultValue { get; }
        string overrideReferenceName { get; set; }

        string GetPropertyBlockString();
        string GetPropertyDeclarationString(string delimiter = ";");

        string GetPropertyAsArgumentString();

        PreviewProperty GetPreviewMaterialProperty();
        INode ToConcreteNode();
        IShaderProperty Copy();
    }
}
