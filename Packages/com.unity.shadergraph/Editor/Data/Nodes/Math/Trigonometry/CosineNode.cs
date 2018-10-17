using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Cosine")]
    public class CosineNode : CodeFunctionNode
    {
        public CosineNode()
        {
            name = "Cosine";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Cosine-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Cosine", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Cosine(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = cos(In);
}
";
        }
    }
}
