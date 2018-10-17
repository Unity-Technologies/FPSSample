using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "One Minus")]
    public class OneMinusNode : CodeFunctionNode
    {
        public OneMinusNode()
        {
            name = "One Minus";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/One-Minus-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_OneMinus", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_OneMinus(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = 1 - In;
}
";
        }
    }
}
