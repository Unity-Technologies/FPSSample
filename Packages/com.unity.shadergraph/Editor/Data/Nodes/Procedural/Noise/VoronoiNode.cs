using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Noise", "Voronoi")]
    public class VoronoiNode : CodeFunctionNode
    {
        public VoronoiNode()
        {
            name = "Voronoi";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Voronoi-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Voronoi", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Voronoi(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 2.0f, 0, 0, 0)] Vector1 AngleOffset,
            [Slot(2, Binding.None, 5.0f, 5.0f, 5.0f, 5.0f)] Vector1 CellDensity,
            [Slot(3, Binding.None)] out Vector1 Out,
            [Slot(4, Binding.None)] out Vector1 Cells)
        {
            return
                @"
{
    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);

    for(int y=-1; y<=1; y++)
    {
        for(int x=-1; x<=1; x++)
        {
            float2 lattice = float2(x,y);
            float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset);
            float d = distance(lattice + offset, f);

            if(d < res.x)
            {

                res = float3(d, offset.x, offset.y);
                Out = res.x;
                Cells = res.y;

            }
        }

    }

}
";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction("unity_voronoi_noise_randomVector", s => s.Append(@"
inline float2 unity_voronoi_noise_randomVector (float2 UV, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
}
"));
            base.GenerateNodeFunction(registry, graphContext, generationMode);
        }
    }
}
