using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "Or")]
    public class OrNode : CodeFunctionNode
    {
        public OrNode()
        {
            name = "Or";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Or-Node"; }
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Or", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Or(
            [Slot(0, Binding.None)] Boolean A,
            [Slot(1, Binding.None)] Boolean B,
            [Slot(2, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = A || B;
}
";
        }
    }
}
