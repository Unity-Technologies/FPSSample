using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "Is NaN")]
    public class IsNanNode : CodeFunctionNode
    {
        public IsNanNode()
        {
            name = "Is NaN";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Is-NaN-Node"; }
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_IsNaN", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_IsNaN(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = (In < 0.0 || In > 0.0 || In == 0.0) ? 0 : 1;
}
";
        }
    }
}
