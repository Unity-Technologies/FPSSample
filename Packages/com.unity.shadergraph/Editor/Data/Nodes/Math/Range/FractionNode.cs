using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Fraction")]
    public class FractionNode : CodeFunctionNode
    {
        public FractionNode()
        {
            name = "Fraction";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Fraction-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Fraction", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Fraction(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = frac(In);
}
";
        }
    }
}
