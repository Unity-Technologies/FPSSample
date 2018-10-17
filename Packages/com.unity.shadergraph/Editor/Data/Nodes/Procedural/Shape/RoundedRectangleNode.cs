using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Rounded Rectangle")]
    public class RoundedRectangleNode : CodeFunctionNode
    {
        public RoundedRectangleNode()
        {
            name = "Rounded Rectangle";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Rounded-Rectangle-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RoundedRectangle", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RoundedRectangle(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Height,
            [Slot(3, Binding.None, 0.1f, 0, 0, 0)] Vector1 Radius,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
                @"
{
    Radius = max(min(min(abs(Radius * 2), abs(Width)), abs(Height)), 1e-5);
    {precision}2 uv = abs(UV * 2 - 1) - {precision}2(Width, Height) + Radius;
    {precision} d = length(max(0, uv)) / Radius;
    Out = saturate((1 - d) / fwidth(d));
}";
        }
    }
}
