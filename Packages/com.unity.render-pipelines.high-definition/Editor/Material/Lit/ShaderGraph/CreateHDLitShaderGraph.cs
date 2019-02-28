using System.IO;
using UnityEditor.ProjectWindowCallback;

namespace UnityEditor.ShaderGraph
{
    public class CreateHDLitShaderGraph : EndNameEditAction
    {
        [MenuItem("Assets/Create/Shader/Lit Graph", false, 208)]
        public static void CreateMaterialGraph()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateHDLitShaderGraph>(),
                string.Format("New Shader Graph.{0}", ShaderGraphImporter.Extension), null, null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var graph = new MaterialGraph();
            graph.AddNode(new HDLitMasterNode());
            graph.path = "Shader Graphs";
            File.WriteAllText(pathName, EditorJsonUtility.ToJson(graph));
            AssetDatabase.Refresh();
        }
    }
}
