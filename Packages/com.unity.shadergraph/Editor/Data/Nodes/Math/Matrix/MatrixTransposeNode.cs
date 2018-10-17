using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Matrix", "Matrix Transpose")]
    public class MatrixTransposeNode : CodeFunctionNode
    {
        public MatrixTransposeNode()
        {
            name = "Matrix Transpose";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Matrix-Transpose-Node"; }
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_MatrixTranspose", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_MatrixTranspose(
            [Slot(0, Binding.None)] DynamicDimensionMatrix In,
            [Slot(1, Binding.None)] out DynamicDimensionMatrix Out)
        {
            return
                @"
{
    Out = transpose(In);
}
";
        }
    }
}
