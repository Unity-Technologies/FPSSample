using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Truncate")]
    public class TruncateNode : CodeFunctionNode
    {
        public TruncateNode()
        {
            name = "Truncate";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Truncate-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Truncate", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Truncate(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = trunc(In);
}
";
        }
    }
}
