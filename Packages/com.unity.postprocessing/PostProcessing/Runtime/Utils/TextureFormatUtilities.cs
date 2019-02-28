using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.PostProcessing
{
    // Temporary code dump until the texture format refactor goes into trunk...

    /// <summary>
    /// A set of utilities to deal with texture formats.
    /// </summary>
    public static class TextureFormatUtilities
    {
        static Dictionary<int, RenderTextureFormat> s_FormatAliasMap;
        static Dictionary<int, bool> s_SupportedRenderTextureFormats;
        static Dictionary<int, bool> s_SupportedTextureFormats;

        static TextureFormatUtilities()
        {
            s_FormatAliasMap = new Dictionary<int, RenderTextureFormat>
            {
                { (int)TextureFormat.Alpha8, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ARGB4444, RenderTextureFormat.ARGB4444 },
                { (int)TextureFormat.RGB24, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.RGBA32, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ARGB32, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.RGB565, RenderTextureFormat.RGB565 },
                { (int)TextureFormat.R16, RenderTextureFormat.RHalf },
                { (int)TextureFormat.DXT1, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.DXT5, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.RGBA4444, RenderTextureFormat.ARGB4444 },
                { (int)TextureFormat.BGRA32, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.RHalf, RenderTextureFormat.RHalf },
                { (int)TextureFormat.RGHalf, RenderTextureFormat.RGHalf },
                { (int)TextureFormat.RGBAHalf, RenderTextureFormat.ARGBHalf },
                { (int)TextureFormat.RFloat, RenderTextureFormat.RFloat },
                { (int)TextureFormat.RGFloat, RenderTextureFormat.RGFloat },
                { (int)TextureFormat.RGBAFloat, RenderTextureFormat.ARGBFloat },
                { (int)TextureFormat.RGB9e5Float, RenderTextureFormat.ARGBHalf },
                { (int)TextureFormat.BC4, RenderTextureFormat.R8 },
                { (int)TextureFormat.BC5, RenderTextureFormat.RGHalf },
                { (int)TextureFormat.BC6H, RenderTextureFormat.ARGBHalf },
                { (int)TextureFormat.BC7, RenderTextureFormat.ARGB32 },
            #if !UNITY_IOS && !UNITY_TVOS
                { (int)TextureFormat.DXT1Crunched, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.DXT5Crunched, RenderTextureFormat.ARGB32 },
            #endif
                { (int)TextureFormat.PVRTC_RGB2, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.PVRTC_RGBA2, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.PVRTC_RGB4, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.PVRTC_RGBA4, RenderTextureFormat.ARGB32 },
            #if !UNITY_2018_1_OR_NEWER
                { (int)TextureFormat.ATC_RGB4, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ATC_RGBA8, RenderTextureFormat.ARGB32 },
            #endif
                { (int)TextureFormat.ETC_RGB4, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ETC2_RGB, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ETC2_RGBA1, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ETC2_RGBA8, RenderTextureFormat.ARGB32 },
            #if UNITY_2019_1_OR_NEWER
                { (int)TextureFormat.ASTC_4x4, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_5x5, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_6x6, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_8x8, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_10x10, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_12x12, RenderTextureFormat.ARGB32 },
            #else
                { (int)TextureFormat.ASTC_RGB_4x4, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_RGB_5x5, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_RGB_6x6, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_RGB_8x8, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_RGB_10x10, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_RGB_12x12, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_RGBA_4x4, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_RGBA_5x5, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_RGBA_6x6, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_RGBA_8x8, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_RGBA_10x10, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ASTC_RGBA_12x12, RenderTextureFormat.ARGB32 },
            #endif
            #if !UNITY_2018_3_OR_NEWER
                { (int)TextureFormat.ETC_RGB4_3DS, RenderTextureFormat.ARGB32 },
                { (int)TextureFormat.ETC_RGBA8_3DS, RenderTextureFormat.ARGB32 }
            #endif
            };

            // TODO: refactor the next two scopes in a generic function once we have support for enum constraints on generics
            // In 2018.1 SystemInfo.SupportsRenderTextureFormat() generates garbage so we need to
            // cache its calls to avoid that...
            {
                s_SupportedRenderTextureFormats = new Dictionary<int, bool>();
                var values = Enum.GetValues(typeof(RenderTextureFormat));

                foreach (var format in values)
                {
                    if ((int)format < 0) // Safe guard, negative values are deprecated stuff
                        continue;

                    if (IsObsolete(format))
                        continue;

                    bool supported = SystemInfo.SupportsRenderTextureFormat((RenderTextureFormat)format);
                    s_SupportedRenderTextureFormats[(int)format] = supported;
                }
            }

            // Same for TextureFormat
            {
                s_SupportedTextureFormats = new Dictionary<int, bool>();
                var values = Enum.GetValues(typeof(TextureFormat));

                foreach (var format in values)
                {
                    if ((int)format < 0) // Crashes the runtime otherwise (!)
                        continue;

                    if (IsObsolete(format))
                        continue;

                    bool supported = SystemInfo.SupportsTextureFormat((TextureFormat)format);
                    s_SupportedTextureFormats[(int)format] = supported;
                }
            }
        }

        static bool IsObsolete(object value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var attributes = (ObsoleteAttribute[])fieldInfo.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            return attributes != null && attributes.Length > 0;
        }

        /// <summary>
        /// Returns a <see cref="RenderTextureFormat"/> compatible with the given texture's format.
        /// </summary>
        /// <param name="texture">A texture to get a compatible format from</param>
        /// <returns>A compatible render texture format</returns>
        public static RenderTextureFormat GetUncompressedRenderTextureFormat(Texture texture)
        {
            Assert.IsNotNull(texture);

            if (texture is RenderTexture)
                return (texture as RenderTexture).format;

            if (texture is Texture2D)
            {
                var inFormat = ((Texture2D)texture).format;
                RenderTextureFormat outFormat;

                if (!s_FormatAliasMap.TryGetValue((int)inFormat, out outFormat))
                    throw new NotSupportedException("Texture format not supported");

                return outFormat;
            }

            return RenderTextureFormat.Default;
        }

        internal static bool IsSupported(this RenderTextureFormat format)
        {
            bool supported;
            s_SupportedRenderTextureFormats.TryGetValue((int)format, out supported);
            return supported;
        }

        internal static bool IsSupported(this TextureFormat format)
        {
            bool supported;
            s_SupportedTextureFormats.TryGetValue((int)format, out supported);
            return supported;
        }
    }
}
