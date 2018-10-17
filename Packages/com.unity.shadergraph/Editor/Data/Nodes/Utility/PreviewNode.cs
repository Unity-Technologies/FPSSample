using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Utility", "Preview")]
    public class PreviewNode : CodeFunctionNode
    {
        public override bool hasPreview { get { return true; } }

        [SerializeField]
        float m_Width;

        [SerializeField]
        float m_Height;

        public void SetDimensions(float width, float height)
        {
            float newSize = Mathf.Clamp(Mathf.Min(width, height), 150f, 1000f);

            m_Width = newSize;
            m_Height = newSize;
        }

        public float width
        {
            get { return m_Width; }
        }

        public float height
        {
            get { return m_Height; }
        }

        public PreviewNode()
        {
            name = "Preview";

            m_Width = 208f;
            m_Height = 208f;
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Preview-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Preview", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Preview(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = In;
}
";
        }
    }
}
