using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Wave", "Square Wave")]
    class SquareWaveNode : CodeFunctionNode
    {
        public SquareWaveNode()
        {
            name = "Square Wave";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Square-Wave-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("SquareWave", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string SquareWave(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = 1.0 - 2.0 * round(frac(In));
}
";
        }
    }
}
