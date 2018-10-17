using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Polygon")]
    public class PolygonNode : CodeFunctionNode
    {
        public PolygonNode()
        {
            name = "Polygon";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Polygon-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Polygon", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Polygon(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 6, 0, 0, 0)] Vector1 Sides,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(3, Binding.None, 0.5f, 0, 0, 0)] Vector1 Height,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision} pi = 3.14159265359;
    {precision} aWidth = Width * cos(pi / Sides);
    {precision} aHeight = Height * cos(pi / Sides);
    {precision}2 uv = (UV * 2 - 1) / {precision}2(aWidth, aHeight);
    uv.y *= -1;
    {precision} pCoord = atan2(uv.x, uv.y);
    {precision} r = 2 * pi / Sides;
    {precision} distance = cos(floor(0.5 + pCoord / r) * r - pCoord) * length(uv);
    Out = saturate((1 - distance) / fwidth(distance));
}
";
        }
    }
}
