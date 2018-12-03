using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CurveCustomDrawerAttribute))]
public class CurveCustomDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var curveAttr = attribute as CurveCustomDrawerAttribute;

        var color = Color.green;
        ColorUtility.TryParseHtmlString(curveAttr.color, out color);
        EditorGUI.CurveField(position, property, color, Rect.MinMaxRect(curveAttr.minX, curveAttr.minY,
            curveAttr.maxX, curveAttr.maxY));
    }
}


