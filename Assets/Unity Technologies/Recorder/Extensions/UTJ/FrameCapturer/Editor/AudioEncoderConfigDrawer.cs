using UnityEngine;
using UnityEditor;

namespace UTJ.FrameCapturer
{
    [CustomPropertyDrawer(typeof(AudioEncoderConfigs))]
    class AudioEncoderConfigsDrawer : PropertyDrawer
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
            switch ((AudioEncoder.Type)type.intValue)
            {
                case AudioEncoder.Type.Wave:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("waveEncoderSettings"), true);
                    break;
                case AudioEncoder.Type.Ogg:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("oggEncoderSettings"), true);
                    break;
                case AudioEncoder.Type.Flac:
                    EditorGUILayout.PropertyField(property.FindPropertyRelative("flacEncoderSettings"), true);
                    break;
            }
            EditorGUI.indentLevel--;
        }
    }
}
