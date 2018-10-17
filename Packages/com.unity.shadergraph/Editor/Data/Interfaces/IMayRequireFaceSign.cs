using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public interface IMayRequireFaceSign
    {
        bool RequiresFaceSign(ShaderStageCapability stageCapability = ShaderStageCapability.Fragment);
    }

    public static class IMayRequireFaceSignExtensions
    {
        public static bool RequiresFaceSign(this ISlot slot)
        {
            var mayRequireFaceSign = slot as IMayRequireFaceSign;
            return mayRequireFaceSign != null && mayRequireFaceSign.RequiresFaceSign();
        }
    }
}
