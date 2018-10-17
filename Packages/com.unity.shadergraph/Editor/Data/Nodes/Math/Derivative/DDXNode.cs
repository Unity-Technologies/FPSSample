using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Derivative", "DDX")]
    public class DDXNode : CodeFunctionNode
    {
        public DDXNode()
        {
            name = "DDX";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/DDX-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DDX", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DDX(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = ddx(In);
}
";
        }
    }
}
