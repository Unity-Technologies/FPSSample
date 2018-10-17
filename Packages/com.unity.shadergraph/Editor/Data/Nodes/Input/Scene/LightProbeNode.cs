using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Light Probe")]
    public class LightProbeNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return false; } }

        public LightProbeNode()
        {
            name = "Light Probe";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Light-Probe-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_LightProbe", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_LightProbe(
            [Slot(0, Binding.WorldSpaceNormal)] Vector3 Normal,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;
            return
                @"
{
    Out = SampleSH(Normal);
}
";
        }

        public override PreviewMode previewMode
        {
            get
            {
                return PreviewMode.Preview3D;
            }
        }
    }
}
