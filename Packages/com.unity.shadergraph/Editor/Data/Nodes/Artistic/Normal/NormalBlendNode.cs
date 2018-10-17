using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Normal", "Normal Blend")]
    public class NormalBlendNode : CodeFunctionNode
    {
        public NormalBlendNode()
        {
            name = "Normal Blend";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Normal-Blend-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_NormalBlend", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_NormalBlend(
            [Slot(0, Binding.None, 0, 0, 1, 0)] Vector3 A,
            [Slot(1, Binding.None, 0, 0, 1, 0)] Vector3 B,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;

            return @"
{
    Out = normalize({precision}3(A.rg + B.rg, A.b * B.b));
}
";
        }
    }
}
