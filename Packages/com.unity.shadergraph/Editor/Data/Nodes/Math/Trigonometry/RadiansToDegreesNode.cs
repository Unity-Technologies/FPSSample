using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Radians To Degrees")]
    public class RadiansToDegreesNode : CodeFunctionNode
    {
        public RadiansToDegreesNode()
        {
            name = "Radians To Degrees";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Radians-To-Degrees-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RadiansToDegrees", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RadiansToDegrees(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = degrees(In);
}
";
        }
    }
}
