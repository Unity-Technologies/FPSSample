using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal class SkyUpdateContext
    {
        SkySettings m_SkySettings;
        SkyRenderer m_Renderer;

        public int      skyParametersHash = -1;
        public float    currentUpdateTime = 0.0f;
        public int      updatedFramesRequired = 1; // The first frame after the scene load is currently not rendered correctly

        public SkySettings skySettings
        {
            get { return m_SkySettings; }
            set
            {
                if (m_SkySettings == value)
                    return;

                if (m_Renderer != null)
                {
                    m_Renderer.Cleanup();
                    m_Renderer = null;
                }

                skyParametersHash = -1;
                m_SkySettings = value;
                updatedFramesRequired = 1;
                currentUpdateTime = 0.0f;

                if (value != null)
                {
                    m_Renderer = value.CreateRenderer();
                    m_Renderer.Build();
                }
            }
        }

        public SkyRenderer renderer { get { return m_Renderer; } }

        public bool IsValid()
        {
            // We need to check m_SkySettings in addition to the renderer because it can be "nulled" when destroying the volume containing the settings (as it's a ScriptableObject) without the context knowing about it.
            return m_Renderer != null && m_Renderer.IsValid() && m_SkySettings != null;
        }

        public void Cleanup()
        {
            if (m_Renderer != null)
                m_Renderer.Cleanup();
        }
    }
}
