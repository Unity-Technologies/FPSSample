using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Adjustment", "Replace Color")]
    public class ReplaceColorNode : CodeFunctionNode
    {
        public ReplaceColorNode()
        {
            name = "Replace Color";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Replace-Color-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ReplaceColor", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ReplaceColor(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] ColorRGB From,
            [Slot(2, Binding.None)] ColorRGB To,
            [Slot(3, Binding.None)] Vector1 Range,
            [Slot(5, Binding.None)] Vector1 Fuzziness,
            [Slot(4, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    {precision} Distance = distance(From, In);
    Out = lerp(To, In, saturate((Distance - Range) / max(Fuzziness, 1e-5f)));
}";
        }
    }
}
