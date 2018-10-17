using UnityEngine;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Wave", "Noise Sine Wave")]
    class NoiseSineWaveNode : CodeFunctionNode
    {
        public NoiseSineWaveNode()
        {
            name = "Noise Sine Wave";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Noise-Sine-Wave-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("NoiseSineWave", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string NoiseSineWave(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None, -0.5f, 0.5f, 1, 1)] Vector2 MinMax,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision} sinIn = sin(In);
    {precision} sinInOffset = sin(In + 1.0);
    {precision} randomno =  frac(sin((sinIn - sinInOffset) * (12.9898 + 78.233))*43758.5453);
    {precision} noise = lerp(MinMax.x, MinMax.y, randomno);
    Out = sinIn + noise;
}
";
        }
    }
}
