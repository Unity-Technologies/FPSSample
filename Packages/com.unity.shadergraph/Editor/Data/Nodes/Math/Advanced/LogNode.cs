using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum LogBase
    {
        BaseE,
        Base2,
        Base10
    };

    [Title("Math", "Advanced", "Log")]
    public class LogNode : CodeFunctionNode
    {
        public LogNode()
        {
            name = "Log";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Log-Node"; }
        }

        [SerializeField]
        private LogBase m_LogBase = LogBase.BaseE;

        [EnumControl("Base")]
        public LogBase logBase
        {
            get { return m_LogBase; }
            set
            {
                if (m_LogBase == value)
                    return;

                m_LogBase = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (m_LogBase)
            {
                case LogBase.Base2:
                    return GetType().GetMethod("Unity_Log2", BindingFlags.Static | BindingFlags.NonPublic);
                case LogBase.Base10:
                    return GetType().GetMethod("Unity_Log10", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    return GetType().GetMethod("Unity_Log", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_Log(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = log(In);
}
";
        }

        static string Unity_Log2(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = log2(In);
}
";
        }

        static string Unity_Log10(
            [Slot(0, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = log10(In);
}
";
        }
    }
}
