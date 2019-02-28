using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class VectorFieldGenerator : EditorWindow
{
    GUIContent m_Title = new GUIContent("VF Generator");
    [SerializeField]
    Texture2D m_Texture = null;
    TextureFormat m_TextureFormat = TextureFormat.RGBAHalf;
    public bool m_UnsignedNormalized = false;
    TextureWrapMode m_WrapMode = TextureWrapMode.Clamp;
    FilterMode m_FilterMode = FilterMode.Bilinear;

    [MenuItem("Window/Visual Effects/(Deprecated)/Generate 3D Texture", false, 3012, true)]
    public static void OpenConverter()
    {
        EditorWindow.GetWindow<VectorFieldGenerator>();
    }

    void OnGUI()
    {
        titleContent = m_Title;
        using (new GUILayout.VerticalScope())
        {
            Texture2D t = m_Texture;
            m_Texture = (Texture2D)EditorGUILayout.ObjectField(m_Texture, typeof(Texture2D), false);
            if (m_Texture != t)
            {
                m_TextureFormat = m_Texture.format;
            }

            m_UnsignedNormalized = EditorGUILayout.Toggle("Unsigned Normalized", m_UnsignedNormalized);

            if (m_Texture != null)
            {
                EditorGUILayout.LabelField("Texture Size", m_Texture.width + "x" + m_Texture.height);
                if (m_Texture.height * m_Texture.height == m_Texture.width)
                {
                    EditorGUILayout.LabelField("Projected Size", m_Texture.height.ToString());
                }
                else
                {
                    GUI.color = Color.red * 4;
                    EditorGUILayout.LabelField("Projected Size", "Invalid Size for a 3D Texture!");
                    GUI.color = Color.white;
                }
            }

            m_TextureFormat = (TextureFormat)EditorGUILayout.EnumPopup(new GUIContent("Texture Format"), m_TextureFormat);
            m_FilterMode = (FilterMode)EditorGUILayout.EnumPopup(new GUIContent("Filter Mode"), m_FilterMode);
            m_WrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup(new GUIContent("Wrap Mode"), m_WrapMode);

            if (m_Texture != null)
            {
                GUILayout.Space(20);
                if (GUILayout.Button("Export To 3DTexture Asset", GUILayout.Height(32)))
                {
                    if (m_Texture.width == (m_Texture.height * m_Texture.height))
                    {
                        int wsize = m_Texture.height;
                        Texture3D VectorField = new Texture3D(wsize, wsize, wsize, m_TextureFormat, false);

                        Color[] data = new Color[wsize * wsize * wsize];
                        for (int i = 0; i < wsize; i++)
                            for (int j = 0; j < wsize; j++)
                                for (int k = 0; k < wsize; k++)
                                {
                                    data[i + (j * wsize) + (k * wsize * wsize)] = m_Texture.GetPixel(i + (k * wsize), j);
                                }
                        VectorField.SetPixels(data);
                        VectorField.wrapMode = m_WrapMode;
                        VectorField.filterMode = m_FilterMode;
                        VectorField.Apply();

                        string path = EditorUtility.SaveFilePanelInProject("Save Texture", m_Texture.name + "_VF.asset", "asset", "Save a Texture");
                        AssetDatabase.CreateAsset(VectorField, path);
                    }
                    else
                        Debug.LogError("Can only convert textures of NxNxN unwrapped on U axis : here texture is : " + m_Texture.width + "x" + m_Texture.height);
                }
            }
            else
            {
                if (GUILayout.Button("Load VF File & Export To 3DTexture Asset", GUILayout.Height(32)))
                {
                    string fname = EditorUtility.OpenFilePanel("Open VF File", "", "vf");
                    Texture3D VectorField = LoadVFFile(fname, m_TextureFormat, m_UnsignedNormalized);
                    VectorField.wrapMode = m_WrapMode;
                    VectorField.filterMode = m_FilterMode;
                    VectorField.Apply();

                    string path = EditorUtility.SaveFilePanelInProject("Save Texture", fname + "_VF.asset", "asset", "Save a Texture");
                    AssetDatabase.CreateAsset(VectorField, path);
                }
            }
        }
    }

    public static Texture3D LoadVFFile(string filename, TextureFormat textureformat, bool unsignedNormalized)
    {
        BinaryReader br = new BinaryReader(File.OpenRead(filename));
        string fourcc = new string(br.ReadChars(4));
        ushort size_x = br.ReadUInt16();
        ushort size_y = br.ReadUInt16();
        ushort size_z = br.ReadUInt16();

        int mode = -1;
        if (fourcc == "VF_F")
            mode = 0;
        else if (fourcc == "VF_V")
            mode = 1;
        else
            throw new System.Exception("Invalid VF FourCC");

        if (mode != -1)
        {
            Texture3D outputFile = new Texture3D(size_x, size_y, size_z, textureformat, false);
            ulong length = (ulong)size_x * (ulong)size_y * (ulong)size_z;
            Color[] colors = new Color[length];

            for (ulong i = 0; i < length; i++)
            {
                if (mode == 0)
                {
                    float val = br.ReadSingle();
                    if (unsignedNormalized)
                    {
                        val = val * 0.5f + 0.5f;
                    }
                    colors[i] = new Color(val, val, val);
                }
                else
                {
                    float r = br.ReadSingle();
                    float g = br.ReadSingle();
                    float b = br.ReadSingle();
                    if (unsignedNormalized)
                    {
                        r = r * 0.5f + 0.5f;
                        g = g * 0.5f + 0.5f;
                        b = b * 0.5f + 0.5f;
                    }
                    colors[i] = new Color(r, g, b);
                }
            }
            outputFile.SetPixels(colors);
            br.Close();
            return outputFile;
        }

        return null;
    }
}
