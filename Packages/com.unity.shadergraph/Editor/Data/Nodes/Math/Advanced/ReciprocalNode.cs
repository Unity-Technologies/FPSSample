using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum ReciprocalMethod
    {
        Default,
        Fast
    };

    [Title("Math", "Advanced", "Reciprocal")]
    public class ReciprocalNode : CodeFunctionNode
    {
        public ReciprocalNode()
        {
            name = "Reciprocal";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Reciprocal-Node"; }
        }

        [SerializeField]
        private ReciprocalMethod m_ReciprocalMethod = ReciprocalMethod.Default;

        [EnumControl("Method")]
        public ReciprocalMethod reciprocalMethod
        {
            get { return m_ReciprocalMethod; }
            set
            {
                if (m_ReciprocalMethod == value)
                    return;

                m_ReciprocalMethod = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (m_ReciprocalMethod)
            {
                case ReciprocalMethod.Fast:
                    return GetType().GetMethod("Unity_Reciprocal_Fast", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    return GetType().GetMethod("Unity_Reciprocal", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_Reciprocal(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = 1.0/In;
}
";
        }

        static string Unity_Reciprocal_Fast(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = rcp(In);
}
";
        }
    }
}
