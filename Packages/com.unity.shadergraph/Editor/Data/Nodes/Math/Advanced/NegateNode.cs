using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Negate")]
    public class NegateNode : CodeFunctionNode
    {
        public NegateNode()
        {
            name = "Negate";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Negate-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Negate", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Negate(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = -1 * In;
}
";
        }
    }
}
