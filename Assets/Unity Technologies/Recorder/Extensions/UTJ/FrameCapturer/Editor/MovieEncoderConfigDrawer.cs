using UnityEngine;
using UnityEditor;

namespace UTJ.FrameCapturer
{
    [CustomPropertyDrawer(typeof(MovieEncoderConfigs))]
    class MovieEncoderConfigsDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0.0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var type = property.FindPropertyRelative("format");
            EditorGUILayout.PropertyField(type);
            EditorGUI.indentLevel++;
            switch ((MovieEncoder.Type)type.intValue)
            {
                case MovieEncoder.Type.Png:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("pngEncoderSettings"), true);
                    break;
                case MovieEncoder.Type.Exr:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("exrEncoderSettings"), true);
                    break;
                case MovieEncoder.Type.Gif:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("gifEncoderSettings"), true);
                    break;
                case MovieEncoder.Type.WebM:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("webmEncoderSettings"), true);
                    break;
                case MovieEncoder.Type.MP4:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("mp4EncoderSettings"), true);
                    break;
            }
            EditorGUI.indentLevel--;
        }
    }
}
