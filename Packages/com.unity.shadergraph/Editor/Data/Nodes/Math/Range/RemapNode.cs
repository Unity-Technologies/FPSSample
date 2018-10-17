using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Remap")]
    public class RemapNode : CodeFunctionNode
    {
        public RemapNode()
        {
            name = "Remap";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Remap-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Remap", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Remap(
            [Slot(0, Binding.None, -1, -1, -1, -1)] DynamicDimensionVector In,
            [Slot(1, Binding.None, -1, 1, 0, 0)] Vector2 InMinMax,
            [Slot(2, Binding.None, 0, 1, 0, 0)] Vector2 OutMinMax,
            [Slot(3, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}
";
        }
    }
}
