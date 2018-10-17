using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public interface IMayRequireMeshUV
    {
        bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    public static class MayRequireMeshUVExtensions
    {
        public static bool RequiresMeshUV(this ISlot slot, UVChannel channel)
        {
            var mayRequireMeshUV = slot as IMayRequireMeshUV;
            return mayRequireMeshUV != null && mayRequireMeshUV.RequiresMeshUV(channel);
        }
    }
}
