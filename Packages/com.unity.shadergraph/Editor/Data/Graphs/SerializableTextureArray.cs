using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SerializableTextureArray
    {
        [SerializeField]
        string m_SerializedTexture;

        [SerializeField]
        string m_Guid;

        [NonSerialized]
        Texture2DArray m_TextureArray;

        [Serializable]
        class TextureHelper
        {
#pragma warning disable 649
            public Texture2DArray textureArray;
#pragma warning restore 649
        }

        public Texture2DArray textureArray
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SerializedTexture))
                {
                    var textureHelper = new TextureHelper();
                    EditorJsonUtility.FromJsonOverwrite(m_SerializedTexture, textureHelper);
                    m_SerializedTexture = null;
                    m_Guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(textureHelper.textureArray));
                    m_TextureArray = textureHelper.textureArray;
                }
                else if (!string.IsNullOrEmpty(m_Guid) && m_TextureArray == null)
                {
                    m_TextureArray = AssetDatabase.LoadAssetAtPath<Texture2DArray>(AssetDatabase.GUIDToAssetPath(m_Guid));
                }

                return m_TextureArray;
            }
            set
            {
                m_SerializedTexture = null;
                m_Guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
                m_TextureArray = value;
            }
        }
    }
}
