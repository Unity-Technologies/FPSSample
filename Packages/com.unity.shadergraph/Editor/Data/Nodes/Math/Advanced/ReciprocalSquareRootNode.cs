using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Reciprocal Square Root")]
    public class ReciprocalSquareRootNode : CodeFunctionNode
    {
        public ReciprocalSquareRootNode()
        {
            name = "Reciprocal Square Root";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Reciprocal-Square-Root-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Rsqrt", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Rsqrt(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = rsqrt(In);
}
";
        }
    }
}
