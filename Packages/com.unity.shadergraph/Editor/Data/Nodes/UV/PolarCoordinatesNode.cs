using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Polar Coordinates")]
    public class PolarCoordinatesNode : CodeFunctionNode
    {
        public PolarCoordinatesNode()
        {
            name = "Polar Coordinates";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Polar-Coordinates-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_PolarCoordinates", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_PolarCoordinates(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(2, Binding.None, 1.0f, 1.0f, 1.0f, 1.0f)] Vector1 RadialScale,
            [Slot(3, Binding.None, 1.0f, 1.0f, 1.0f, 1.0f)] Vector1 LengthScale,
            [Slot(4, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;
            return
                @"
{
    float2 delta = UV - Center;
    {precision} radius = length(delta) * 2 * RadialScale;
    {precision} angle = atan2(delta.x, delta.y) * 1.0/6.28 * LengthScale;
    Out = float2(radius, angle);
}
";
        }
    }
}
