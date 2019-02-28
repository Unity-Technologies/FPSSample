using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using UnityEngine;

namespace UnityEditor.VFX.Utils
{
    public class PCache
    {
        public List<PropertyDesc> properties;
        public List<List<object>> buckets;
        public int elementCount;

        public enum Format
        {
            Ascii,
            Binary
        }

        public PCache()
        {
            properties = new List<PropertyDesc>();
            buckets = new List<List<object>>();
            elementCount = 0;
        }

        public void Clear()
        {
            properties.Clear();
            buckets.Clear();
            elementCount = 0;
        }

        public void AddFloatProperty(string name)
        {
            properties.Add(new PropertyDesc()
            {
                Name = name,
                ComponentIndex = 0,
                ComponentName = name,
                Type = "float"
            });

            buckets.Add(new List<object>());
        }

        public void AddVector2Property(string name)
        {
            properties.Add(new PropertyDesc()
            {
                Name = name + ".x",
                ComponentIndex = 0,
                ComponentName = name,
                Type = "float"
            });
            properties.Add(new PropertyDesc()
            {
                Name = name + ".y",
                ComponentIndex = 1,
                ComponentName = name,
                Type = "float"
            });
            buckets.Add(new List<object>());
            buckets.Add(new List<object>());
        }

        public void AddVector3Property(string name)
        {
            properties.Add(new PropertyDesc()
            {
                Name = name + ".x",
                ComponentIndex = 0,
                ComponentName = name,
                Type = "float"
            });
            properties.Add(new PropertyDesc()
            {
                Name = name + ".y",
                ComponentIndex = 1,
                ComponentName = name,
                Type = "float"
            }); properties.Add(new PropertyDesc()
            {
                Name = name + ".z",
                ComponentIndex = 2,
                ComponentName = name,
                Type = "float"
            });
            buckets.Add(new List<object>());
            buckets.Add(new List<object>());
            buckets.Add(new List<object>());
        }

        public void AddVector4Property(string name)
        {
            properties.Add(new PropertyDesc()
            {
                Name = name + ".x",
                ComponentIndex = 0,
                ComponentName = name,
                Type = "float"
            });
            properties.Add(new PropertyDesc()
            {
                Name = name + ".y",
                ComponentIndex = 1,
                ComponentName = name,
                Type = "float"
            });
            properties.Add(new PropertyDesc()
            {
                Name = name + ".z",
                ComponentIndex = 2,
                ComponentName = name,
                Type = "float"
            });
            properties.Add(new PropertyDesc()
            {
                Name = name + ".w",
                ComponentIndex = 3,
                ComponentName = name,
                Type = "float"
            });
            buckets.Add(new List<object>());
            buckets.Add(new List<object>());
            buckets.Add(new List<object>());
            buckets.Add(new List<object>());
        }

        public void AddColorProperty(string name)
        {
            properties.Add(new PropertyDesc()
            {
                Name = name + ".r",
                ComponentIndex = 0,
                ComponentName = name,
                Type = "float"
            });
            properties.Add(new PropertyDesc()
            {
                Name = name + ".g",
                ComponentIndex = 1,
                ComponentName = name,
                Type = "float"
            });
            properties.Add(new PropertyDesc()
            {
                Name = name + ".b",
                ComponentIndex = 2,
                ComponentName = name,
                Type = "float"
            });
            properties.Add(new PropertyDesc()
            {
                Name = name + ".a",
                ComponentIndex = 3,
                ComponentName = name,
                Type = "float"
            });
            buckets.Add(new List<object>());
            buckets.Add(new List<object>());
            buckets.Add(new List<object>());
            buckets.Add(new List<object>());
        }

        public void SetFloatData(string property, List<float> data)
        {
            var prop = properties.FirstOrDefault(o => o.Name == property);
            if (prop.Name != property)
                throw new InvalidOperationException("Could not find property :" + property);

            if (elementCount == 0 || data.Count == elementCount)
            {
                int index = properties.IndexOf(prop);
                foreach (float f in data)
                {
                    buckets[index].Add(f);
                }

                if (elementCount == 0) elementCount = data.Count;
            }
            else throw new InvalidOperationException("Need to set data corresponding to the actual element count :" + elementCount);
        }

