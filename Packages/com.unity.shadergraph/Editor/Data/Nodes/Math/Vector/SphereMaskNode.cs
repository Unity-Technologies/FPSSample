using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Sphere Mask")]
    public class SphereMaskNode : CodeFunctionNode
    {
        public SphereMaskNode()
        {
            name = "Sphere Mask";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Sphere-Mask-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("SphereMask", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string SphereMask(
            [Slot(0, Binding.None)] DynamicDimensionVector Coords,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] DynamicDimensionVector Center,
            [Slot(2, Binding.None, 0.1f, 0.1f, 0.1f, 0.1f)] Vector1 Radius,
            [Slot(3, Binding.None, 0.8f, 0.8f, 0.8f, 0.8f)] Vector1 Hardness,
            [Slot(4, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
	Out = 1 - saturate((distance(Coords, Center) - Radius) / (1 - Hardness));
}
";
        }
    }
}
