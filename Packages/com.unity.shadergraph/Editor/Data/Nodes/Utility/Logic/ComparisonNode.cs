using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum ComparisonType
    {
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual
    };

    [Title("Utility", "Logic", "Comparison")]
    public class ComparisonNode : CodeFunctionNode
    {
        public ComparisonNode()
        {
            name = "Comparison";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Comparison-Node"; }
        }

        [SerializeField]
        private ComparisonType m_ComparisonType = ComparisonType.Equal;

        [EnumControl("")]
        public ComparisonType comparisonType
        {
            get { return m_ComparisonType; }
            set
            {
                if (m_ComparisonType == value)
                    return;

                m_ComparisonType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (comparisonType)
            {
                case ComparisonType.NotEqual:
                    return GetType().GetMethod("Unity_Comparison_NotEqual", BindingFlags.Static | BindingFlags.NonPublic);
                case ComparisonType.Less:
                    return GetType().GetMethod("Unity_Comparison_Less", BindingFlags.Static | BindingFlags.NonPublic);
                case ComparisonType.LessOrEqual:
                    return GetType().GetMethod("Unity_Comparison_LessOrEqual", BindingFlags.Static | BindingFlags.NonPublic);
                case ComparisonType.Greater:
                    return GetType().GetMethod("Unity_Comparison_Greater", BindingFlags.Static | BindingFlags.NonPublic);
                case ComparisonType.GreaterOrEqual:
                    return GetType().GetMethod("Unity_Comparison_GreaterOrEqual", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    return GetType().GetMethod("Unity_Comparison_Equal", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_Comparison_Equal(
            [Slot(0, Binding.None)] Vector1 A,
            [Slot(1, Binding.None)] Vector1 B,
            [Slot(2, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = A == B ? 1 : 0;
}
";
        }

        static string Unity_Comparison_NotEqual(
            [Slot(0, Binding.None)] Vector1 A,
            [Slot(1, Binding.None)] Vector1 B,
            [Slot(2, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = A != B ? 1 : 0;
}
";
        }

        static string Unity_Comparison_Less(
            [Slot(0, Binding.None)] Vector1 A,
            [Slot(1, Binding.None)] Vector1 B,
            [Slot(2, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = A < B ? 1 : 0;
}
";
        }

        static string Unity_Comparison_LessOrEqual(
            [Slot(0, Binding.None)] Vector1 A,
            [Slot(1, Binding.None)] Vector1 B,
            [Slot(2, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = A <= B ? 1 : 0;
}
";
        }

        static string Unity_Comparison_Greater(
            [Slot(0, Binding.None)] Vector1 A,
            [Slot(1, Binding.None)] Vector1 B,
            [Slot(2, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = A > B ? 1 : 0;
}
";
        }

        static string Unity_Comparison_GreaterOrEqual(
            [Slot(0, Binding.None)] Vector1 A,
            [Slot(1, Binding.None)] Vector1 B,
            [Slot(2, Binding.None)] out Boolean Out)
        {
            return
                @"
{
    Out = A >= B ? 1 : 0;
}
";
        }
    }
}