        public void SetVector3Data(string component, List<float> dataX, List<float> dataY, List<float> dataZ)
        {
            SetFloatData(component + ".x", dataX);
            SetFloatData(component + ".y", dataY);
            SetFloatData(component + ".z", dataZ);
        }

        public void SetVector2Data(string component, List<Vector2> data)
        {
            var dataX = new List<float>();
            var dataY = new List<float>();

            foreach (var v in data)
            {
                dataX.Add(v.x);
                dataY.Add(v.y);
            }

            SetFloatData(component + ".x", dataX);
            SetFloatData(component + ".y", dataY);
        }

        public void SetVector3Data(string component, List<Vector3> data)
        {
            var dataX = new List<float>();
            var dataY = new List<float>();
            var dataZ = new List<float>();

            foreach (var v in data)
            {
                dataX.Add(v.x);
                dataY.Add(v.y);
                dataZ.Add(v.z);
            }

            SetFloatData(component + ".x", dataX);
            SetFloatData(component + ".y", dataY);
            SetFloatData(component + ".z", dataZ);
        }

        public void SetColorData(string component, List<Vector4> data)
        {
            var dataX = new List<float>();
            var dataY = new List<float>();
            var dataZ = new List<float>();
            var dataW = new List<float>();

            foreach (var v in data)
            {
                dataX.Add(v.x);
                dataY.Add(v.y);
                dataZ.Add(v.z);
                dataW.Add(v.w);
            }

            SetFloatData(component + ".r", dataX);
            SetFloatData(component + ".g", dataY);
            SetFloatData(component + ".b", dataZ);
            SetFloatData(component + ".a", dataW);
        }

        public void SetVector4Data(string component, List<Vector4> data)
        {
            var dataX = new List<float>();
            var dataY = new List<float>();
            var dataZ = new List<float>();
            var dataW = new List<float>();

            foreach (var v in data)
            {
                dataX.Add(v.x);
                dataY.Add(v.y);
                dataZ.Add(v.z);
                dataW.Add(v.w);
            }

            SetFloatData(component + ".x", dataX);
            SetFloatData(component + ".y", dataY);
            SetFloatData(component + ".z", dataZ);
            SetFloatData(component + ".w", dataW);
        }

        public void SaveToFile(string filename, Format format = Format.Binary)
        {
            FileStream outFile = File.Create(filename);
            BinaryWriter binaryWriter = new BinaryWriter(outFile);

            binaryWriter.Write(BuildHeaderString(format));

            if (format == Format.Binary)
            {
                for (int i = 0; i < elementCount; i++)
                {
                    for (int j = 0; j < properties.Count; j++)
                    {
                        var prop = properties[j];
                        switch (prop.Type)
                        {
                            case "byte":
                                binaryWriter.Write((byte)buckets[j][i]); break;
                            case "short":
                                binaryWriter.Write((short)buckets[j][i]); break;
                            case "ushort":
                                binaryWriter.Write((ushort)buckets[j][i]); break;
                            case "int":
                                binaryWriter.Write((int)buckets[j][i]); break;
                            case "uint":
                                binaryWriter.Write((uint)buckets[j][i]); break;
                            case "float":
                                binaryWriter.Write((float)buckets[j][i]); break;
                            case "double":
                                binaryWriter.Write((double)buckets[j][i]); break;
                        }
                    }
                }
            }
            else if (format == Format.Ascii)
            {
                for (int i = 0; i < elementCount; i++)
                {
                    StringBuilder sb = new StringBuilder();

                    for (int j = 0; j < properties.Count; j++)
                    {
                        var prop = properties[j];
                        switch (prop.Type)
                        {
                            case "byte":
                                sb.Append(((byte)buckets[j][i]).ToString(CultureInfo.InvariantCulture)); break;
                            case "short":
                                sb.Append(((short)buckets[j][i]).ToString(CultureInfo.InvariantCulture)); break;
                            case "ushort":
                                sb.Append(((ushort)buckets[j][i]).ToString(CultureInfo.InvariantCulture)); break;
                            case "int":
                                sb.Append(((int)buckets[j][i]).ToString(CultureInfo.InvariantCulture)); break;
                            case "uint":
                                sb.Append(((uint)buckets[j][i]).ToString(CultureInfo.InvariantCulture)); break;
                            case "float":
                                sb.Append(((float)buckets[j][i]).ToString(CultureInfo.InvariantCulture)); break;
                            case "double":
                                sb.Append(((double)buckets[j][i]).ToString(CultureInfo.InvariantCulture)); break;
                        }
                        sb.Append(j == properties.Count - 1 ? "\n" : " ");
                    }
                    binaryWriter.Write(sb.ToString().ToCharArray());
                }
            }
            else throw new Exception("Invalid format : " + format);

            binaryWriter.Close();
        }

