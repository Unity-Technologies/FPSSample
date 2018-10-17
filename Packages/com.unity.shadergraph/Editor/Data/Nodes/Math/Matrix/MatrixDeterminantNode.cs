using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Matrix", "Matrix Determinant")]
    public class MatrixDeterminantNode : CodeFunctionNode
    {
        public MatrixDeterminantNode()
        {
            name = "Matrix Determinant";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Matrix-Determinant-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_MatrixDeterminant", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_MatrixDeterminant(
            [Slot(0, Binding.None)] DynamicDimensionMatrix In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = determinant(In);
}
";
        }
    }
}
