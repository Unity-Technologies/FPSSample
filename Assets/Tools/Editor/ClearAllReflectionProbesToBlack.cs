using UnityEngine;
using UnityEditor;


public static class ClearAllReflectionProbesToBlack

{
    [MenuItem("FPS Sample/Lighting/Reflection Probes/Clear to Black")]

    static void ClearAllReflection()
    {
        ReflectionProbe[] probes = Component.FindObjectsOfType<ReflectionProbe>();
        foreach (var rp in probes)
        {
            rp.bakedTexture = null;
        }

        foreach (var rp in probes)
        {
            rp.bakedTexture = GetDefaultBlackCube();
        }
    }

    private static void ClearBakedTexture(Texture cubemap)
    {
        Cubemap p = cubemap as Cubemap;
        FillCubemapFace(p, CubemapFace.PositiveX, Color.black);
        FillCubemapFace(p, CubemapFace.PositiveY, Color.black);
        FillCubemapFace(p, CubemapFace.PositiveZ, Color.black);
        FillCubemapFace(p, CubemapFace.NegativeX, Color.black);
        FillCubemapFace(p, CubemapFace.NegativeY, Color.black);
        FillCubemapFace(p, CubemapFace.NegativeZ, Color.black);
        p.Apply();
    }

    private static void FillCubemapFace(Cubemap c, CubemapFace f, Color color)
    {
        Color[] colors = c.GetPixels(f);
        for (int i = 0; i < colors.Length; i++) colors[i] = color;
        c.SetPixels(colors, f);
    }

    private static Cubemap GetDefaultBlackCube()
    {
        var cm = new Cubemap(1, TextureFormat.RGB24, true);
        cm.SetPixels(new Color[] { Color.black }, CubemapFace.NegativeX, 0);
        cm.SetPixels(new Color[] { Color.black }, CubemapFace.PositiveX, 0);
        cm.SetPixels(new Color[] { Color.black }, CubemapFace.NegativeY, 0);
        cm.SetPixels(new Color[] { Color.black }, CubemapFace.PositiveY, 0);
        cm.SetPixels(new Color[] { Color.black }, CubemapFace.NegativeZ, 0);
        cm.SetPixels(new Color[] { Color.black }, CubemapFace.PositiveZ, 0);
        cm.Apply();
        return cm;
    }
}