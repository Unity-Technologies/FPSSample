using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Interpolation", "Smoothstep")]
    class SmoothstepNode : CodeFunctionNode
    {
        public SmoothstepNode()
        {
            name = "Smoothstep";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Smoothstep-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Smoothstep", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Smoothstep(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector Edge1,
            [Slot(1, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector Edge2,
            [Slot(2, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector In,
            [Slot(3, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = smoothstep(Edge1, Edge2, In);
}";
        }
    }
}
