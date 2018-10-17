using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [VolumeParameterDrawer(typeof(MinFloatParameter))]
    sealed class MinFloatParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Float)
                return false;

            var o = parameter.GetObjectRef<MinFloatParameter>();
            float v = EditorGUILayout.FloatField(title, value.floatValue);
            value.floatValue = Mathf.Max(v, o.min);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(NoInterpMinFloatParameter))]
    sealed class NoInterpMinFloatParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Float)
                return false;

            var o = parameter.GetObjectRef<NoInterpMinFloatParameter>();
            float v = EditorGUILayout.FloatField(title, value.floatValue);
            value.floatValue = Mathf.Max(v, o.min);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(MaxFloatParameter))]
    sealed class MaxFloatParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Float)
                return false;

            var o = parameter.GetObjectRef<MaxFloatParameter>();
            float v = EditorGUILayout.FloatField(title, value.floatValue);
            value.floatValue = Mathf.Min(v, o.max);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(NoInterpMaxFloatParameter))]
    sealed class NoInterpMaxFloatParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Float)
                return false;

            var o = parameter.GetObjectRef<NoInterpMaxFloatParameter>();
            float v = EditorGUILayout.FloatField(title, value.floatValue);
            value.floatValue = Mathf.Min(v, o.max);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(ClampedFloatParameter))]
    sealed class ClampedFloatParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Float)
                return false;

            var o = parameter.GetObjectRef<ClampedFloatParameter>();
            EditorGUILayout.Slider(value, o.min, o.max, title);
            value.floatValue = Mathf.Clamp(value.floatValue, o.min, o.max);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(NoInterpClampedFloatParameter))]
    sealed class NoInterpClampedFloatParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Float)
                return false;

            var o = parameter.GetObjectRef<NoInterpClampedFloatParameter>();
            EditorGUILayout.Slider(value, o.min, o.max, title);
            value.floatValue = Mathf.Clamp(value.floatValue, o.min, o.max);
            return true;
        }
    }

    [VolumeParameterDrawer(typeof(FloatRangeParameter))]
    sealed class FloatRangeParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Vector2)
                return false;

            var o = parameter.GetObjectRef<FloatRangeParameter>();
            var v = value.vector2Value;

            // The layout system breaks alignement when mixing inspector fields with custom layouted
            // fields as soon as a scrollbar is needed in the inspector, so we'll do the layout
            // manually instead
            const int kFloatFieldWidth = 50;
            const int kSeparatorWidth = 5;
            float indentOffset = EditorGUI.indentLevel * 15f;
            var lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            lineRect.xMin += 4f;
            lineRect.y += 2f;
            var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
            var floatFieldLeft = new Rect(labelRect.xMax, lineRect.y, kFloatFieldWidth + indentOffset, lineRect.height);
            var sliderRect = new Rect(floatFieldLeft.xMax + kSeparatorWidth - indentOffset, lineRect.y, lineRect.width - labelRect.width - kFloatFieldWidth * 2 - kSeparatorWidth * 2, lineRect.height);
            var floatFieldRight = new Rect(sliderRect.xMax + kSeparatorWidth - indentOffset, lineRect.y, kFloatFieldWidth + indentOffset, lineRect.height);

            EditorGUI.PrefixLabel(labelRect, title);
            v.x = EditorGUI.FloatField(floatFieldLeft, v.x);
            EditorGUI.MinMaxSlider(sliderRect, ref v.x, ref v.y, o.min, o.max);
            v.y = EditorGUI.FloatField(floatFieldRight, v.y);

            value.vector2Value = v;
            return true;
        }
    }
}
