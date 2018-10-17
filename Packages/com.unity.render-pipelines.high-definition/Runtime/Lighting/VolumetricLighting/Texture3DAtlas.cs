using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class Texture3DAtlas 
    {
        private List<Texture3D> m_textures = new List<Texture3D>();

        private Texture3D m_atlas;
        private TextureFormat m_format;

        private bool m_updateAtlas = false;
        private int m_atlasSize = 0;

        public delegate void AtlasUpdated();
        public AtlasUpdated OnAtlasUpdated = null;


        void NotifyAtlasUpdated()
        {
            if (OnAtlasUpdated != null)
            {
                OnAtlasUpdated();
            }
        }

        public Texture3DAtlas(TextureFormat format, int textureSize)
        {
            m_format = format;
            m_atlasSize = textureSize;
        }

        public void AddTexture(Texture3D tex)
        {
            if (tex.width != m_atlasSize || tex.height != m_atlasSize || tex.depth != m_atlasSize)
            {
                Debug.LogError(String.Format("3D Texture Atlas: Added texture {4} size {0}x{1}x{2} does not match size of atlas {3}x{3}x{3}", tex.width, tex.height, tex.depth, m_atlasSize, tex.name));
                return;
            }

            if (tex.format != m_format) 
            {
                Debug.LogError(String.Format("3D Texture Atlas: Added texture {2} format {0} does not match format of atlas {1}", tex.format, m_format, tex.name));
                return;
            }

            m_textures.Add(tex);

            m_updateAtlas = true;
        }

        public void RemoveTexture(Texture3D tex)
        {
            if (m_textures.Contains(tex)) 
            {
                m_textures.Remove(tex); 
                m_updateAtlas = true; 
            }
        }

        public void ClearTextures()
        {
            m_textures.Clear();
            m_updateAtlas = true;
        }

        public int GetTextureIndex(Texture3D tex)
        {
            return m_textures.IndexOf(tex);
        }

        public void GenerateAtlas(CommandBuffer cmd)
        {
            if (!m_updateAtlas)
            {
                return;
            }

            if (m_textures.Count > 0)
            {
                int textureSliceSize = m_atlasSize * m_atlasSize * m_atlasSize;
                int totalTextureSize = textureSliceSize * m_textures.Count;

                Color [] colorData = new Color[totalTextureSize];
                m_atlas = new Texture3D(m_atlasSize, m_atlasSize, m_atlasSize * m_textures.Count, m_format, true);
                
                //Iterate through all the textures and append their texture data to the texture array
                //Once CopyTexture works for 3D textures we can replace this with a series of copy texture calls 
                for (int i = 0; i < m_textures.Count; i++)
                {
                    Texture3D tex = m_textures[i];
                    Color [] texData = tex.GetPixels();
                    Array.Copy(texData, 0, colorData, textureSliceSize * i, texData.Length);
                }

                m_atlas.SetPixels(colorData);
                m_atlas.Apply();
            }
            else
            {
                m_atlas = null;
            }

            NotifyAtlasUpdated();

            m_updateAtlas = false;
        }

        public Texture3D GetAtlas()
        {
            return m_atlas;
        }
    }
}
