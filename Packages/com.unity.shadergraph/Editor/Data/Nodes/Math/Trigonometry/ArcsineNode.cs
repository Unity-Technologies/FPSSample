using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Arcsine")]
    public class ArcsineNode : CodeFunctionNode
    {
        public ArcsineNode()
        {
            name = "Arcsine";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Arcsine-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Arcsine", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Arcsine(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = asin(In);
}
";
        }
    }
}
