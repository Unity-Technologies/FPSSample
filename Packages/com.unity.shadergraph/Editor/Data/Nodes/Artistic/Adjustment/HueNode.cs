using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum HueMode
    {
        Degrees,
        Normalized
    };

    [Title("Artistic", "Adjustment", "Hue")]
    public class HueNode : CodeFunctionNode
    {
        public HueNode()
        {
            name = "Hue";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Hue-Node"; }
        }

        [SerializeField]
        private HueMode m_HueMode = HueMode.Degrees;

        [EnumControl("Range")]
        public HueMode hueMode
        {
            get { return m_HueMode; }
            set
            {
                if (m_HueMode == value)
                    return;

                m_HueMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (m_HueMode)
            {
                case HueMode.Normalized:
                    return GetType().GetMethod("Unity_Hue_Normalized", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    return GetType().GetMethod("Unity_Hue_Degrees", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_Hue_Degrees(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] Vector1 Offset,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    // RGB to HSV
    {precision}4 K = {precision}4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    {precision}4 P = lerp({precision}4(In.bg, K.wz), {precision}4(In.gb, K.xy), step(In.b, In.g));
    {precision}4 Q = lerp({precision}4(P.xyw, In.r), {precision}4(In.r, P.yzx), step(P.x, In.r));
    {precision} D = Q.x - min(Q.w, Q.y);
    {precision} E = 1e-10;
    {precision}3 hsv = {precision}3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);

    {precision} hue = hsv.x + Offset / 360;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    {precision}4 K2 = {precision}4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    {precision}3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}";
        }

        static string Unity_Hue_Normalized(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector1 Offset,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    // RGB to HSV
    {precision}4 K = {precision}4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    {precision}4 P = lerp({precision}4(In.bg, K.wz), {precision}4(In.gb, K.xy), step(In.b, In.g));
    {precision}4 Q = lerp({precision}4(P.xyw, In.r), {precision}4(In.r, P.yzx), step(P.x, In.r));
    {precision} D = Q.x - min(Q.w, Q.y);
    {precision} E = 1e-10;
    {precision}3 hsv = {precision}3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);

    {precision} hue = hsv.x + Offset;
    hsv.x = (hue < 0)
            ? hue + 1
            : (hue > 1)
                ? hue - 1
                : hue;

    // HSV to RGB
    {precision}4 K2 = {precision}4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    {precision}3 P2 = abs(frac(hsv.xxx + K2.xyz) * 6.0 - K2.www);
    Out = hsv.z * lerp(K2.xxx, saturate(P2 - K2.xxx), hsv.y);
}";
        }
    }
}
