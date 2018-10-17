using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.ShaderGraph
{
    public interface ISubShader
    {
        string GetSubshader(IMasterNode masterNode, GenerationMode mode, List<string> sourceAssetDependencyPaths = null);
        bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset);
    }
}
