using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class SPTDistribution
    {
        static SPTDistribution s_Instance;

        public static SPTDistribution instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new SPTDistribution();

                return s_Instance;
            }
        }

        int m_refCounting;

        Texture2D m_PivotData;

        const int k_PivotLUTResolution = 64;
        const int k_PivotLUTEntryDim = 4; // the s_PivotLUTData array has 4 components for each entry, we only upload and use first 2

        SPTDistribution()
        {
            m_refCounting = 0;
        }

        // Load LUT data in texture
        void LoadLUT(Texture2D tex, double[,] PivotData)
        {
            const int count = k_PivotLUTResolution * k_PivotLUTResolution;
            Color[] pixels = new Color[count];

            for (int i = 0; i < count; i++)
            {
                pixels[i] = new Color((float)PivotData[i, 0], (float)PivotData[i, 1], 0.0f, 0.0f);
            }

            tex.SetPixels(pixels);
        }

        public void Build()
        {
            Debug.Assert(m_refCounting >= 0);

            if (m_refCounting == 0)
            {
                m_PivotData = new Texture2D(k_PivotLUTResolution, k_PivotLUTResolution, TextureFormat.RGHalf, false /*mipmap*/, true /* linear */)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = CoreUtils.GetTextureAutoName(k_PivotLUTResolution, k_PivotLUTResolution, TextureFormat.RGHalf, depth: 0, dim: TextureDimension.Tex2D, name: "PIVOT_LUT")
                };

                LoadLUT(m_PivotData, s_PivotLUTData);

                m_PivotData.Apply();
            }

            m_refCounting++;
        }

        public void Cleanup()
        {
            m_refCounting--;

            if (m_refCounting == 0)
            {
                CoreUtils.Destroy(m_PivotData);
            }

            Debug.Assert(m_refCounting >= 0);
        }

        public void Bind()
        {
            Shader.SetGlobalTexture("_PivotData", m_PivotData);
        }
    }
}
