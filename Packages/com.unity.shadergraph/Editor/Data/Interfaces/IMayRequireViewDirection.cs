using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireViewDirection
    {
        NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    public static class MayRequireViewDirectionExtensions
    {
        public static NeededCoordinateSpace RequiresViewDirection(this ISlot slot)
        {
            var mayRequireViewDirection = slot as IMayRequireViewDirection;
            return mayRequireViewDirection != null ? mayRequireViewDirection.RequiresViewDirection() : NeededCoordinateSpace.None;
        }
    }
}
