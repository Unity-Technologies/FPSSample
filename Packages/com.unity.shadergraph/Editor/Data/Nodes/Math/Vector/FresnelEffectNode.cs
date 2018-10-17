using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Fresnel Effect")]
    class FresnelNode : CodeFunctionNode
    {
        public FresnelNode()
        {
            name = "Fresnel Effect";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Fresnel-Effect-Node"; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_FresnelEffect", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_FresnelEffect(
            [Slot(0, Binding.WorldSpaceNormal)] Vector3 Normal,
            [Slot(1, Binding.WorldSpaceViewDirection)] Vector3 ViewDir,
            [Slot(2, Binding.None, 1, 1, 1, 1)] Vector1 Power,
            [Slot(3, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
}
";
        }
    }
}
