using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "Branch")]
    public class BranchNode : CodeFunctionNode
    {
        public BranchNode()
        {
            name = "Branch";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Branch-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Branch", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Branch(
            [Slot(0, Binding.None)] Boolean Predicate,
            [Slot(1, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector True,
            [Slot(2, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector False,
            [Slot(3, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = lerp(False, True, Predicate);
}
";
        }
    }
}
