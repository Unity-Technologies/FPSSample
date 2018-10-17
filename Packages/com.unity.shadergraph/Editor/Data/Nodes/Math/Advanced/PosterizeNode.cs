using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Posterize")]
    class PosterizeNode : CodeFunctionNode
    {
        public PosterizeNode()
        {
            name = "Posterize";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Posterize-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Posterize", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Posterize(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector In,
            [Slot(1, Binding.None, 4, 4, 4, 4)] DynamicDimensionVector Steps,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = floor(In / (1 / Steps)) * (1 / Steps);
}
";
        }
    }
}
