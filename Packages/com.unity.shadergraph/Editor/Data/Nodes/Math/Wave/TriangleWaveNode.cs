using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Wave", "Triangle Wave")]
    class TriangleWaveNode : CodeFunctionNode
    {
        public TriangleWaveNode()
        {
            name = "Triangle Wave";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Triangle-Wave-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("TriangleWave", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string TriangleWave(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = 2.0 * abs( 2 * (In - floor(0.5 + In)) ) - 1.0;
}
";
        }
    }
}
