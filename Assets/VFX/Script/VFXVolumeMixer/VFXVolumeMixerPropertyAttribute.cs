using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXVolumeMixerPropertyAttribute : PropertyAttribute
{
    public enum PropertyType
    {
        Float,
        Vector,
        Color
    }

    public PropertyType type;

    public VFXVolumeMixerPropertyAttribute(PropertyType type)
    {
        this.type = type;
    }
}
