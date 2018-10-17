using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing
{
    public interface INodeModificationListener
    {
        void OnNodeModified(ModificationScope scope);
    }
}
