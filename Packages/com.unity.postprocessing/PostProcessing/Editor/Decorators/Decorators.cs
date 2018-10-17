using System;
using UnityEngine;

namespace UnityEditor.Rendering.PostProcessing
{
    [Decorator(typeof(RangeAttribute))]
    public sealed class RangeDecorator : AttributeDecorator
    {
        public override bool OnGUI(SerializedProperty property, SerializedProperty overrideState, GUIContent title, Attribute attribute)
        {
            var attr = (RangeAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.Float)
            {
                property.floatValue = EditorGUILayout.Slider(title, property.floatValue, attr.min, attr.max);
                return true;
            }

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = EditorGUILayout.IntSlider(title, property.intValue, (int)attr.min, (int)attr.max);
                return true;
            }

            return false;
        }
    }

    [Decorator(typeof(UnityEngine.Rendering.PostProcessing.MinAttribute))]
    public sealed class MinDecorator : AttributeDecorator
    {
        public override bool OnGUI(SerializedProperty property, SerializedProperty overrideState, GUIContent title, Attribute attribute)
        {
            var attr = (UnityEngine.Rendering.PostProcessing.MinAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.Float)
            {
                float v = EditorGUILayout.FloatField(title, property.floatValue);
                property.floatValue = Mathf.Max(v, attr.min);
                return true;
            }

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int v = EditorGUILayout.IntField(title, property.intValue);
                property.intValue = Mathf.Max(v, (int)attr.min);
                return true;
            }

            return false;
        }
    }

    [Decorator(typeof(UnityEngine.Rendering.PostProcessing.MaxAttribute))]
    public sealed class MaxDecorator : AttributeDecorator
    {
        public override bool OnGUI(SerializedProperty property, SerializedProperty overrideState, GUIContent title, Attribute attribute)
        {
            var attr = (UnityEngine.Rendering.PostProcessing.MaxAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.Float)
            {
                float v = EditorGUILayout.FloatField(title, property.floatValue);
                property.floatValue = Mathf.Min(v, attr.max);
                return true;
            }

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int v = EditorGUILayout.IntField(title, property.intValue);
                property.intValue = Mathf.Min(v, (int)attr.max);
                return true;
            }

            return false;
        }
    }

    [Decorator(typeof(UnityEngine.Rendering.PostProcessing.MinMaxAttribute))]
    public sealed class MinMaxDecorator : AttributeDecorator
    {
        public override bool OnGUI(SerializedProperty property, SerializedProperty overrideState, GUIContent title, Attribute attribute)
        {
            var attr = (UnityEngine.Rendering.PostProcessing.MinMaxAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.Float)
            {
                float v = EditorGUILayout.FloatField(title, property.floatValue);
                property.floatValue = Mathf.Clamp(v, attr.min, attr.max);
                return true;
            }

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int v = EditorGUILayout.IntField(title, property.intValue);
                property.intValue = Mathf.Clamp(v, (int)attr.min, (int)attr.max);
                return true;
            }

            if (property.propertyType == SerializedPropertyType.Vector2)
            {
                var v = property.vector2Value;
                EditorGUILayout.MinMaxSlider(title, ref v.x, ref v.y, attr.min, attr.max);
                property.vector2Value = v;
                return true;
            }

            return false;
        }
    }

    [Decorator(typeof(ColorUsageAttribute))]
    public sealed class ColorUsageDecorator : AttributeDecorator
    {
        public override bool OnGUI(SerializedProperty property, SerializedProperty overrideState, GUIContent title, Attribute attribute)
        {
            var attr = (ColorUsageAttribute)attribute;

            if (property.propertyType != SerializedPropertyType.Color)
                return false;

#if UNITY_2018_1_OR_NEWER
            property.colorValue = EditorGUILayout.ColorField(title, property.colorValue, true, attr.showAlpha, attr.hdr);
#else
            ColorPickerHDRConfig hdrConfig = null;

            if (attr.hdr)
            {
                hdrConfig = new ColorPickerHDRConfig(
                    attr.minBrightness,
                    attr.maxBrightness,
                    attr.minExposureValue,
                    attr.maxExposureValue
                );
            }

            property.colorValue = EditorGUILayout.ColorField(title, property.colorValue, true, attr.showAlpha, attr.hdr, hdrConfig);
#endif

            return true;
        }
    }
}
