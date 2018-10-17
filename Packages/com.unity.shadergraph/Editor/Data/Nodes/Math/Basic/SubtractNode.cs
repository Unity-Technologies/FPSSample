using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Subtract")]
    class SubtractNode : CodeFunctionNode
    {
        public SubtractNode()
        {
            name = "Subtract";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Subtract-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Subtract", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Subtract(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector A,
            [Slot(1, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = A - B;
}
";
        }
    }
}
