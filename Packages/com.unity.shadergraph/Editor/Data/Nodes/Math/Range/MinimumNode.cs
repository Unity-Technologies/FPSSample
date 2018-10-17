using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Minimum")]
    public class MinimumNode : CodeFunctionNode
    {
        public MinimumNode()
        {
            name = "Minimum";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Minimum-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Minimum", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Minimum(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector A,
            [Slot(1, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = min(A, B);
};";
        }
    }
}
