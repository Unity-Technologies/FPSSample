using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.ShaderGraph
{
    public interface IMasterNode : INode
    {
        string GetShader(GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null);
        bool IsPipelineCompatible(RenderPipelineAsset renderPipelineAsset);
    }
}
