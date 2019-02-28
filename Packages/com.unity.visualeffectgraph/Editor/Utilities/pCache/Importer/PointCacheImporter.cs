using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.VFX.Utils
{
    [ScriptedImporter(1, "pcache")]
    public class PointCacheImporter : ScriptedImporter
    {
        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        class InProperty
        {
            public string PropertyType;
            public string Name;
            public int Index;
            public OutProperty OutProperty;

            public InProperty(string propertyType, string name, int index, OutProperty outProperty)
            {
                PropertyType = propertyType;
                Name = name;
                Index = index;
                OutProperty = outProperty;
            }
        }

        class OutProperty
        {
            public string PropertyType;
            public string Name;
            public int Size;
            public OutProperty(string propertyType, string name, int size)
            {
                PropertyType = propertyType;
                Name = name;
                Size = size;
            }
        }

        public static void GetHeader(Stream s, out long byteLength, out List<string> lines)
        {
            byteLength = 0;
            bool found_end_header = false;
            lines = new List<string>();

            s.Seek(0, SeekOrigin.Begin);
            BinaryReader sr = new BinaryReader(s);

            do
            {
                StringBuilder sb = new StringBuilder();
                bool newline = false;
                do
                {
                    char c = sr.ReadChar();
                    byteLength++;
                    if ((c == '\n' || c == '\r') && sb.Length > 0) newline = true;
                    else sb.Append(c);
                }
                while (!newline);

                string line = sb.ToString();
                lines.Add(line);
                if (line == "end_header") found_end_header = true;
            }
            while (!found_end_header);
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            try
            {
                PCache pcache = PCache.FromFile(ctx.assetPath);

                PointCacheAsset cache = ScriptableObject.CreateInstance<PointCacheAsset>();
                cache.name = "PointCache";
                ctx.AddObjectToAsset("PointCache", cache);
                ctx.SetMainObject(cache);

                List<InProperty> inProperties = new List<InProperty>();
                Dictionary<string, OutProperty> outProperties = new Dictionary<string, OutProperty>();
                Dictionary<OutProperty, Texture2D> surfaces = new Dictionary<OutProperty, Texture2D>();

                foreach (var prop in pcache.properties)
                {
                    OutProperty p_out;
                    if (outProperties.ContainsKey(prop.ComponentName))
                    {
                        p_out = outProperties[prop.ComponentName];
                        p_out.Size = Math.Max(p_out.Size, prop.ComponentIndex + 1);
                    }
                    else
                    {
                        p_out = new OutProperty(prop.Type, prop.ComponentName, prop.ComponentIndex + 1);
                        outProperties.Add(prop.ComponentName, p_out);
                    }

                    inProperties.Add(new InProperty(prop.Type, prop.Name, prop.ComponentIndex, p_out));
                }


                int width, height;
                FindBestSize(pcache.elementCount, out width, out height);

                // Output Surface Creation
                foreach (var kvp in outProperties)
                {
                    TextureFormat surfaceFormat = TextureFormat.Alpha8;
                    switch (kvp.Value.PropertyType)
                    {
                        case "byte":
                            if (kvp.Value.Size == 1) surfaceFormat = TextureFormat.Alpha8;
                            else surfaceFormat = TextureFormat.RGBA32;
                            break;
                        case "float":
                            if (kvp.Value.Size == 1) surfaceFormat = TextureFormat.RHalf;
                            else surfaceFormat = TextureFormat.RGBAHalf;
                            break;
                        default: throw new NotImplementedException("Types other than byte/float are not supported yet");
                    }

                    Texture2D surface = new Texture2D(width, height, surfaceFormat, false);
                    surface.name = kvp.Key;
                    surfaces.Add(kvp.Value, surface);
                }

                cache.PointCount = pcache.elementCount;
                cache.surfaces = new Texture2D[surfaces.Count];

                Dictionary<OutProperty, Color> outValues = new Dictionary<OutProperty, Color>();
                foreach (var kvp in outProperties)
                    outValues.Add(kvp.Value, new Color());

                for (int i = 0; i < pcache.elementCount; i++)
                {
                    int idx = 0;
                    foreach (var prop in inProperties)
                    {
                        float val = 0.0f;
                        switch (prop.PropertyType)
                        {
                            case "byte":
                                val = Mathf.Clamp01(((int)pcache.buckets[idx][i]) / 256.0f);
                                break;
                            case "float":
                                val = ((float)pcache.buckets[idx][i]);
                                break;
                            default: throw new NotImplementedException("Types other than byte/float are not supported yet");
                        }

                        SetPropValue(prop.Index, outValues, prop.OutProperty, val);
                        idx++;
                    }
                    foreach (var kvp in outProperties)
                    {
                        surfaces[kvp.Value].SetPixel(i % width, i / width, outValues[kvp.Value]);
                    }
                }

                int k = 0;

                foreach (var kvp in surfaces)
                {
                    kvp.Value.Apply();
                    kvp.Value.hideFlags = HideFlags.HideInHierarchy;
                    ctx.AddObjectToAsset(kvp.Key.Name, kvp.Value);
                    cache.surfaces[k] = kvp.Value;
                    k++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void SetPropValue(int index, Dictionary<OutProperty, Color> data, OutProperty prop, float val)
        {
            Color c = data[prop];

            switch (index)
            {
                case 0: if (prop.Size == 1 && prop.PropertyType == "int") c.a = val; else c.r = val; break;
                case 1: c.g = val; break;
                case 2: c.b = val; break;
                case 3: c.a = val; break;
            }
            data[prop] = c;
        }

        private void FindBestSize(int count, out int width, out int height)
        {
            float r = Mathf.Sqrt(count);
            width = (int)Mathf.Ceil(r);
            height = width;
        }
    }
}
