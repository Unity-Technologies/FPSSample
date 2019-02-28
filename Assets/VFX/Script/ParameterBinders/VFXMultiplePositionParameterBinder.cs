using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.VFX.Utils;

[VFXBinder("Utility/Multiple Position Binder")]
public class VFXMultiplePositionParameterBinder : VFXBinderBase
{
    [VFXParameterBinding("UnityEngine.Texture2D")]
    public ExposedParameter PositionMapParameter = "PositionMap";
    [VFXParameterBinding("System.Int32")]
    public ExposedParameter PositionCountParameter = "PositionCount";

    public GameObject[] Targets;
    public bool EveryFrame = false;

    private Texture2D positionMap;
    private int count = 0;

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateTexture();
    }

    public override bool IsValid(VisualEffect component)
    {
        return Targets != null &&
            component.HasTexture(PositionMapParameter) &&
            component.HasInt(PositionCountParameter);
    }

    public override void UpdateBinding(VisualEffect component)
    {
        if (EveryFrame || Application.isEditor)
            UpdateTexture();

        component.SetTexture(PositionMapParameter, positionMap);
        component.SetInt(PositionCountParameter, count);
    }

    void UpdateTexture()
    {
        if (Targets == null || Targets.Length == 0)
            return;

        var candidates = new List<Vector3>();

        foreach (var obj in Targets)
        {
            if(obj != null)
                candidates.Add(obj.transform.position);
        }

        count = candidates.Count;

        if (positionMap == null || positionMap.width != count)
        {
            positionMap = new Texture2D(count, 1, TextureFormat.RGBAFloat, false);
        }

        List<Color> colors = new List<Color>();
        foreach (var pos in candidates)
        {
            colors.Add(new Color(pos.x, pos.y, pos.z));
        }
        positionMap.name = gameObject.name + "_PositionMap";
        positionMap.filterMode = FilterMode.Point;
        positionMap.wrapMode = TextureWrapMode.Repeat;
        positionMap.SetPixels(colors.ToArray(), 0);
        positionMap.Apply();
    }

    public override string ToString()
    {
        return string.Format("Multiple Position Binder ({0} positions)", count);
    }

}
