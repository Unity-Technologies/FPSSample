using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(VFXVolumeMixerPropertyAttribute))]
public class VFXVolumeMixerPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        VFXVolumeMixerPropertyAttribute.PropertyType type = (attribute as VFXVolumeMixerPropertyAttribute).type;

        string[] names;
        int[] values;
        int count;

        switch(type)
        {
            case VFXVolumeMixerPropertyAttribute.PropertyType.Float:
                count = VFXVolumeMixerSettings.floatPropertyCount;
                names = new string[count];
                values = new int[count];
                for(int i = 0; i < VFXVolumeMixerSettings.floatPropertyCount; i++)
                {
                    names[i] = VFXVolumeMixerSettings.floatPropertyNames[i];
                    values[i] = i;
                }
                property.intValue = EditorGUI.IntPopup(position, ObjectNames.NicifyVariableName(property.name), property.intValue, names, values);
                break;
            case VFXVolumeMixerPropertyAttribute.PropertyType.Vector:
                count = VFXVolumeMixerSettings.vectorPropertyCount;
                names = new string[count];
                values = new int[count];
                for (int i = 0; i < VFXVolumeMixerSettings.vectorPropertyCount; i++)
                {
                    names[i] = VFXVolumeMixerSettings.vectorPropertyNames[i];
                    values[i] = i;
                }
                property.intValue = EditorGUI.IntPopup(position, ObjectNames.NicifyVariableName(property.name), property.intValue, names, values);
                break;
            case VFXVolumeMixerPropertyAttribute.PropertyType.Color:
                count = VFXVolumeMixerSettings.colorPropertyCount;
                names = new string[count];
                values = new int[count];
                for (int i = 0; i < VFXVolumeMixerSettings.colorPropertyCount; i++)
                {
                    names[i] = VFXVolumeMixerSettings.colorPropertyNames[i];
                    values[i] = i;
                }
                property.intValue = EditorGUI.IntPopup(position, ObjectNames.NicifyVariableName(property.name), property.intValue, names, values);
                break;
        }
    }
}
