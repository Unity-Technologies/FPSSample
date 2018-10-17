using UnityEditor;

namespace UnityEngine.Experimental.Rendering
{
    [CustomPropertyDrawer(typeof(XRGraphicsConfig))]
    public class XRGraphicsConfigDrawer : PropertyDrawer
    {
        internal class Styles
        {
            public static GUIContent XRSettingsLabel = new GUIContent("XR Config", "Enable XR in Player Settings. Then SetConfig can be used to set this configuration to XRSettings.");
            public static GUIContent useOcclusionMeshLabel = new GUIContent("Use Occlusion Mesh", "Determines whether or not to draw the occlusion mesh (goggles-shaped overlay) when rendering");
            public static GUIContent occlusionScaleLabel = new GUIContent("Occlusion Mesh Scale", "Scales the occlusion mesh");

        }
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var drawUseOcclusionMesh = property.FindPropertyRelative("useOcclusionMesh");
            var drawOcclusionMeshScale = property.FindPropertyRelative("occlusionMeshScale");

            EditorGUI.BeginDisabledGroup(!XRGraphicsConfig.tryEnable);
            EditorGUILayout.LabelField(Styles.XRSettingsLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(drawUseOcclusionMesh, Styles.useOcclusionMeshLabel);
            EditorGUILayout.PropertyField(drawOcclusionMeshScale, Styles.occlusionScaleLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUI.EndDisabledGroup();
        }
    }
}
