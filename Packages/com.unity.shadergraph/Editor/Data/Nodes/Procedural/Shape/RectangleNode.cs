using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Rectangle")]
    public class RectangleNode : CodeFunctionNode
    {
        public RectangleNode()
        {
            name = "Rectangle";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Rectangle-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Rectangle", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Rectangle(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Height,
            [Slot(3, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
                @"
{
    {precision}2 d = abs(UV * 2 - 1) - {precision}2(Width, Height);
    d = 1 - d / fwidth(d);
    Out = saturate(min(d.x, d.y));
}";
        }
    }
}
