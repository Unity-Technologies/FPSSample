using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.VFX.Utils
{
    public partial class PointCacheBakeTool : EditorWindow
    {
        public enum MeshBakeMode
        {
            Vertex,
            Triangle
        }

        public enum Distribution
        {
            Sequential,
            Random,
            RandomUniformArea
        }

        Distribution m_Distribution = Distribution.RandomUniformArea;
        MeshBakeMode m_MeshBakeMode = MeshBakeMode.Triangle;
        PCache.Format m_OutputFormat = PCache.Format.Ascii;

        bool m_ExportUV = false;
        bool m_ExportNormals = true;
        bool m_ExportColors = false;

        Mesh m_Mesh;
        int m_OutputPointCount = 4096;
        int m_SeedMesh;

        void OnGUI_Mesh()
        {
            GUILayout.Label("Mesh Baking", EditorStyles.boldLabel);
            m_Mesh = (Mesh)EditorGUILayout.ObjectField(Contents.mesh, m_Mesh, typeof(Mesh), false);
            m_Distribution = (Distribution)EditorGUILayout.EnumPopup(Contents.distribution, m_Distribution);
            if (m_Distribution != Distribution.RandomUniformArea)
                m_MeshBakeMode = (MeshBakeMode)EditorGUILayout.EnumPopup(Contents.meshBakeMode, m_MeshBakeMode);

            m_ExportNormals = EditorGUILayout.Toggle("Export Normals", m_ExportNormals);
            m_ExportColors = EditorGUILayout.Toggle("Export Colors", m_ExportColors);
            m_ExportUV = EditorGUILayout.Toggle("Export UVs", m_ExportUV);

            m_OutputPointCount = EditorGUILayout.IntField("Point Count", m_OutputPointCount);
            if (m_Distribution != Distribution.Sequential)
                m_SeedMesh = EditorGUILayout.IntField("Seed", m_SeedMesh);

            m_OutputFormat = (PCache.Format)EditorGUILayout.EnumPopup("File Format", m_OutputFormat);

            if (m_Mesh != null)
            {
                if (GUILayout.Button("Save to pCache file..."))
                {
                    string fileName = EditorUtility.SaveFilePanelInProject("pCacheFile", m_Mesh.name, "pcache", "Save PCache");
                    if (fileName != null)
                    {
                        try
                        {
                            EditorUtility.DisplayProgressBar("pCache bake tool", "Capturing data...", 0.0f);
                            var file = ComputePCacheFromMesh();
                            if (file != null)
                            {
                                EditorUtility.DisplayProgressBar("pCache bake tool", "Saving pCache file", 1.0f);
                                file.SaveToFile(fileName, m_OutputFormat);
                                AssetDatabase.ImportAsset(fileName, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }
                        EditorUtility.ClearProgressBar();
                    }
                }
                EditorGUILayout.Space();

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Mesh Statistics", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.IntField("Vertices", m_Mesh.vertexCount);
                    EditorGUILayout.IntField("Triangles", m_Mesh.triangles.Length);
                    EditorGUILayout.IntField("Sub Meshes", m_Mesh.subMeshCount);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space();
        }

        class MeshData
        {
            public struct Vertex
            {
                public Vector3 position;
                public Color color;
                public Vector3 normal;
                public Vector4 tangent;
                public Vector4[] uvs;

                public static Vertex operator+(Vertex a, Vertex b)
                {
                    if (a.uvs.Length != b.uvs.Length)
                        throw new InvalidOperationException("Adding compatible vertex");

                    var r = new Vertex()
                    {
                        position = a.position + b.position,
                        color = a.color + b.color,
                        normal = a.normal + b.normal,
                        tangent = a.tangent + b.tangent,
                        uvs = new Vector4[a.uvs.Length]
                    };

                    for (int i = 0; i < a.uvs.Length; ++i)
                        r.uvs[i] = a.uvs[i] + b.uvs[i];

                    return r;
                }

                public static Vertex operator*(float a, Vertex b)
                {
                    var r = new Vertex()
                    {
                        position = a * b.position,
                        color = a * b.color,
                        normal = a * b.normal,
                        tangent = a * b.tangent,
                        uvs = new Vector4[b.uvs.Length]
                    };

                    for (int i = 0; i < b.uvs.Length; ++i)
                        r.uvs[i] = a * b.uvs[i];

                    return r;
                }
            };

            public struct Triangle
            {
                public int a, b, c;
            };

            public Vertex[] vertices;
            public Triangle[] triangles;
        }

        static MeshData ComputeDataCache(Mesh input)
        {
            var positions = input.vertices;
            var normals = input.normals;
            var tangents = input.tangents;
            var colors = input.colors;
            var uvs = new List<Vector4[]>();

            normals = normals.Length == input.vertexCount ? normals : null;
            tangents = tangents.Length == input.vertexCount ? tangents : null;
            colors = colors.Length == input.vertexCount ? colors : null;

            for (int i = 0; i < 8; ++i)
            {
                var uv = new List<Vector4>();
                input.GetUVs(i, uv);
                if (uv.Count == input.vertexCount)
                {
                    uvs.Add(uv.ToArray());
                }
                else
                {
                    break;
                }
            }

            var meshData = new MeshData();
            meshData.vertices = new MeshData.Vertex[input.vertexCount];
            for (int i = 0; i < input.vertexCount; ++i)
            {
                meshData.vertices[i] = new MeshData.Vertex()
                {
                    position = positions[i],
                    color = colors != null ? colors[i] : Color.white,
                    normal = normals != null ? normals[i] : Vector3.up,
                    tangent = tangents != null ? tangents[i] : Vector4.one,
                    uvs = Enumerable.Range(0, uvs.Count).Select(c => uvs[c][i]).ToArray()
                };
            }

            meshData.triangles = new MeshData.Triangle[input.triangles.Length / 3];
            var triangles = input.triangles;
            for (int i = 0; i < meshData.triangles.Length; ++i)
            {
                meshData.triangles[i] = new MeshData.Triangle()
                {
                    a = triangles[i * 3 + 0],
                    b = triangles[i * 3 + 1],
                    c = triangles[i * 3 + 2],
                };
            }
            return meshData;
        }

        abstract class Picker
        {
            public abstract MeshData.Vertex GetNext();

            protected Picker(MeshData data)
            {
                m_cacheData = data;
            }

            //See http://inis.jinr.ru/sl/vol1/CMC/Graphics_Gems_1,ed_A.Glassner.pdf (p24) uniform distribution from two numbers in triangle generating barycentric coordinate
            protected readonly static  Vector2 center_of_sampling = new Vector2(4.0f / 9.0f, 3.0f / 4.0f);
            protected MeshData.Vertex Interpolate(MeshData.Triangle triangle, Vector2 p)
            {
                return Interpolate(m_cacheData.vertices[triangle.a], m_cacheData.vertices[triangle.b], m_cacheData.vertices[triangle.c], p);
            }

            protected static MeshData.Vertex Interpolate(MeshData.Vertex A, MeshData.Vertex B, MeshData.Vertex C, Vector2 p)
            {
                float s = p.x;
                float t = Mathf.Sqrt(p.y);
                float a = 1.0f - t;
                float b = (1 - s) * t;
                float c = s * t;

                var r = a * A + b * B + c * C;
                r.normal = r.normal.normalized;
                var tangent = new Vector3(r.tangent.x, r.tangent.y, r.tangent.z).normalized;
                r.tangent = new Vector4(tangent.x, tangent.y, tangent.z, r.tangent.w > 0.0f ? 1.0f : -1.0f);
                return r;
            }

            protected MeshData m_cacheData;
        }

        abstract class RandomPicker : Picker
        {
            protected RandomPicker(MeshData data, int seed) : base(data)
            {
                m_Rand = new System.Random(seed);
            }

            protected float GetNextRandFloat()
            {
                return (float)m_Rand.NextDouble(); //[0; 1[
            }

            protected System.Random m_Rand;
        }

        class RandomPickerVertex : RandomPicker
        {
            public RandomPickerVertex(MeshData data, int seed) : base(data, seed)
            {
            }

            public override sealed MeshData.Vertex GetNext()
            {
                int randomIndex = m_Rand.Next(0, m_cacheData.vertices.Length);
                return m_cacheData.vertices[randomIndex];
            }
        }

        class SequentialPickerVertex : Picker
        {
            private uint m_Index = 0;
            public SequentialPickerVertex(MeshData data) : base(data)
            {
            }

            public override sealed MeshData.Vertex GetNext()
            {
                var r = m_cacheData.vertices[m_Index];
                m_Index++;
                if (m_Index >= m_cacheData.vertices.Length)
                    m_Index = 0;
                return r;
            }
        }

        class RandomPickerTriangle : RandomPicker
        {
            public RandomPickerTriangle(MeshData data, int seed) : base(data, seed)
            {
            }

            public override sealed MeshData.Vertex GetNext()
            {
                var index = m_Rand.Next(0, m_cacheData.triangles.Length);
                var rand = new Vector2(GetNextRandFloat(), GetNextRandFloat());
                return Interpolate(m_cacheData.triangles[index], rand);
            }
        }

        class RandomPickerUniformArea : RandomPicker
        {
            private double[] m_accumulatedAreaTriangles;

            private double ComputeTriangleArea(MeshData.Triangle t)
            {
                var A = m_cacheData.vertices[t.a].position;
                var B = m_cacheData.vertices[t.b].position;
                var C = m_cacheData.vertices[t.c].position;
                return 0.5f * Vector3.Cross(B - A, C - A).magnitude;
            }

            public RandomPickerUniformArea(MeshData data, int seed) : base(data, seed)
            {
                m_accumulatedAreaTriangles = new double[data.triangles.Length];
                m_accumulatedAreaTriangles[0] = ComputeTriangleArea(data.triangles[0]);
                for (int i = 1; i < data.triangles.Length; ++i)
                {
                    m_accumulatedAreaTriangles[i] = m_accumulatedAreaTriangles[i - 1] + ComputeTriangleArea(data.triangles[i]);
                }
            }

            private uint FindIndexOfArea(double area)
            {
                uint min = 0;
                uint max = (uint)m_accumulatedAreaTriangles.Length - 1;
                uint mid = max >> 1;
                while (max >= min)
                {
                    if (mid > m_accumulatedAreaTriangles.Length)
                        throw new InvalidOperationException("Cannot Find FindIndexOfArea");

                    if (m_accumulatedAreaTriangles[mid] >= area &&
                        (mid == 0 || (m_accumulatedAreaTriangles[mid-1] < area)))
                    {
                        return mid;
                    }
                    else if (area < m_accumulatedAreaTriangles[mid])
                    {
                        max = mid - 1;
                    }
                    else
                    {
                        min = mid + 1;
                    }
                    mid = (min + max) >> 1;
                }
                throw new InvalidOperationException("Cannot FindIndexOfArea");
            }

            public override sealed MeshData.Vertex GetNext()
            {
                var areaPosition = m_Rand.NextDouble() * m_accumulatedAreaTriangles.Last();
                uint areaIndex = FindIndexOfArea(areaPosition);

                var rand = new Vector2(GetNextRandFloat(), GetNextRandFloat());
                return Interpolate(m_cacheData.triangles[areaIndex], rand);
            }
        }

        class SequentialPickerTriangle : Picker
        {
            private uint m_Index = 0;
            public SequentialPickerTriangle(MeshData data) : base(data)
            {
            }

            public override sealed MeshData.Vertex GetNext()
            {
                var t = m_cacheData.triangles[m_Index];
                m_Index++;
                if (m_Index >= m_cacheData.triangles.Length)
                    m_Index = 0;
                return Interpolate(t, center_of_sampling);
            }
        }

        PCache ComputePCacheFromMesh()
        {
            var meshCache = ComputeDataCache(m_Mesh);

            Picker picker = null;
            if (m_Distribution == Distribution.Sequential)
            {
                if (m_MeshBakeMode == MeshBakeMode.Vertex)
                {
                    picker = new SequentialPickerVertex(meshCache);
                }
                else if (m_MeshBakeMode == MeshBakeMode.Triangle)
                {
                    picker = new SequentialPickerTriangle(meshCache);
                }
            }
            else if (m_Distribution == Distribution.Random)
            {
                if (m_MeshBakeMode == MeshBakeMode.Vertex)
                {
                    picker = new RandomPickerVertex(meshCache, m_SeedMesh);
                }
                else if (m_MeshBakeMode == MeshBakeMode.Triangle)
                {
                    picker = new RandomPickerTriangle(meshCache, m_SeedMesh);
                }
            }
            else if (m_Distribution == Distribution.RandomUniformArea)
            {
                picker = new RandomPickerUniformArea(meshCache, m_SeedMesh);
            }
            if (picker == null)
                throw new InvalidOperationException("Unable to find picker");

            var positions = new List<Vector3>();
            var normals = m_ExportNormals ? new List<Vector3>() : null;
            var colors = m_ExportColors ? new List<Vector4>() : null;
            var uvs = m_ExportUV ? new List<Vector4>() : null;

            for (int i = 0; i < m_OutputPointCount; ++i)
            {
                if (i % 64 == 0)
                {
                    var cancel = EditorUtility.DisplayCancelableProgressBar("pCache bake tool", string.Format("Sampling data... {0}/{1}", i, m_OutputPointCount), (float)i / (float)m_OutputPointCount);
                    if (cancel)
                    {
                        return null;
                    }
                }

                var vertex = picker.GetNext();
                positions.Add(vertex.position);
                if (m_ExportNormals) normals.Add(vertex.normal);
                if (m_ExportColors) colors.Add(vertex.color);
                if (m_ExportUV) uvs.Add(vertex.uvs.Any() ? vertex.uvs[0] : Vector4.zero);
            }

            var file = new PCache();
            file.AddVector3Property("position");
            if (m_ExportNormals) file.AddVector3Property("normal");
            if (m_ExportColors) file.AddColorProperty("color");
            if (m_ExportUV) file.AddVector4Property("uv");

            EditorUtility.DisplayProgressBar("pCache bake tool", "Generating pCache...", 0.0f);
            file.SetVector3Data("position", positions);
            if (m_ExportNormals) file.SetVector3Data("normal", normals);
            if (m_ExportColors) file.SetColorData("color", colors);
            if (m_ExportUV) file.SetVector4Data("uv", uvs);

            EditorUtility.ClearProgressBar();
            return file;
        }

        static partial class Contents
        {
            public static GUIContent meshBakeMode = new GUIContent("Bake Mode");
            public static GUIContent distribution = new GUIContent("Distribution");
            public static GUIContent mesh = new GUIContent("Mesh");
        }
    }
}
