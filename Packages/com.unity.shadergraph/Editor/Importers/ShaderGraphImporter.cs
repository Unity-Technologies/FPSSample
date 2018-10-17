using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.ShaderGraph
{
    [ScriptedImporter(19, Extension)]
    public class ShaderGraphImporter : ScriptedImporter
    {
        public const string Extension = "shadergraph";

        const string k_ErrorShader = @"
Shader ""Hidden/GraphErrorShader2""
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include ""UnityCG.cginc""

            struct appdata_t {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1,0,1,1);
            }
            ENDCG
        }
    }
    Fallback Off
}";

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var oldShader = AssetDatabase.LoadAssetAtPath<Shader>(ctx.assetPath);
            if (oldShader != null)
                ShaderUtil.ClearShaderErrors(oldShader);

            List<PropertyCollector.TextureInfo> configuredTextures;
            string path = ctx.assetPath;
            var sourceAssetDependencyPaths = new List<string>();
            var text = GetShaderText(path, out configuredTextures, sourceAssetDependencyPaths);
            var shader = ShaderUtil.CreateShaderAsset(text);

            EditorMaterialUtility.SetShaderDefaults(
                shader,
                configuredTextures.Where(x => x.modifiable).Select(x => x.name).ToArray(),
                configuredTextures.Where(x => x.modifiable).Select(x => EditorUtility.InstanceIDToObject(x.textureId) as Texture).ToArray());
            EditorMaterialUtility.SetShaderNonModifiableDefaults(
                shader,
                configuredTextures.Where(x => !x.modifiable).Select(x => x.name).ToArray(),
                configuredTextures.Where(x => !x.modifiable).Select(x => EditorUtility.InstanceIDToObject(x.textureId) as Texture).ToArray());

            ctx.AddObjectToAsset("MainAsset", shader);
            ctx.SetMainObject(shader);

            foreach (var sourceAssetDependencyPath in sourceAssetDependencyPaths.Distinct())
                ctx.DependsOnSourceAsset(sourceAssetDependencyPath);
        }

        internal static string GetShaderText(string path, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths)
        {
            string shaderString = null;
            var shaderName = Path.GetFileNameWithoutExtension(path);
            try
            {
                var textGraph = File.ReadAllText(path, Encoding.UTF8);
                var graph = JsonUtility.FromJson<MaterialGraph>(textGraph);
                graph.LoadedFromDisk();

                if (!string.IsNullOrEmpty(graph.path))
                    shaderName = graph.path + "/" + shaderName;
                shaderString = graph.GetShader(shaderName, GenerationMode.ForReals, out configuredTextures, sourceAssetDependencyPaths);

                if (sourceAssetDependencyPaths != null)
                {
                    foreach (var node in graph.GetNodes<AbstractMaterialNode>())
                        node.GetSourceAssetDependencies(sourceAssetDependencyPaths);
                }
            }
            catch (Exception)
            {
                configuredTextures = new List<PropertyCollector.TextureInfo>();

                // ignored
            }

            return shaderString ?? k_ErrorShader.Replace("Hidden/GraphErrorShader2", shaderName);
        }

        internal static string GetShaderText(string path, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            return GetShaderText(path, out configuredTextures, null);
        }
    }
}
