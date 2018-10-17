using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [VolumeParameterDrawer(typeof(ColorParameter))]
    sealed class ColorParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Color)
                return false;

            var o = parameter.GetObjectRef<ColorParameter>();
            value.colorValue = EditorGUILayout.ColorField(title, value.colorValue, o.showEyeDropper, o.showAlpha, o.hdr);
            return true;
        }
    }
}
