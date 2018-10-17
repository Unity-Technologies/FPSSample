using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Absolute")]
    public class AbsoluteNode : CodeFunctionNode
    {
        public AbsoluteNode()
        {
            name = "Absolute";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Absolute-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Absolute", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Absolute(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = abs(In);
}
";
        }
    }
}
