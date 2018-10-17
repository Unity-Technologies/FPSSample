using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Rotate About Axis")]
    public class RotateAboutAxisNode : CodeFunctionNode
    {
        [SerializeField]
        private RotationUnit m_Unit = RotationUnit.Radians;

        [EnumControl("Unit")]
        public RotationUnit unit
        {
            get { return m_Unit; }
            set
            {
                if (m_Unit == value)
                    return;

                m_Unit = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public RotateAboutAxisNode()
        {
            name = "Rotate About Axis";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Rotate-About-Axis-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            if (m_Unit == RotationUnit.Radians)
                return GetType().GetMethod("Unity_Rotate_About_Axis_Radians", BindingFlags.Static | BindingFlags.NonPublic);
            else
                return GetType().GetMethod("Unity_Rotate_About_Axis_Degrees", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Rotate_About_Axis_Degrees(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] Vector3 Axis,
            [Slot(2, Binding.None)] Vector1 Rotation,
            [Slot(3, Binding.None)] out Vector3 Out)
        {
            Out = In;
            return
                @"
{
    {precision} s = sin(Rotation);
    {precision} c = cos(Rotation);
    {precision} one_minus_c = 1.0 - c;
    
    Axis = normalize(Axis);

    {precision}3x3 rot_mat = { one_minus_c * Axis.x * Axis.x + c,            one_minus_c * Axis.x * Axis.y - Axis.z * s,     one_minus_c * Axis.z * Axis.x + Axis.y * s,
                               one_minus_c * Axis.x * Axis.y + Axis.z * s,   one_minus_c * Axis.y * Axis.y + c,              one_minus_c * Axis.y * Axis.z - Axis.x * s,
                               one_minus_c * Axis.z * Axis.x - Axis.y * s,   one_minus_c * Axis.y * Axis.z + Axis.x * s,     one_minus_c * Axis.z * Axis.z + c
                             };

    Out = mul(rot_mat,  In);
}
";
        }

        static string Unity_Rotate_About_Axis_Radians(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] Vector3 Axis,
            [Slot(2, Binding.None)] Vector1 Rotation,
            [Slot(3, Binding.None)] out Vector3 Out)
        {
            Out = In;
            return
                @"
{
    Rotation = radians(Rotation);

    {precision} s = sin(Rotation);
    {precision} c = cos(Rotation);
    {precision} one_minus_c = 1.0 - c;
    
    Axis = normalize(Axis);

    {precision}3x3 rot_mat = { one_minus_c * Axis.x * Axis.x + c,            one_minus_c * Axis.x * Axis.y - Axis.z * s,     one_minus_c * Axis.z * Axis.x + Axis.y * s,
                               one_minus_c * Axis.x * Axis.y + Axis.z * s,   one_minus_c * Axis.y * Axis.y + c,              one_minus_c * Axis.y * Axis.z - Axis.x * s,
                               one_minus_c * Axis.z * Axis.x - Axis.y * s,   one_minus_c * Axis.y * Axis.z + Axis.x * s,     one_minus_c * Axis.z * Axis.z + c
                             };

    Out = mul(rot_mat,  In);
}
";
        }
    }
}
