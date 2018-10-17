using System;

namespace UnityEditor.ShaderGraph
{
    [Flags]
    public enum NeededCoordinateSpace
    {
        None = 0,
        Object = 1 << 0,
        View = 1 << 1,
        World = 1 << 2,
        Tangent = 1 << 3
    }

    public enum CoordinateSpace
    {
        Object,
        View,
        World,
        Tangent
    }

    public enum InterpolatorType
    {
        Normal,
        BiTangent,
        Tangent,
        ViewDirection,
        Position
    }

    public static class CoordinateSpaceNameExtensions
    {
        static int s_SpaceCount = Enum.GetValues(typeof(CoordinateSpace)).Length;
        static int s_InterpolatorCount = Enum.GetValues(typeof(InterpolatorType)).Length;
        static string[] s_VariableNames = new string[s_SpaceCount * s_InterpolatorCount];

        public static string ToVariableName(this CoordinateSpace space, InterpolatorType type)
        {
            var index = (int)space + (int)type * s_SpaceCount;
            if (string.IsNullOrEmpty(s_VariableNames[index]))
                s_VariableNames[index] = string.Format("{0}Space{1}", space, type);
            return s_VariableNames[index];
        }

        public static NeededCoordinateSpace ToNeededCoordinateSpace(this CoordinateSpace space)
        {
            switch (space)
            {
                case CoordinateSpace.Object:
                    return NeededCoordinateSpace.Object;
                case CoordinateSpace.View:
                    return NeededCoordinateSpace.View;
                case CoordinateSpace.World:
                    return NeededCoordinateSpace.World;
                case CoordinateSpace.Tangent:
                    return NeededCoordinateSpace.Tangent;
                default:
                    throw new ArgumentOutOfRangeException("space", space, null);
            }
        }
    }
}
