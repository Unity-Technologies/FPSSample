using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    public interface IShaderGraph
    {
        string GetShader(string name, GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null);
        void LoadedFromDisk();
    }
}
