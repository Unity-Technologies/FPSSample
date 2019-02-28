using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.VFX;
using UnityEngine.VFX.Utils;

[VFXBinder("VFX Volume Mixer/Float Property Binder")]
public class VFXVolumeMixerFloatPropertyBinder : VFXVolumeMixerPropertyBinderBase
{
    [VFXVolumeMixerProperty(VFXVolumeMixerPropertyAttribute.PropertyType.Float)]
    public int FloatMixerProperty = 0;
    [VFXParameterBinding("System.Single")]
    public ExposedParameter FloatParameter = "Parameter";

    public override bool IsValid(VisualEffect component)
    {
        return base.IsValid(component) && FloatMixerProperty < 8 && FloatMixerProperty >= 0 && computedTransform != null && component.HasFloat(FloatParameter);
    }

    public override void UpdateBinding(VisualEffect component)
    {
        component.SetFloat(FloatParameter, VFXVolumeMixer.GetFloatValueAt(FloatMixerProperty, computedTransform, Layer));
    }

    public override string ToString()
    {
        return "VFXVolumeMixer Float #" + FloatMixerProperty + " : " + FloatParameter.ToString() + " " + base.ToString();
    }
}
