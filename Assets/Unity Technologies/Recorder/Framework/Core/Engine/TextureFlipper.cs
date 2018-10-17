using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Recorder
{
    public class TextureFlipper : IDisposable
    {
        Shader          m_shVFlip;
        Material        m_VFLipMaterial;
        RenderTexture   m_WorkTexture;

        public TextureFlipper()
        {
            m_shVFlip = Shader.Find("Hidden/Unity/Recorder/Custom/VerticalFlipper");
            m_VFLipMaterial = new Material(m_shVFlip);
        }

        public void Flip(RenderTexture target)
        {
            if (m_WorkTexture == null || m_WorkTexture.width != target.width || m_WorkTexture.height != target.height)
            {
                UnityHelpers.Destroy(m_WorkTexture);
                m_WorkTexture = new RenderTexture(target.width, target.height, target.depth, target.format, RenderTextureReadWrite.Linear);
            }
            Graphics.Blit( target, m_WorkTexture, m_VFLipMaterial );
            Graphics.Blit( m_WorkTexture, target );            
        }

        public void Dispose()
        {
            UnityHelpers.Destroy(m_WorkTexture);
            m_WorkTexture = null;
            UnityHelpers.Destroy(m_VFLipMaterial);
            m_VFLipMaterial = null;
        }

    }
}
