using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Logic", "Not")]
    public class NotNode : CodeFunctionNode
    {
        public NotNode()
        {
            name = "Not";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Not-Node"; }
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Not", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Not(
            [Slot(0, Binding.None)] Boolean In,
            [Slot(1, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = !In;
}
";
        }
    }
}
