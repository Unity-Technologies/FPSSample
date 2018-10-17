using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace UnityEditor.ShaderGraph
{
    [CustomEditor(typeof(ShaderSubGraphImporter))]
    public class ShaderSubGraphImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Shader Editor"))
            {
                AssetImporter importer = target as AssetImporter;
                Debug.Assert(importer != null, "importer != null");
                ShaderGraphImporterEditor.ShowGraphEditWindow(importer.assetPath);
            }
        }
    }
}
