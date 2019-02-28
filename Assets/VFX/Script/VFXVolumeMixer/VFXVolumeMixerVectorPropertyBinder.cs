using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.VFX;
using UnityEngine.VFX.Utils;

[VFXBinder("VFX Volume Mixer/Vector Property Binder")]
public class VFXVolumeMixerVectorPropertyBinder : VFXVolumeMixerPropertyBinderBase
{
    [VFXVolumeMixerProperty( VFXVolumeMixerPropertyAttribute.PropertyType.Vector)]
    public int VectorMixerProperty = 0;
    [VFXParameterBinding("UnityEngine.Vector3")]
    public ExposedParameter VectorParameter = "Parameter";

    public override bool IsValid(VisualEffect component)
    {
        return base.IsValid(component) && VectorMixerProperty < 8 && VectorMixerProperty >= 0 && computedTransform != null && component.HasVector3(VectorParameter);
    }

    public override void UpdateBinding(VisualEffect component)
    {
        component.SetVector3(VectorParameter, VFXVolumeMixer.GetVectorValueAt(VectorMixerProperty, computedTransform, Layer));
    }

    public override string ToString()
    {
        return "VFXVolumeMixer Vector3 #"+ VectorMixerProperty+ " : " + VectorParameter.ToString()+" "+ base.ToString();
    }
}
