using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Tangent")]
    public class TangentNode : CodeFunctionNode
    {
        public TangentNode()
        {
            name = "Tangent";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Tangent-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Tangent", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Tangent(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = tan(In);
}
";
        }
    }
}
