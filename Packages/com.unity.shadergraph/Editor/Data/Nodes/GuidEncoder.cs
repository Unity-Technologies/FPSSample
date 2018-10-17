using System;

namespace UnityEditor.ShaderGraph
{
    public static class GuidEncoder
    {
        public static string Encode(Guid guid)
        {
            string enc = Convert.ToBase64String(guid.ToByteArray());
            return String.Format("{0:X}", enc.GetHashCode());
        }
    }
}
