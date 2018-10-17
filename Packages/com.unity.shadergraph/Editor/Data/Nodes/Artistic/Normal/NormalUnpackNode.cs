using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Normal", "Normal Unpack")]
    internal class NormalUnpackNode : CodeFunctionNode
    {
        public NormalUnpackNode()
        {
            name = "Normal Unpack";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Normal-Unpack-Node"; }
        }

        [SerializeField]
        private NormalMapSpace m_NormalMapSpace = NormalMapSpace.Tangent;

        [EnumControl("Space")]
        public NormalMapSpace normalMapSpace
        {
            get { return m_NormalMapSpace; }
            set
            {
                if (m_NormalMapSpace == value)
                    return;

                m_NormalMapSpace = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod(normalMapSpace == NormalMapSpace.Tangent ? "Unity_NormalUnpack" : "Unity_NormalUnpackRGB", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_NormalUnpack(
            [Slot(0, Binding.None)] Vector4 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.up;

            return
            @"
            {
                Out = UnpackNormalmapRGorAG(In);
            }
            ";
        }

        static string Unity_NormalUnpackRGB(
            [Slot(0, Binding.None)] Vector4 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.up;

            return
            @"
            {
                Out = UnpackNormalmapRGB(In);
            }
            ";
        }
    }
}
