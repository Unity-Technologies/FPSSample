using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum ExponentialBase
    {
        BaseE,
        Base2
    };

    [Title("Math", "Advanced", "Exponential")]
    public class ExponentialNode : CodeFunctionNode
    {
        public ExponentialNode()
        {
            name = "Exponential";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Exponential-Node"; }
        }

        [SerializeField]
        private ExponentialBase m_ExponentialBase = ExponentialBase.BaseE;

        [EnumControl("Base")]
        public ExponentialBase exponentialBase
        {
            get { return m_ExponentialBase; }
            set
            {
                if (m_ExponentialBase == value)
                    return;

                m_ExponentialBase = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (m_ExponentialBase)
            {
                case ExponentialBase.Base2:
                    return GetType().GetMethod("Unity_Exponential2", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    return GetType().GetMethod("Unity_Exponential", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_Exponential(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = exp(In);
}
";
        }

        static string Unity_Exponential2(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = exp2(In);
}
";
        }
    }
}
