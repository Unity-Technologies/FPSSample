using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Adjustment", "White Balance")]
    public class WhiteBalanceNode : CodeFunctionNode
    {
        public WhiteBalanceNode()
        {
            name = "White Balance";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/White-Balance-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_WhiteBalance", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_WhiteBalance(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] Vector1 Temperature,
            [Slot(2, Binding.None)] Vector1 Tint,
            [Slot(3, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return @"
{
        // Range ~[-1.67;1.67] works best
        {precision} t1 = Temperature * 10 / 6;
        {precision} t2 = Tint * 10 / 6;

        // Get the CIE xy chromaticity of the reference white point.
        // Note: 0.31271 = x value on the D65 white point
        {precision} x = 0.31271 - t1 * (t1 < 0 ? 0.1 : 0.05);
        {precision} standardIlluminantY = 2.87 * x - 3 * x * x - 0.27509507;
        {precision} y = standardIlluminantY + t2 * 0.05;

        // Calculate the coefficients in the LMS space.
        {precision}3 w1 = {precision}3(0.949237, 1.03542, 1.08728); // D65 white point

        // CIExyToLMS
        {precision} Y = 1;
        {precision} X = Y * x / y;
        {precision} Z = Y * (1 - x - y) / y;
        {precision} L = 0.7328 * X + 0.4296 * Y - 0.1624 * Z;
        {precision} M = -0.7036 * X + 1.6975 * Y + 0.0061 * Z;
        {precision} S = 0.0030 * X + 0.0136 * Y + 0.9834 * Z;
        {precision}3 w2 = {precision}3(L, M, S);

        {precision}3 balance = {precision}3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);

        {precision}3x3 LIN_2_LMS_MAT = {
        3.90405e-1, 5.49941e-1, 8.92632e-3,
        7.08416e-2, 9.63172e-1, 1.35775e-3,
        2.31082e-2, 1.28021e-1, 9.36245e-1
    };

        {precision}3x3 LMS_2_LIN_MAT = {
        2.85847e+0, -1.62879e+0, -2.48910e-2,
        -2.10182e-1,  1.15820e+0,  3.24281e-4,
        -4.18120e-2, -1.18169e-1,  1.06867e+0
    };

    {precision}3 lms = mul(LIN_2_LMS_MAT, In);
    lms *= balance;
    Out = mul(LMS_2_LIN_MAT, lms);
}
";
        }
    }
}
