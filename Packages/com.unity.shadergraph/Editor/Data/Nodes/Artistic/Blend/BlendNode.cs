using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Blend", "Blend")]
    public class BlendNode : CodeFunctionNode
    {
        public BlendNode()
        {
            name = "Blend";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Blend-Node"; }
        }

        string GetCurrentBlendName()
        {
            return System.Enum.GetName(typeof(BlendMode), m_BlendMode);
        }

        [SerializeField]
        BlendMode m_BlendMode = BlendMode.Overlay;

        [EnumControl("Mode")]
        public BlendMode blendMode
        {
            get { return m_BlendMode; }
            set
            {
                if (m_BlendMode == value)
                    return;

                m_BlendMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod(string.Format("Unity_Blend_{0}", GetCurrentBlendName()),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Blend_Burn(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out =  1.0 - (1.0 - Blend)/Base;
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_Darken(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = min(Blend, Base);
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_Difference(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = abs(Blend - Base);
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_Dodge(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = Base / (1.0 - Blend);
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_Divide(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = Base / (Blend + 0.000000000001);
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_Exclusion(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = Blend + Base - (2.0 * Blend * Base);
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_HardLight(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision}{slot2dimension} result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    {precision}{slot2dimension} result2 = 2.0 * Base * Blend;
    {precision}{slot2dimension} zeroOrOne = step(Blend, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_HardMix(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = step(1 - Base, Blend);
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_Lighten(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = max(Blend, Base);
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_LinearBurn(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = Base + Blend - 1.0;
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_LinearDodge(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = Base + Blend;
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_LinearLight(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = Blend < 0.5 ? max(Base + (2 * Blend) - 1, 0) : min(Base + 2 * (Blend - 0.5), 1);
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_LinearLightAddSub(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = Blend + 2.0 * Base - 1.0;
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_Multiply(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = Base * Blend;
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_Negation(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = 1.0 - abs(1.0 - Blend - Base);
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_Screen(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = 1.0 - (1.0 - Blend) * (1.0 - Base);
    Out = lerp(Base, Out, Opacity);
}";
        }

        static string Unity_Blend_Overlay(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision}{slot2dimension} result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    {precision}{slot2dimension} result2 = 2.0 * Base * Blend;
    {precision}{slot2dimension} zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}
";
        }

        static string Unity_Blend_PinLight(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision}{slot2dimension} check = step (0.5, Blend);
    {precision}{slot2dimension} result1 = check * max(2.0 * (Base - 0.5), Blend);
    Out = result1 + (1.0 - check) * min(2.0 * Base, Blend);
    Out = lerp(Base, Out, Opacity);
}
";
        }

        static string Unity_Blend_SoftLight(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision}{slot2dimension} result1 = 2.0 * Base * Blend + Base * Base * (1.0 - 2.0 * Blend);
    {precision}{slot2dimension} result2 = sqrt(Base) * (2.0 * Blend - 1.0) + 2.0 * Base * (1.0 - Blend);
    {precision}{slot2dimension} zeroOrOne = step(0.5, Blend);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}
";
        }

        static string Unity_Blend_VividLight(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    {precision}{slot2dimension} result1 = 1.0 - (1.0 - Blend) / (2.0 * Base);
    {precision}{slot2dimension} result2 = Blend / (2.0 * (1.0 - Base));
    {precision}{slot2dimension} zeroOrOne = step(0.5, Base);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}
";
        }

        static string Unity_Blend_Subtract(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = Base - Blend;
    Out = lerp(Base, Out, Opacity);
}
";
        }
        
        static string Unity_Blend_Overwrite(
            [Slot(0, Binding.None)] DynamicDimensionVector Base,
            [Slot(1, Binding.None)] DynamicDimensionVector Blend,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 Opacity,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = lerp(Base, Blend, Opacity);
}";
        }
    }
}
