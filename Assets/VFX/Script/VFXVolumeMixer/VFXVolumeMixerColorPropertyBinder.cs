using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.VFX.Utils;

[VFXBinder("VFX Volume Mixer/Color Property Binder")]
public class VFXVolumeMixerColorPropertyBinder : VFXVolumeMixerPropertyBinderBase
{
    [VFXVolumeMixerProperty(VFXVolumeMixerPropertyAttribute.PropertyType.Color)]
    public int ColorMixerProperty = 0;
    [VFXParameterBinding("UnityEngine.Color")]
    public ExposedParameter ColorParameter = "Parameter";

    public override bool IsValid(VisualEffect component)
    {
        return base.IsValid(component) && ColorMixerProperty < 8 && ColorMixerProperty >= 0 && computedTransform != null && component.HasVector4(ColorParameter);
    }

    public override void UpdateBinding(VisualEffect component)
    {
        component.SetVector4(ColorParameter, VFXVolumeMixer.GetColorValueAt(ColorMixerProperty, computedTransform, Layer)); 
    }

    public override string ToString()
    {
        return "VFXVolumeMixer Color #" + ColorMixerProperty + " : " + ColorParameter.ToString() + " " + base.ToString();
    }
}
