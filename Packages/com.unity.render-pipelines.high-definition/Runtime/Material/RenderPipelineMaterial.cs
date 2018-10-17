using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class RenderPipelineMaterial : Object
    {
        // GBuffer management
        public virtual bool IsDefferedMaterial() { return false; }
        public virtual int GetMaterialGBufferCount(HDRenderPipelineAsset asset) { return 0; }
        public virtual void GetMaterialGBufferDescription(HDRenderPipelineAsset asset, out RenderTextureFormat[] RTFormat, out bool[] sRGBFlag, out GBufferUsage[] gBufferUsage, out bool[] enableWrite)
        {
            RTFormat = null;
            sRGBFlag = null;
            gBufferUsage = null;
            enableWrite = null;
        }

        // Regular interface
        public virtual void Build(HDRenderPipelineAsset hdAsset) {}
        public virtual void Cleanup() {}

        // Following function can be use to initialize GPU resource (once or each frame) and bind them
        public virtual void RenderInit(CommandBuffer cmd) {}
        public virtual void Bind() {}
    }
}
