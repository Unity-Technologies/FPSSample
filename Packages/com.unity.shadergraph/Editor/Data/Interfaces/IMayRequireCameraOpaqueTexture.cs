using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public interface IMayRequireCameraOpaqueTexture
    {
        bool RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }

    public static class MayRequireCameraOpaqueTextureExtensions
    {
        public static bool RequiresCameraOpaqueTexture(this ISlot slot)
        {
            var mayRequireCameraOpaqueTexture = slot as IMayRequireCameraOpaqueTexture;
            return mayRequireCameraOpaqueTexture != null && mayRequireCameraOpaqueTexture.RequiresCameraOpaqueTexture();
        }
    }
}
