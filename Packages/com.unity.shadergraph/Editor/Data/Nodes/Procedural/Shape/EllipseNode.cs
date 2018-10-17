using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Shape", "Ellipse")]
    public class EllipseNode : CodeFunctionNode
    {
        public EllipseNode()
        {
            name = "Ellipse";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Ellipse-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Ellipse", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Ellipse(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(2, Binding.None, 0.5f, 0, 0, 0)] Vector1 Width,
            [Slot(3, Binding.None, 0.5f, 0, 0, 0)] Vector1 Height,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] out Vector1 Out)
        {
            return
                @"
{
    {precision} d = length((UV * 2 - 1) / {precision}2(Width, Height));
    Out = saturate((1 - d) / fwidth(d));
}";
        }
    }
}
