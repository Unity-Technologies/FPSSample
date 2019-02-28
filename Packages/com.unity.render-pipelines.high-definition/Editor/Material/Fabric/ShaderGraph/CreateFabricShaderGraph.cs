using System.IO;
using UnityEditor.ProjectWindowCallback;

namespace UnityEditor.ShaderGraph
{
    public class CreateFabricShaderGraph : EndNameEditAction
    {
        [MenuItem("Assets/Create/Shader/Fabric Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateFabricShaderGraph>(),
                "New Shader Graph.ShaderGraph", null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new MaterialGraph();
            graph.AddNode(new FabricMasterNode());
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
        }
    }
}
