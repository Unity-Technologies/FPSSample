using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public interface IMayRequirePosition
    {
        NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    public static class MayRequirePositionExtensions
    {
        public static NeededCoordinateSpace RequiresPosition(this ISlot slot)
        {
            var mayRequirePosition = slot as IMayRequirePosition;
            return mayRequirePosition != null ? mayRequirePosition.RequiresPosition() : NeededCoordinateSpace.None;
        }
    }
}
