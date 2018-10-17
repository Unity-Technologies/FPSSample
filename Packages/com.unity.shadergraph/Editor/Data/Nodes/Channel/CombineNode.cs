using System;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel", "Combine")]
    public class CombineNode : CodeFunctionNode
    {
        public CombineNode()
        {
            name = "Combine";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Combine-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Combine", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Combine(
            [Slot(0, Binding.None)] Vector1 R,
            [Slot(1, Binding.None)] Vector1 G,
            [Slot(2, Binding.None)] Vector1 B,
            [Slot(3, Binding.None)] Vector1 A,
            [Slot(4, Binding.None)] out Vector4 RGBA,
            [Slot(5, Binding.None)] out Vector3 RGB,
            [Slot(6, Binding.None)] out Vector2 RG)
        {
            RGBA = Vector4.zero;
            RGB = Vector3.zero;
            RG = Vector2.zero;
            return @"
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}
";
        }
    }
}
