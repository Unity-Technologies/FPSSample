namespace UnityEngine.Experimental.Rendering
{
    [System.Serializable]
    public class TextureSettings
    {
        public const int kDefaultSpotCookieSize = 128;
        public const int kDefaultPointCookieSize = 512;
        public const int kDefaultReflectionCubemapSize = 128;

        public int spotCookieSize = kDefaultSpotCookieSize;
        public int pointCookieSize = kDefaultPointCookieSize;
        public int reflectionCubemapSize = kDefaultReflectionCubemapSize;
    }
}