        private char[] BuildHeaderString(Format format)
        {
            StringBuilder b = new StringBuilder();

            b.AppendLine("pcache");
            b.AppendLine(string.Format("format {0} 1.0", GetFormatString(format)));
            b.AppendLine("comment Exported with PCache.cs");
            b.AppendLine(string.Format("elements {0}", elementCount));

            foreach (var property in properties)
            {
                b.AppendLine(string.Format("property {0} {1}", property.Type, property.Name));
            }
            b.AppendLine("end_header");
            return b.ToString().ToCharArray();
        }

        public static PCache FromFile(string filename)
        {
            PCache data = new PCache();

            Stream s = File.OpenRead(filename);
            List<string> header;
            long offset;
            GetHeader(s, out offset, out header);

            if (header[0] != "pcache")
                throw new Exception("Invalid header : missing magic number");

            Format format = (Format)int.MaxValue;
            data.elementCount = 0;

            data.properties = new List<PropertyDesc>();


            foreach (string line in header)
            {
                var words = line.Split(' ');
                switch (words[0].ToLower())
                {
                    case "comment": //do nothing
                        break;
                    case "format":
                        if (words.Length != 3) throw new Exception("Invalid format description :" + line);
                        switch (words[1])
                        {
                            case "ascii": format = Format.Ascii; break;
                            case "binary": format = Format.Binary; break;
                            default: throw new Exception("Invalid Format :" + words[1]);
                        }
                        break;
                    case "elements":
                        if (words.Length != 2) throw new Exception("Invalid element description  :" + line);
                        if (!int.TryParse(words[1], out data.elementCount))
                            throw new Exception("Invalid element count :" + words[1]);
                        break;
                    case "property":
                        if (words.Length != 3) throw new Exception("Invalid property description :" + line);
                        string property = words[2];
                        string component = GetComponentName(property);
                        int idx = GetComponentIndex(property);
                        string type = words[1];
                        int stride = GetPropertySize(type);
                        if (stride == 0)
                            throw new Exception("Invalid Type for " + property + " property : " + type);

                        PropertyDesc prop = new PropertyDesc()
                        {
                            Name = property,
                            Type = type,
                            ComponentName = component,
                            ComponentIndex = idx,
                            Stride = stride
                        };
                        data.properties.Add(prop);
                        break;
                    case "end_header":
                        if (words.Length != 1) throw new Exception("Invalid end_header description :" + line);
                        break;
                }
            }

            data.buckets = new List<List<object>>();

            foreach (var property in data.properties)
            {
                data.buckets.Add(new List<object>(data.elementCount));
            }

            if (format == Format.Binary)
            {
                s.Close(); // End Header, goto binary mode
                s = File.OpenRead(filename);
                BinaryReader b = new BinaryReader(s);
                s.Seek(offset, SeekOrigin.Begin);

                for (int i = 0; i < data.elementCount; i++)
                {
                    for (int j = 0; j < data.properties.Count; j++)
                    {
                        var prop = data.properties[j];
                        switch (prop.Type)
                        {
                            case "short": data.buckets[j].Add(b.ReadInt16()); break;
                            case "ushort": data.buckets[j].Add(b.ReadUInt16()); break;
                            case "int": data.buckets[j].Add(b.ReadInt32()); break;
                            case "uint": data.buckets[j].Add(b.ReadUInt32()); break;
                            case "byte": data.buckets[j].Add(b.ReadChar()); break;
                            case "float": data.buckets[j].Add(b.ReadSingle()); break;
                            case "double": data.buckets[j].Add(b.ReadDouble()); break;
                        }
                    }
                }
            }
            else if (format == Format.Ascii)
            {
                s.Close(); // End Header, goto ascii mode
                s = File.OpenRead(filename);
                StreamReader reader = new StreamReader(s);
                s.Seek(offset, SeekOrigin.Begin);

                string[] lines = reader.ReadToEnd().Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length != data.elementCount)
                    throw new InvalidOperationException(string.Format("Bad item amount, {0} expected in header, found {1}", data.elementCount, lines.Length));

                for (int i = 0; i < data.elementCount; i++)
                {
                    string line = lines[i];
                    string[] elements = line.Split(' ');

                    for (int j = 0; j < data.properties.Count; j++)
                    {
                        var prop = data.properties[j];
                        switch (prop.Type)
                        {
                            case "short": data.buckets[j].Add(short.Parse(elements[j], CultureInfo.InvariantCulture)); break;
                            case "ushort": data.buckets[j].Add(ushort.Parse(elements[j], CultureInfo.InvariantCulture)); break;
                            case "int": data.buckets[j].Add(int.Parse(elements[j], CultureInfo.InvariantCulture)); break;
                            case "uint": data.buckets[j].Add(uint.Parse(elements[j], CultureInfo.InvariantCulture)); break;
                            case "byte": data.buckets[j].Add(byte.Parse(elements[j], CultureInfo.InvariantCulture)); break;
                            case "float": data.buckets[j].Add(float.Parse(elements[j], CultureInfo.InvariantCulture)); break;
                            case "double": data.buckets[j].Add(double.Parse(elements[j], CultureInfo.InvariantCulture)); break;
                        }
                    }
                }
            }
            return data;
        }

