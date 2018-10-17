using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Normal", "Normal Strength")]
    internal class NormalStrengthNode : CodeFunctionNode
    {
        public NormalStrengthNode()
        {
            name = "Normal Strength";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Normal-Strength-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_NormalStrength", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_NormalStrength(
            [Slot(0, Binding.None, 0, 0, 1, 0)] Vector3 In,
            [Slot(1, Binding.None, 1, 1, 1, 1)] Vector1 Strength,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.up;
            return
                @"
{
    Out = {precision}3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}
";
        }
    }
}
