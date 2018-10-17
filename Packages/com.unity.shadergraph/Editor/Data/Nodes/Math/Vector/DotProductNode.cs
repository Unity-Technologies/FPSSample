using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Dot Product")]
    public class DotProductNode : CodeFunctionNode
    {
        public DotProductNode()
        {
            name = "Dot Product";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Dot-Product-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DotProduct", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DotProduct(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector A,
            [Slot(1, Binding.None, 0, 1, 0, 0)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = dot(A, B);
}
";
        }
    }
}
