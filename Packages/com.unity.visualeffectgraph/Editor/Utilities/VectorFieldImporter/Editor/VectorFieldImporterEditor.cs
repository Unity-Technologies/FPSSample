using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.VFXToolbox
{
    [CustomEditor(typeof(VectorFieldImporter))]
    public class VectorFieldImporterEditor : ScriptedImporterEditor
    {
        SerializedProperty format;
        SerializedProperty wrapMode;
        SerializedProperty filterMode;
        SerializedProperty generateMipMaps;
        SerializedProperty anisoLevel;


        public override void OnEnable()
        {
            format = serializedObject.FindProperty("m_OutputFormat");
            wrapMode = serializedObject.FindProperty("m_WrapMode");
            filterMode = serializedObject.FindProperty("m_FilterMode");
            generateMipMaps = serializedObject.FindProperty("m_GenerateMipMaps");
            anisoLevel = serializedObject.FindProperty("m_AnisoLevel");
        }

        public override void OnInspectorGUI()
        {
            VectorFieldImporter.VectorFieldOutputFormat formatEnum = (VectorFieldImporter.VectorFieldOutputFormat)format.intValue;
            EditorGUI.BeginChangeCheck();
            formatEnum = (VectorFieldImporter.VectorFieldOutputFormat)EditorGUILayout.EnumPopup("Output Format", formatEnum);
            if (EditorGUI.EndChangeCheck())
            {
                format.intValue = (int)formatEnum;
            }

            TextureWrapMode wrapEnum = (TextureWrapMode)wrapMode.intValue;
            EditorGUI.BeginChangeCheck();
            wrapEnum = (TextureWrapMode)EditorGUILayout.EnumPopup("Wrap Mode", wrapEnum);
            if (EditorGUI.EndChangeCheck())
            {
                wrapMode.intValue = (int)wrapEnum;
            }

            FilterMode filterEnum = (FilterMode)filterMode.intValue;
            EditorGUI.BeginChangeCheck();
            filterEnum = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", filterEnum);
            if (EditorGUI.EndChangeCheck())
            {
                filterMode.intValue = (int)filterEnum;
            }

            EditorGUILayout.PropertyField(generateMipMaps);
            EditorGUILayout.PropertyField(anisoLevel);

            // Important: call this at end!
            base.ApplyRevertGUI();
        }
    }
}
