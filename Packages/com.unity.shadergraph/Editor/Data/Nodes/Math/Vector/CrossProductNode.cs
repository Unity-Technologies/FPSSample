using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Cross Product")]
    public class CrossProductNode : CodeFunctionNode
    {
        public CrossProductNode()
        {
            name = "Cross Product";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Cross-Product-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_CrossProduct", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_CrossProduct(
            [Slot(0, Binding.None, 0, 0, 0, 0)] Vector3 A,
            [Slot(1, Binding.None, 0, 1, 0, 0)] Vector3 B,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    Out = cross(A, B);
}
";
        }
    }
}
