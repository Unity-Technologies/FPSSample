using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Sign")]
    public class SignNode : CodeFunctionNode
    {
        public SignNode()
        {
            name = "Sign";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Sign-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Sign", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Sign(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = sign(In);
}
";
        }
    }
}
