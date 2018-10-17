using UnityEngine;
using UnityEngine.Recorder;

namespace UnityEditor.Recorder
{
    [CustomPropertyDrawer(typeof(OutputPath))]
    class OutputPathDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float rootWidth = 70;
            float btnWidth = 30;
            float leafWidth = position.width - rootWidth - btnWidth - 10;
            var rootRect = new Rect(position.x, position.y, rootWidth, position.height);
            var leafRect = new Rect(position.x + rootWidth + 5, position.y, leafWidth, position.height);
            var btnRect = new Rect(position.x + rootWidth  + leafWidth + 10, position.y, btnWidth, position.height);


            EditorGUI.PropertyField(rootRect, property.FindPropertyRelative("m_root"), GUIContent.none);
            EditorGUI.PropertyField(leafRect, property.FindPropertyRelative("m_leaf"), GUIContent.none);

            var fullPath = OutputPath.GetFullPath( (OutputPath.ERoot)property.FindPropertyRelative("m_root").intValue, property.FindPropertyRelative("m_leaf").stringValue);
            if (GUI.Button( btnRect, new GUIContent("...", fullPath)))
            {
                var newPath = EditorUtility.OpenFolderPanel("Select output location", fullPath, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    var newValue = OutputPath.FromPath(newPath);
                    property.FindPropertyRelative("m_root").intValue = (int)newValue.root;
                    property.FindPropertyRelative("m_leaf").stringValue = newValue.leaf;
                }
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
