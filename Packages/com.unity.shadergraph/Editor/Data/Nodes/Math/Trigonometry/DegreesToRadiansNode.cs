using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Degrees To Radians")]
    public class DegreesToRadiansNode : CodeFunctionNode
    {
        public DegreesToRadiansNode()
        {
            name = "Degrees To Radians";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Degrees-To-Radians-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DegreesToRadians", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DegreesToRadians(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = radians(In);
}
";
        }
    }
}
