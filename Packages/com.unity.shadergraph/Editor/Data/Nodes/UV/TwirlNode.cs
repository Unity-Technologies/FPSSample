using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UV", "Twirl")]
    public class TwirlNode : CodeFunctionNode
    {
        public TwirlNode()
        {
            name = "Twirl";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Twirl-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Twirl", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Twirl(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(2, Binding.None, 10f, 0f, 0f, 0f)] Vector1 Strength,
            [Slot(3, Binding.None)] Vector2 Offset,
            [Slot(4, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;

            return
                @"
{
    float2 delta = UV - Center;
    {precision} angle = Strength * length(delta);
    {precision} x = cos(angle) * delta.x - sin(angle) * delta.y;
    {precision} y = sin(angle) * delta.x + cos(angle) * delta.y;
    Out = float2(x + Center.x + Offset.x, y + Center.y + Offset.y);
}
";
        }
    }
}
