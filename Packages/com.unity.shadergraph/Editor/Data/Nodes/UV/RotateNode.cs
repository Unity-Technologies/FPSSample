using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public enum RotationUnit
    {
        Radians,
        Degrees
    };

    [Title("UV", "Rotate")]
    public class RotateNode : CodeFunctionNode
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

        public RotateNode()
        {
            name = "Rotate";
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Rotate-Node"; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            if (m_Unit == RotationUnit.Radians)
                return GetType().GetMethod("Unity_Rotate_Radians", BindingFlags.Static | BindingFlags.NonPublic);
            else
                return GetType().GetMethod("Unity_Rotate_Degrees", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Rotate_Radians(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(2, Binding.None)] Vector1 Rotation,
            [Slot(3, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;


            return
                @"
{
    //rotation matrix
    UV -= Center;
    {precision} s = sin(Rotation);
    {precision} c = cos(Rotation);

    //center rotation matrix
    {precision}2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix*2 - 1;

    //multiply the UVs by the rotation matrix
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;

    Out = UV;
}";
        }

        static string Unity_Rotate_Degrees(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector2 Center,
            [Slot(2, Binding.None)] Vector1 Rotation,
            [Slot(3, Binding.None)] out Vector2 Out)
        {
            Out = Vector2.zero;

            return @"
{
    //rotation matrix
    Rotation = Rotation * (3.1415926f/180.0f);
    UV -= Center;
    {precision} s = sin(Rotation);
    {precision} c = cos(Rotation);

    //center rotation matrix
    {precision}2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix*2 - 1;

    //multiply the UVs by the rotation matrix
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;

    Out = UV;
}";
        }
    }
}
