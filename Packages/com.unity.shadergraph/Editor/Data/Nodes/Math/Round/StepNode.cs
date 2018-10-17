using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Step")]
    public class StepNode : CodeFunctionNode
    {
        public StepNode()
        {
            name = "Step";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Step-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Step", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Step(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector Edge,
            [Slot(1, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector In,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = step(Edge, In);
}
";
        }
    }
}
