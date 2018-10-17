using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Tiling And Offset")]
    public class TilingAndOffsetNode : CodeFunctionNode
    {
        public TilingAndOffsetNode()
        {
            name = "Tiling And Offset";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Tiling-And-Offset-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_TilingAndOffset", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_TilingAndOffset(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 1f, 1f, 1f, 1f)] Vector2 Tiling,
            [Slot(2, Binding.None, 0f, 0f, 0f, 0f)] Vector2 Offset,
            [Slot(3, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;
            return
                @"
{
    Out = UV * Tiling + Offset;
}
";
        }
    }
}
