using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Length")]
    public class LengthNode : CodeFunctionNode
    {
        public LengthNode()
        {
            name = "Length";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Length-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Length", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Length(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = length(In);
}
";
        }
    }
}
