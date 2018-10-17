using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    static class HDIncludePaths
    {
        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            var paths = new string[1];
            paths[0] = Path.GetFullPath("Packages/com.unity.render-pipelines.high-definition");
            return paths;
        }
    }
}
