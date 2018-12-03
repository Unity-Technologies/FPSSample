using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFrame : MaskableGraphic
{
    public float width = 3;
    public float offset = 0;

    private Vector3 Snap(Vector3 p)
    {
        return RectTransformUtility.PixelAdjustPoint(p, transform, canvas);
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        base.OnPopulateMesh(vh);
        vh.Clear();

        // Trying to build a pixel accurate frame. Surprisingly annoying :-)

        var rect = GetPixelAdjustedRect();

        var w = rect.width;
        var h = rect.height;

        var vLL = (new Vector3(0 - rectTransform.pivot.x * w + offset, 0 - rectTransform.pivot.y * h + offset));
        var vLR = (new Vector3(w - rectTransform.pivot.x * w - offset, 0 - rectTransform.pivot.y * h + offset));
        var vUR = (new Vector3(w - rectTransform.pivot.x * w - offset, h - rectTransform.pivot.y * h - offset));
        var vUL = (new Vector3(0 - rectTransform.pivot.x * w + offset, h - rectTransform.pivot.y * h - offset));

        var vMid = new Vector3((vLL.x + vLR.x) * 0.5f, (vLL.y + vUL.y) * 0.5f, 0);
        var vdLL  = Snap(vLL) - vMid;
        var vdLR = new Vector3(-vdLL.x, vdLL.y);
        var vdUR = new Vector3(-vdLL.x, -vdLL.y);
        var vdUL = new Vector3(vdLL.x, -vdLL.y);

        var vdLLi = Snap(vLL + new Vector3(width, width)) - Snap(vLL);
        var maxd = Mathf.Max(vdLLi.x, vdLLi.y);
        vdLLi = Snap(vLL) + new Vector3(maxd, maxd) - vMid;
        var vdLRi = new Vector3(-vdLLi.x, vdLLi.y);
        var vdURi = new Vector3(-vdLLi.x, -vdLLi.y);
        var vdULi = new Vector3(vdLLi.x, -vdLLi.y);

        vLL = vMid + vdLL;
        vLR = vMid + vdLR;
        vUR = vMid + vdUR;
        vUL = vMid + vdUL;

        var vLLi = vMid + vdLLi;
        var vLRi = vMid + vdLRi;
        var vURi = vMid + vdURi;
        var vULi = vMid + vdULi;

        vh.AddVert(vLL, color, Vector2.zero); 
        vh.AddVert(vLR, color, Vector2.zero); 
        vh.AddVert(vLRi, color, Vector2.zero); 
        vh.AddVert(vLLi, color, Vector2.zero);

        vh.AddTriangle(0, 2, 1);
        vh.AddTriangle(0, 3, 2);

        vh.AddVert(vUL, color, Vector2.zero); 
        vh.AddVert(vUR, color, Vector2.zero); 
        vh.AddVert(vURi, color, Vector2.zero); 
        vh.AddVert(vULi, color, Vector2.zero);

        vh.AddTriangle(4, 6, 5);
        vh.AddTriangle(4, 7, 6);

        vh.AddVert(vLL, color, Vector2.zero);
        vh.AddVert(vUL, color, Vector2.zero);
        vh.AddVert(vULi, color, Vector2.zero);
        vh.AddVert(vLLi, color, Vector2.zero);

        vh.AddTriangle(8, 9, 10);
        vh.AddTriangle(8, 10, 11);

        vh.AddVert(vLRi, color, Vector2.zero);
        vh.AddVert(vURi, color, Vector2.zero);
        vh.AddVert(vUR, color, Vector2.zero);
        vh.AddVert(vLR, color, Vector2.zero);

        vh.AddTriangle(12, 13, 14);
        vh.AddTriangle(12, 14, 15);
    }
}
