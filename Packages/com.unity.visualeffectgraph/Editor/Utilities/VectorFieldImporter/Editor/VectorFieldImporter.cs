using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.VFXToolbox
{
    [ScriptedImporter(1, "vf")]
    public class VectorFieldImporter : ScriptedImporter
    {
        public enum VectorFieldOutputFormat
        {
            Float = 0,
            Half = 1,
            Byte = 2,
        }

        public VectorFieldOutputFormat m_OutputFormat = VectorFieldOutputFormat.Half;
        public TextureWrapMode m_WrapMode = TextureWrapMode.Repeat;
        public FilterMode m_FilterMode = FilterMode.Bilinear;
        public bool m_GenerateMipMaps = false;
        public int m_AnisoLevel = 1;

        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            byte[] bytes = File.ReadAllBytes(ctx.assetPath);
            int width = 1, height = 1, depth = 1;

            try
            {
                int channels = 0;
                string fourcc = Encoding.UTF8.GetString(SubArray<byte>(bytes, 0, 4));

                if (fourcc != "VF_F" && fourcc != "VF_V")
                    throw new Exception("Invalid VF File Header. Need VF_F or VF_V, found :" + fourcc);
                else
                {
                    if (fourcc == "VF_F") channels = 1;
                    if (fourcc == "VF_V") channels = 3;
                }

                TextureFormat outFormat = TextureFormat.Alpha8;
                switch (m_OutputFormat)
                {
                    case VectorFieldOutputFormat.Byte: outFormat = channels == 3 ? TextureFormat.RGBA32 : TextureFormat.Alpha8; break;
                    case VectorFieldOutputFormat.Half: outFormat = channels == 3 ? TextureFormat.RGBAHalf : TextureFormat.RHalf; break;
                    case VectorFieldOutputFormat.Float: outFormat = channels == 3 ? TextureFormat.RGBAFloat : TextureFormat.RFloat; break;
                }

                if (bytes.Length < 10)
                    throw new Exception("Malformed VF File, invalid header (less than 10 bytes)");

                width =     BitConverter.ToUInt16(bytes, 4);
                height =    BitConverter.ToUInt16(bytes, 6);
                depth =     BitConverter.ToUInt16(bytes, 8);

                int requiredLength = 10 + (4 * channels * (width * height * depth));

                if (bytes.Length != requiredLength)
                    throw new Exception("Malformed VF File, invalid length (expected :" + requiredLength + ", found :" + bytes.Length + ")");

                Texture3D texture = new Texture3D(width, height, depth, outFormat, m_GenerateMipMaps);
                texture.wrapMode = m_WrapMode;
                texture.filterMode = m_FilterMode;
                texture.anisoLevel = m_AnisoLevel;

                int count = width * height * depth;

                Color[] colors = new Color[count];

                for (int i = 0; i < count; i++)
                {
                    Color c;
                    if (channels == 1)
                    {
                        float x = BitConverter.ToSingle(bytes, 10 + (i * 4 * channels));
                        c = new Color(x, 0, 0);
                    }
                    else
                    {
                        float x = BitConverter.ToSingle(bytes, 10 + (i * 4 * channels));
                        float y = BitConverter.ToSingle(bytes, 14 + (i * 4 * channels));
                        float z = BitConverter.ToSingle(bytes, 18 + (i * 4 * channels));
                        c = new Color(x, y, z);
                    }
                    colors[i] = c;
                }

                texture.SetPixels(colors);
                texture.Apply(true, true);
                ctx.AddObjectToAsset("VectorField", texture);
                ctx.SetMainObject(texture);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