        private static void GetHeader(Stream s, out long byteLength, out List<string> lines)
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

                    if (c == '\n' || c == '\r')
                    {
                        if (sb.Length > 0)
                            newline = true;
                    }
                    else sb.Append(c);
                }
                while (!newline);

                string line = sb.ToString();
                lines.Add(line);
                if (line == "end_header") found_end_header = true;
            }
            while (!found_end_header);
        }

        private static string GetComponentName(string property)
        {
            string p = property.ToLower();
            if (p.EndsWith(".x") ||
                p.EndsWith(".y") ||
                p.EndsWith(".z") ||
                p.EndsWith(".w") ||
                p.EndsWith(".r") ||
                p.EndsWith(".g") ||
                p.EndsWith(".b") ||
                p.EndsWith(".a")) return property.Substring(0, property.Length - 2);
            else return property;
        }

        private static int GetComponentIndex(string property)
        {
            string p = property.ToLower();
            if (p.EndsWith(".x") || p.EndsWith(".r")) return 0;
            else if (p.EndsWith(".y") || p.EndsWith(".g")) return 1;
            else if (p.EndsWith(".z") || p.EndsWith(".b")) return 2;
            else if (p.EndsWith(".w") || p.EndsWith(".a")) return 3;
            else return 0;
        }

        private static string GetFormatString(Format format)
        {
            switch (format)
            {
                case Format.Ascii: return "ascii";
                case Format.Binary: return "binary";
                default: throw new InvalidOperationException("Invalid format");
            }
        }

        private static int GetPropertySize(string type)
        {
            if (TypeSize.ContainsKey(type))
                return TypeSize[type];
            else
                return 0;
        }

        private static Dictionary<string, int> TypeSize = new Dictionary<string, int>()
        {
            { "byte", 1 },
            { "short", 2 },
            { "ushort", 2 },
            { "int", 4 },
            { "uint", 4 },
            { "float", 4 },
            { "double", 8 },
        };

        public struct PropertyDesc
        {
            public string Name;
            public string Type;
            public string ComponentName;
            public int ComponentIndex;
            public int Stride;
        }
    }
}
