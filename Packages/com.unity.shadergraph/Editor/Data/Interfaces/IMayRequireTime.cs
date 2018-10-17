using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public interface IMayRequireTime
    {
        bool RequiresTime();
    }


    public static class MayRequireTimeExtensions
    {
        public static bool RequiresTime(this INode node)
        {
            var mayRequireTime = node as IMayRequireTime;
            return mayRequireTime != null && mayRequireTime.RequiresTime();
        }
    }
}
