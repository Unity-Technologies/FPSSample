using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SerializableMesh
    {
        [SerializeField]
        string m_SerializedMesh;

        [SerializeField]
        string m_Guid;

        [NonSerialized]
        Mesh m_Mesh;

        [Serializable]
        class MeshHelper
        {
#pragma warning disable 649
            public Mesh mesh;
#pragma warning restore 649
        }

        public Mesh mesh
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SerializedMesh))
                {
                    var textureHelper = new MeshHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedMesh, textureHelper);
                    m_SerializedMesh = null;
                    m_Guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(textureHelper.mesh));
                    m_Mesh = textureHelper.mesh;
                }
                else if (!string.IsNullOrEmpty(m_Guid) && m_Mesh == null)
                {
                    m_Mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(m_Guid));
                }

                return m_Mesh;
            }
            set
            {
                m_SerializedMesh = null;
                m_Guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_Mesh = value;
            }
        }
    }
}
