using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public enum Colorspace
    {
        RGB,
        Linear,
        HSV
    }

    [Serializable]
    public struct ColorspaceConversion : IEnumConversion
    {
        public Colorspace from;
        public Colorspace to;

        public ColorspaceConversion(Colorspace from, Colorspace to)
        {
            this.from = from;
            this.to = to;
        }

        Enum IEnumConversion.from
        {
            get { return from; }
            set { from = (Colorspace)value; }
        }

        Enum IEnumConversion.to
        {
            get { return to; }
            set { to = (Colorspace)value; }
        }
    }

    [Title("Artistic", "Utility", "Colorspace Conversion")]
    public class ColorspaceConversionNode : CodeFunctionNode
    {
        public ColorspaceConversionNode()
        {
            name = "Colorspace Conversion";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Colorspace-Conversion-Node"; }
        }

        [SerializeField]
        ColorspaceConversion m_Conversion = new ColorspaceConversion(Colorspace.RGB, Colorspace.RGB);

        [EnumConversionControl]
        ColorspaceConversion conversion
        {
            get { return m_Conversion; }
            set
            {
                if (m_Conversion.Equals(value))
                    return;
                m_Conversion = value;
                Dirty(ModificationScope.Graph);
            }
        }

        string GetSpaceFrom()
        {
            return Enum.GetName(typeof(Colorspace), conversion.from);
        }

        string GetSpaceTo()
        {
            return Enum.GetName(typeof(Colorspace), conversion.to);
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod(string.Format("Unity_ColorspaceConversion_{0}_{1}", GetSpaceFrom(), GetSpaceTo()),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ColorspaceConversion_RGB_RGB(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    Out = In;
}
";
        }

        static string Unity_ColorspaceConversion_RGB_Linear(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    {precision}3 linearRGBLo = In / 12.92;;
    {precision}3 linearRGBHi = pow(max(abs((In + 0.055) / 1.055), 1.192092896e-07), {precision}3(2.4, 2.4, 2.4));;
    Out = {precision}3(In <= 0.04045) ? linearRGBLo : linearRGBHi;
}
";
        }

        static string Unity_ColorspaceConversion_RGB_HSV(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    {precision}4 K = {precision}4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    {precision}4 P = lerp({precision}4(In.bg, K.wz), {precision}4(In.gb, K.xy), step(In.b, In.g));
    {precision}4 Q = lerp({precision}4(P.xyw, In.r), {precision}4(In.r, P.yzx), step(P.x, In.r));
    {precision} D = Q.x - min(Q.w, Q.y);
    {precision}  E = 1e-10;
    Out = {precision}3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);
}
";
        }

        static string Unity_ColorspaceConversion_Linear_RGB(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    {precision}3 sRGBLo = In * 12.92;
    {precision}3 sRGBHi = (pow(max(abs(In), 1.192092896e-07), {precision}3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
    Out = {precision}3(In <= 0.0031308) ? sRGBLo : sRGBHi;
}
";
        }

        static string Unity_ColorspaceConversion_Linear_Linear(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    Out = In;
}
";
        }

        static string Unity_ColorspaceConversion_Linear_HSV(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    {precision}3 sRGBLo = In * 12.92;
    {precision}3 sRGBHi = (pow(max(abs(In), 1.192092896e-07), {precision}3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
    {precision}3 Linear = {precision}3(In <= 0.0031308) ? sRGBLo : sRGBHi;
    {precision}4 K = {precision}4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    {precision}4 P = lerp({precision}4(Linear.bg, K.wz), {precision}4(Linear.gb, K.xy), step(Linear.b, Linear.g));
    {precision}4 Q = lerp({precision}4(P.xyw, Linear.r), {precision}4(Linear.r, P.yzx), step(P.x, Linear.r));
    {precision} D = Q.x - min(Q.w, Q.y);
    {precision}  E = 1e-10;
    Out = {precision}3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);
}
";
        }

        static string Unity_ColorspaceConversion_HSV_RGB(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    {precision}4 K = {precision}4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    {precision}3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
    Out = In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);
}
";
        }

        static string Unity_ColorspaceConversion_HSV_Linear(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    {precision}4 K = {precision}4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    {precision}3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
    {precision}3 RGB = In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);
    {precision}3 linearRGBLo = RGB / 12.92;;
    {precision}3 linearRGBHi = pow(max(abs((RGB + 0.055) / 1.055), 1.192092896e-07), {precision}3(2.4, 2.4, 2.4));;
    Out = {precision}3(RGB <= 0.04045) ? linearRGBLo : linearRGBHi;
}
";
        }

        static string Unity_ColorspaceConversion_HSV_HSV(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    Out = In;
}
";
        }
    }
}
