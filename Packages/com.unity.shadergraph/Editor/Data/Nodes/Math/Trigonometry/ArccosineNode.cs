using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Arccosine")]
    public class ArccosineNode : CodeFunctionNode
    {
        public ArccosineNode()
        {
            name = "Arccosine";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Arccosine-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Arccosine", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Arccosine(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = acos(In);
}
";
        }
    }
}
