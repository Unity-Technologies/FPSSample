using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Noise", "Gradient Noise")]
    public class GradientNoiseNode : CodeFunctionNode
    {
        public GradientNoiseNode()
        {
            name = "Gradient Noise";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Gradient-Noise-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_GradientNoise", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_GradientNoise(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 10, 10, 10, 10)] Vector1 Scale,
            [Slot(2, Binding.None)] out Vector1 Out)
        {
            return "{ Out = unity_gradientNoise(UV * Scale) + 0.5; }";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction("unity_gradientNoise_dir", s => s.Append(@"
float2 unity_gradientNoise_dir(float2 p)
{
    // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
    p = p % 289;
    float x = (34 * p.x + 1) * p.x % 289 + p.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}
"));

            registry.ProvideFunction("unity_gradientNoise", s => s.Append(@"
float unity_gradientNoise(float2 p)
{
    float2 ip = floor(p);
    float2 fp = frac(p);
    float d00 = dot(unity_gradientNoise_dir(ip), fp);
    float d01 = dot(unity_gradientNoise_dir(ip + float2(0, 1)), fp - float2(0, 1));
    float d10 = dot(unity_gradientNoise_dir(ip + float2(1, 0)), fp - float2(1, 0));
    float d11 = dot(unity_gradientNoise_dir(ip + float2(1, 1)), fp - float2(1, 1));
    fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
    return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
}
"));

            base.GenerateNodeFunction(registry, graphContext, generationMode);
        }
    }
}
