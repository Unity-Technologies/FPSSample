using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Utils
{
    public partial class PointCacheBakeTool : EditorWindow
    {
        public enum DecimationThresholdMode
        {
            None,
            Alpha,
            Luminance,
            R,
            G,
            B
        }

        Texture2D m_Texture;
        bool m_RandomizePixels;
        int m_SeedPixels;
        DecimationThresholdMode m_DecimationThresholdMode = DecimationThresholdMode.Alpha;
        float m_Threshold = 0.33333f;

        void OnGUI_Texture()
        {
            GUILayout.Label("Texture baking", EditorStyles.boldLabel);

            m_Texture = (Texture2D)EditorGUILayout.ObjectField("Texture", m_Texture, typeof(Texture2D), false);

            m_DecimationThresholdMode = (DecimationThresholdMode)EditorGUILayout.EnumPopup("Decimation Threshold", m_DecimationThresholdMode);
            if (m_DecimationThresholdMode != DecimationThresholdMode.None)
                m_Threshold = EditorGUILayout.Slider("Threshold", m_Threshold, 0.0f, 1.0f);

            m_RandomizePixels = EditorGUILayout.Toggle("Randomize Pixels Order", m_RandomizePixels);
            if (m_RandomizePixels)
                m_SeedPixels = EditorGUILayout.IntField("Seed", m_SeedPixels);
            m_ExportColors = EditorGUILayout.Toggle("Export Colors", m_ExportColors);

            m_OutputFormat = (PCache.Format)EditorGUILayout.EnumPopup("File Format", m_OutputFormat);

            if (m_Texture != null)
            {
                if (GUILayout.Button("Save to pCache file..."))
                {
                    string fileName = EditorUtility.SaveFilePanelInProject("pCacheFile", m_Texture.name, "pcache", "Save PCache");
                    if (fileName != null)
                    {
                        PCache file = new PCache();
                        file.AddVector3Property("position");
                        if (m_ExportColors) file.AddColorProperty("color");

                        List<Vector3> positions = new List<Vector3>();
                        List<Vector4> colors = null;

                        if (m_ExportColors) colors = new List<Vector4>();

                        ComputeTextureData(positions, colors);
                        file.SetVector3Data("position", positions);
                        if (m_ExportColors)
                            file.SetColorData("color", colors);

                        file.SaveToFile(fileName, m_OutputFormat);
                    }
                }

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Texture Statistics", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.IntField("Width", m_Texture.width);
                    EditorGUILayout.IntField("Height", m_Texture.height);
                    EditorGUILayout.IntField("Pixels count", m_Texture.width * m_Texture.height);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private static void Swap<T>(IList<T> list, int a, int b)
        {
            T tmp = list[a];
            list[a] = list[b];
            list[b] = tmp;
        }

        void ComputeTextureData(List<Vector3> positions, List<Vector4> colors)
        {
            Color[] pixels = m_Texture.GetPixels();
            int width = m_Texture.width;
            int height = m_Texture.height;
            for (int i = 0; i < pixels.Length; ++i)
            {
                var color = pixels[i];
                if (m_DecimationThresholdMode != DecimationThresholdMode.None)
                {
                    float value;
                    switch (m_DecimationThresholdMode)
                    {
                        case DecimationThresholdMode.R: value = color.r; break;
                        case DecimationThresholdMode.G: value = color.g; break;
                        case DecimationThresholdMode.B: value = color.b; break;
                        case DecimationThresholdMode.Alpha: value = color.a; break;
                        case DecimationThresholdMode.Luminance: value = color.grayscale; break;
                        default: throw new System.NotImplementedException();
                    }
                    if (value < m_Threshold)
                        continue;
                }

                var fx = (float)(i % width) / width;
                var fy = (float)(i / width) / height;
                positions.Add(new Vector3(fx - 0.5f, fy - 0.5f, 0.0f));
                if (colors != null)
                {
                    colors.Add(color);
                }
            }

            if (m_RandomizePixels && positions.Any())
            {
                var random = new System.Random((int)m_SeedPixels);
                //Fisher-Yates shuffle
                for (int i = 0; i < positions.Count; ++i)
                {
                    int newIndex = i + random.Next(positions.Count - i);
                    Swap(positions, i, newIndex);
                    if (colors != null)
                    {
                        Swap(colors, i, newIndex);
                    }
                }
            }
        }
    }
}
