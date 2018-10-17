using UnityEngine;
using UnityEditor;

 
[CustomPropertyDrawer (typeof(EnumeratedArrayAttribute))]
public class EnumeratedArrayDrawer : PropertyDrawer
{
    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        int idx = -1;
        bool ok = int.TryParse(property.propertyPath.AfterLast("[").BeforeFirst("]"), out idx);
        var names = ((EnumeratedArrayAttribute)attribute).names;
        var name = ok && idx >= 0 && idx < names.Length ? names[idx] : "Unknown (" + idx + ")";
        EditorGUI.PropertyField(rect, property, new GUIContent(name));
    }
}