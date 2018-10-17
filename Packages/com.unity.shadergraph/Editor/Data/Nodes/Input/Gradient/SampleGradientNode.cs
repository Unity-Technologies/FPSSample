using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Gradient", "Sample Gradient")]
    public class SampleGradient : CodeFunctionNode, IGeneratesBodyCode
    {
        public SampleGradient()
        {
            name = "Sample Gradient";
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_SampleGradient", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SampleGradient(
            [Slot(0, Binding.None)] Gradient Gradient,
            [Slot(1, Binding.None)] Vector1 Time,
            [Slot(2, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;
            return
                @"
{
    {precision}3 color = Gradient.colors[0].rgb;
    [unroll]
    for (int c = 1; c < 8; c++)
    {
        {precision} colorPos = saturate((Time - Gradient.colors[c-1].w) / (Gradient.colors[c].w - Gradient.colors[c-1].w)) * step(c, Gradient.colorsLength-1);
        color = lerp(color, Gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), Gradient.type));
    }
#ifndef UNITY_COLORSPACE_GAMMA
    color = SRGBToLinear(color);
#endif
    {precision} alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < 8; a++)
    {
        {precision} alphaPos = saturate((Time - Gradient.alphas[a-1].y) / (Gradient.alphas[a].y - Gradient.alphas[a-1].y)) * step(a, Gradient.alphasLength-1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type));
    }
    Out = {precision}4(color, alpha);
}
";
        }
    }
}
