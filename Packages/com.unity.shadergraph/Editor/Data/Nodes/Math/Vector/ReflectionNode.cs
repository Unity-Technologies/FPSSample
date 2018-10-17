using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Reflection")]
    class ReflectionNode : CodeFunctionNode
    {
        public ReflectionNode()
        {
            name = "Reflection";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Reflection-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Reflection", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Reflection(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector In,
            [Slot(1, Binding.None, 0, 1, 0, 0)] DynamicDimensionVector Normal,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return @"
{
    Out = reflect(In, Normal);
}";
        }
    }
}
