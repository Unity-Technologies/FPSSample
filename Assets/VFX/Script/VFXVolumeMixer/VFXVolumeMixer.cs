using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class VFXVolumeMixer : VolumeComponent
{
    public FloatParameter CustomFloatParameter1 = new FloatParameter(0.0f);
    public FloatParameter CustomFloatParameter2 = new FloatParameter(0.0f);
    public FloatParameter CustomFloatParameter3 = new FloatParameter(0.0f);
    public FloatParameter CustomFloatParameter4 = new FloatParameter(0.0f);
    public FloatParameter CustomFloatParameter5 = new FloatParameter(0.0f);
    public FloatParameter CustomFloatParameter6 = new FloatParameter(0.0f);
    public FloatParameter CustomFloatParameter7 = new FloatParameter(0.0f);
    public FloatParameter CustomFloatParameter8 = new FloatParameter(0.0f);

    public Vector3Parameter CustomVector3Parameter1 = new Vector3Parameter(Vector3.zero);
    public Vector3Parameter CustomVector3Parameter2 = new Vector3Parameter(Vector3.zero);
    public Vector3Parameter CustomVector3Parameter3 = new Vector3Parameter(Vector3.zero);
    public Vector3Parameter CustomVector3Parameter4 = new Vector3Parameter(Vector3.zero);
    public Vector3Parameter CustomVector3Parameter5 = new Vector3Parameter(Vector3.zero);
    public Vector3Parameter CustomVector3Parameter6 = new Vector3Parameter(Vector3.zero);
    public Vector3Parameter CustomVector3Parameter7 = new Vector3Parameter(Vector3.zero);
    public Vector3Parameter CustomVector3Parameter8 = new Vector3Parameter(Vector3.zero);

    public ColorParameter CustomColorParameter1 = new ColorParameter(Color.white, true, false, true);
    public ColorParameter CustomColorParameter2 = new ColorParameter(Color.white, true, false, true);
    public ColorParameter CustomColorParameter3 = new ColorParameter(Color.white, true, false, true);
    public ColorParameter CustomColorParameter4 = new ColorParameter(Color.white, true, false, true);
    public ColorParameter CustomColorParameter5 = new ColorParameter(Color.white, true, false, true);
    public ColorParameter CustomColorParameter6 = new ColorParameter(Color.white, true, false, true);
    public ColorParameter CustomColorParameter7 = new ColorParameter(Color.white, true, false, true);
    public ColorParameter CustomColorParameter8 = new ColorParameter(Color.white, true, false, true);

    public static VolumeStack stack
    {
        get
        {
            if (s_Stack == null)
                s_Stack = VolumeManager.instance.CreateStack();
            return s_Stack;
        }
    }
    static VolumeStack s_Stack; 

    static void UpdateStack(Transform trigger, LayerMask layerMask)
    {
        VolumeManager.instance.Update(stack, trigger, layerMask);
    }

    public static float GetFloatValueAt(int index, Transform trigger, LayerMask layerMask)
    {
        UpdateStack(trigger, layerMask);
        return GetFloatValueAt(index);
    }

    public static float GetFloatValueAt(int index)
    {
        var component = stack.GetComponent<VFXVolumeMixer>();
        
        switch(index)
        {
            default: throw new System.IndexOutOfRangeException();
            case 0: return component.CustomFloatParameter1.value; 
            case 1: return component.CustomFloatParameter2.value; 
            case 2: return component.CustomFloatParameter3.value; 
            case 3: return component.CustomFloatParameter4.value; 
            case 4: return component.CustomFloatParameter5.value; 
            case 5: return component.CustomFloatParameter6.value; 
            case 6: return component.CustomFloatParameter7.value; 
            case 7: return component.CustomFloatParameter8.value; 
        }
    }

    public static Vector3 GetVectorValueAt(int index, Transform trigger, LayerMask layerMask)
    {
        UpdateStack(trigger, layerMask);
        return GetVectorValueAt(index);
    }

    public static Vector3 GetVectorValueAt(int index)
    {
        var component = stack.GetComponent<VFXVolumeMixer>();

        switch (index)
        {
            default: throw new System.IndexOutOfRangeException();
            case 0: return component.CustomVector3Parameter1.value;
            case 1: return component.CustomVector3Parameter2.value;
            case 2: return component.CustomVector3Parameter3.value;
            case 3: return component.CustomVector3Parameter4.value;
            case 4: return component.CustomVector3Parameter5.value;
            case 5: return component.CustomVector3Parameter6.value;
            case 6: return component.CustomVector3Parameter7.value;
            case 7: return component.CustomVector3Parameter8.value;
        }
    }

    public static Color GetColorValueAt(int index, Transform trigger, LayerMask layerMask)
    {
        UpdateStack(trigger, layerMask);
        return GetColorValueAt(index);
    }

    public static Color GetColorValueAt(int index)
    {
        var component = stack.GetComponent<VFXVolumeMixer>();

        switch (index)
        {
            default: throw new System.IndexOutOfRangeException();
            case 0: return component.CustomColorParameter1.value;
            case 1: return component.CustomColorParameter2.value;
            case 2: return component.CustomColorParameter3.value;
            case 3: return component.CustomColorParameter4.value;
            case 4: return component.CustomColorParameter5.value;
            case 5: return component.CustomColorParameter6.value;
            case 6: return component.CustomColorParameter7.value;
            case 7: return component.CustomColorParameter8.value;
        }
    }
}
