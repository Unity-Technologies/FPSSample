using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class MaterialGraph : AbstractMaterialGraph, IShaderGraph
    {
        public IMasterNode masterNode
        {
            get { return GetNodes<INode>().OfType<IMasterNode>().FirstOrDefault(); }
        }

        public string GetShader(string name, GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null)
        {
            return masterNode.GetShader(mode, name, out configuredTextures, sourceAssetDependencyPaths);
        }

        public void LoadedFromDisk()
        {
            OnEnable();
            ValidateGraph();
        }
    }
}
