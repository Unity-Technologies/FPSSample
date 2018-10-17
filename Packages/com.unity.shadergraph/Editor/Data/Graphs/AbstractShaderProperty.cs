using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class AbstractShaderProperty<T> : IShaderProperty
    {
        [SerializeField]
        private T m_Value;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private bool m_GeneratePropertyBlock = true;

        [SerializeField]
        private SerializableGuid m_Guid = new SerializableGuid();

        public T value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public string displayName
        {
            get
            {
                if (string.IsNullOrEmpty(m_Name))
                    return guid.ToString();
                return m_Name;
            }
            set { m_Name = value; }
        }

        string m_DefaultReferenceName;

        public string referenceName
        {
            get
            {
                if (string.IsNullOrEmpty(overrideReferenceName))
                {
                    if (string.IsNullOrEmpty(m_DefaultReferenceName))
                        m_DefaultReferenceName = string.Format("{0}_{1}", propertyType, GuidEncoder.Encode(guid));
                    return m_DefaultReferenceName;
                }
                return overrideReferenceName;
            }
        }

        [SerializeField]
        string m_OverrideReferenceName;

        public string overrideReferenceName
        {
            get { return m_OverrideReferenceName; }
            set { m_OverrideReferenceName = value; }
        }

        public abstract PropertyType propertyType { get; }

        public Guid guid
        {
            get { return m_Guid.guid; }
        }

        public bool generatePropertyBlock
        {
            get { return m_GeneratePropertyBlock; }
            set { m_GeneratePropertyBlock = value; }
        }

        public abstract Vector4 defaultValue { get; }
        public abstract string GetPropertyBlockString();
        public abstract string GetPropertyDeclarationString(string delimiter = ";");

        public virtual string GetPropertyAsArgumentString()
        {
            return GetPropertyDeclarationString(string.Empty);
        }

        public abstract PreviewProperty GetPreviewMaterialProperty();
        public abstract INode ToConcreteNode();
        public abstract IShaderProperty Copy();
    }
}
