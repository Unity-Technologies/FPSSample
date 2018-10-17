using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Adjustment", "Saturation")]
    public class SaturationNode : CodeFunctionNode
    {
        public SaturationNode()
        {
            name = "Saturation";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Saturation-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Saturation", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Saturation(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None, 1, 1, 1, 1)] Vector1 Saturation,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return @"
{
    {precision} luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
    Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
}
";
        }
    }
}
