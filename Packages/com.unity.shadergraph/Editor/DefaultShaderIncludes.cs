using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    internal static class DefaultShaderIncludes
    {
        public static string GetAssetsPackagePath()
        {
            var packageDirectories = Directory.GetDirectories(Application.dataPath, "com.unity.shadergraph", SearchOption.AllDirectories);
            return packageDirectories.Length == 0 ? null : Path.GetFullPath(packageDirectories.First());
        }

        public static string GetDebugOutputPath()
        {
            var path = Application.dataPath;
            if (path == null)
                return null;
            path = Path.Combine(path, "DebugOutput");
            return Directory.Exists(path) ? path : null;
        }

        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            return new[]
            {
                GetAssetsPackagePath() ?? Path.GetFullPath("Packages/com.unity.shadergraph")
            };
        }
    }
}
